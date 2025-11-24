using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public OrderService(ApplicationDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderId).ToList();
            var invoices = await _context.Invoices
                .Where(i => orderIds.Contains(i.OrderId))
                .ToDictionaryAsync(i => i.OrderId, i => i.InvoiceId);

            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                UserId = o.Id,
                BarCodeId = o.BarCodeId,
                Date = o.Date,
                OrderQuantity = o.OrderQuantity,
                ProductId = o.ProductId,
                ProductMSRP = o.ProductMSRP,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                ProductName = o.Product?.ProductName,
                UserName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}" : null,
                CustomerFullName = o.CustomerFullName,
                CustomerPhone = o.CustomerPhone,
                CustomerAddress = o.CustomerAddress,
                CustomerEmail = o.CustomerEmail,
                InvoiceId = invoices.ContainsKey(o.OrderId) ? invoices[o.OrderId] : null
            });
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return null;

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.OrderId == order.OrderId);

            return new OrderDto
            {
                OrderId = order.OrderId,
                UserId = order.Id,
                BarCodeId = order.BarCodeId,
                Date = order.Date,
                OrderQuantity = order.OrderQuantity,
                ProductId = order.ProductId,
                ProductMSRP = order.ProductMSRP,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                ProductName = order.Product?.ProductName,
                UserName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}" : null,
                CustomerFullName = order.CustomerFullName,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                CustomerEmail = order.CustomerEmail,
                InvoiceId = invoice?.InvoiceId
            };
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            var order = new Models.Order
            {
                Id = createOrderDto.UserId,
                BarCodeId = createOrderDto.BarCodeId,
                Date = DateTime.UtcNow,
                OrderQuantity = createOrderDto.OrderQuantity,
                ProductId = createOrderDto.ProductId,
                ProductMSRP = createOrderDto.ProductMSRP,
                Status = "Pending",
                TotalAmount = createOrderDto.Items.Sum(i => i.Quantity * i.UnitPrice),
                OrderStatus = "Pending",
                PaymentMethod = createOrderDto.PaymentMethod,
                CustomerFullName = createOrderDto.CustomerFullName,
                CustomerPhone = createOrderDto.CustomerPhone,
                CustomerAddress = createOrderDto.CustomerAddress,
                CustomerEmail = createOrderDto.CustomerEmail
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order product maps
            foreach (var item in createOrderDto.Items)
            {
                var orderProductMap = new Models.OrderProductMap
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice
                };
                _context.OrderProductMaps.Add(orderProductMap);
            }

            await _context.SaveChangesAsync();

            // Create history entry for order creation
            var createHistory = new Models.OrderHistory
            {
                OrderId = order.OrderId,
                UserId = createOrderDto.UserId,
                PreviousStatus = null,
                NewStatus = "Pending",
                PreviousOrderStatus = null,
                NewOrderStatus = "Pending",
                Action = "Created",
                Notes = "Order created",
                ChangedDate = DateTime.UtcNow
            };
            _context.OrderHistories.Add(createHistory);

            // Automatically create invoice for the order
            int? invoiceId = null;
            try
            {
                var createInvoiceDto = new CreateInvoiceDto
                {
                    OrderId = order.OrderId,
                    DueDate = DateTime.UtcNow.AddDays(30) // Default 30 days payment term
                };
                var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);
                invoiceId = invoice.InvoiceId;
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the order creation
                // Invoice can be created manually later if needed
                // In production, use proper logging framework (e.g., ILogger)
                System.Diagnostics.Debug.WriteLine($"Failed to create invoice for order {order.OrderId}: {ex.Message}");
            }

            await _context.SaveChangesAsync();

            var orderDto = await GetOrderByIdAsync(order.OrderId) ?? new OrderDto();
            if (invoiceId.HasValue)
            {
                orderDto.InvoiceId = invoiceId.Value;
            }
            return orderDto;
        }

        public async Task<bool> UpdateOrderAsync(int id, OrderDto orderDto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            order.Id = orderDto.UserId;
            order.BarCodeId = orderDto.BarCodeId;
            order.OrderQuantity = orderDto.OrderQuantity;
            order.ProductId = orderDto.ProductId;
            order.ProductMSRP = orderDto.ProductMSRP;
            order.Status = orderDto.Status;
            order.TotalAmount = orderDto.TotalAmount;
            order.OrderStatus = orderDto.OrderStatus;
            order.PaymentMethod = orderDto.PaymentMethod;
            order.CustomerFullName = orderDto.CustomerFullName;
            order.CustomerPhone = orderDto.CustomerPhone;
            order.CustomerAddress = orderDto.CustomerAddress;
            order.CustomerEmail = orderDto.CustomerEmail;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto, int userId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            // Validate status values
            if (updateDto.Status != "Paid" && updateDto.Status != "Pending" && updateDto.Status != "Cancelled")
                throw new ArgumentException("Status must be either 'Paid', 'Pending', or 'Cancelled'");

            if (updateDto.OrderStatus != "Paid" && updateDto.OrderStatus != "Pending" && updateDto.OrderStatus != "Cancelled")
                throw new ArgumentException("OrderStatus must be either 'Paid', 'Pending', or 'Cancelled'");

            // Store previous values for history
            var previousStatus = order.Status;
            var previousOrderStatus = order.OrderStatus;

            // Only update status fields
            order.Status = updateDto.Status;
            order.OrderStatus = updateDto.OrderStatus;

            await _context.SaveChangesAsync();

            // Create history entry
            var history = new Models.OrderHistory
            {
                OrderId = order.OrderId,
                UserId = userId,
                PreviousStatus = previousStatus,
                NewStatus = updateDto.Status,
                PreviousOrderStatus = previousOrderStatus,
                NewOrderStatus = updateDto.OrderStatus,
                Action = updateDto.Status == "Cancelled" || updateDto.OrderStatus == "Cancelled" ? "Cancelled" : 
                         updateDto.Status == "Paid" ? "Paid" : "Status Updated",
                Notes = $"Order status changed from {previousStatus} to {updateDto.Status}",
                ChangedDate = DateTime.UtcNow
            };
            _context.OrderHistories.Add(history);

            // Update invoice status when order is paid
            if (updateDto.Status == "Paid" || updateDto.OrderStatus == "Paid")
            {
                try
                {
                    await _invoiceService.UpdateInvoiceStatusByOrderIdAsync(order.OrderId, "Paid");
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the operation
                    System.Diagnostics.Debug.WriteLine($"Failed to update invoice status for order {order.OrderId}: {ex.Message}");
                }
            }

            // Update invoice status when order is cancelled
            if (updateDto.Status == "Cancelled" || updateDto.OrderStatus == "Cancelled")
            {
                try
                {
                    await _invoiceService.UpdateInvoiceStatusByOrderIdAsync(order.OrderId, "Cancelled");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to update invoice status for order {order.OrderId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> BulkUpdateOrderStatusAsync(BulkUpdateOrderStatusDto bulkUpdateDto, int userId)
        {
            if (bulkUpdateDto.OrderIds == null || bulkUpdateDto.OrderIds.Count == 0)
                return 0;

            // Validate status values
            if (bulkUpdateDto.Status != "Paid" && bulkUpdateDto.Status != "Pending" && bulkUpdateDto.Status != "Cancelled")
                throw new ArgumentException("Status must be either 'Paid', 'Pending', or 'Cancelled'");

            if (bulkUpdateDto.OrderStatus != "Paid" && bulkUpdateDto.OrderStatus != "Pending" && bulkUpdateDto.OrderStatus != "Cancelled")
                throw new ArgumentException("OrderStatus must be either 'Paid', 'Pending', or 'Cancelled'");

            var orders = await _context.Orders
                .Where(o => bulkUpdateDto.OrderIds.Contains(o.OrderId))
                .ToListAsync();

            int updatedCount = 0;
            foreach (var order in orders)
            {
                var previousStatus = order.Status;
                var previousOrderStatus = order.OrderStatus;

                order.Status = bulkUpdateDto.Status;
                order.OrderStatus = bulkUpdateDto.OrderStatus;
                updatedCount++;

                // Create history entry for each order
                var history = new Models.OrderHistory
                {
                    OrderId = order.OrderId,
                    UserId = userId,
                    PreviousStatus = previousStatus,
                    NewStatus = bulkUpdateDto.Status,
                    PreviousOrderStatus = previousOrderStatus,
                    NewOrderStatus = bulkUpdateDto.OrderStatus,
                    Action = bulkUpdateDto.Status == "Cancelled" || bulkUpdateDto.OrderStatus == "Cancelled" ? "Cancelled" : 
                             bulkUpdateDto.Status == "Paid" ? "Paid" : "Bulk Status Updated",
                    Notes = $"Bulk update: Order status changed from {previousStatus} to {bulkUpdateDto.Status}",
                    ChangedDate = DateTime.UtcNow
                };
                _context.OrderHistories.Add(history);

                // Update invoice status when order is paid
                if (bulkUpdateDto.Status == "Paid" || bulkUpdateDto.OrderStatus == "Paid")
                {
                    try
                    {
                        await _invoiceService.UpdateInvoiceStatusByOrderIdAsync(order.OrderId, "Paid");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to update invoice status for order {order.OrderId}: {ex.Message}");
                    }
                }

                // Update invoice status when order is cancelled
                if (bulkUpdateDto.Status == "Cancelled" || bulkUpdateDto.OrderStatus == "Cancelled")
                {
                    try
                    {
                        await _invoiceService.UpdateInvoiceStatusByOrderIdAsync(order.OrderId, "Cancelled");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to update invoice status for order {order.OrderId}: {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return updatedCount;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CustomerSearchDto>> SearchCustomersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<CustomerSearchDto>();

            var searchLower = searchTerm.ToLower();

            var customers = await _context.Orders
                .Where(o => !string.IsNullOrEmpty(o.CustomerFullName) &&
                           (o.CustomerFullName.ToLower().Contains(searchLower) ||
                            (o.CustomerPhone != null && o.CustomerPhone.Contains(searchTerm)) ||
                            (o.CustomerEmail != null && o.CustomerEmail.ToLower().Contains(searchLower))))
                .GroupBy(o => new
                {
                    o.CustomerFullName,
                    o.CustomerPhone,
                    o.CustomerEmail,
                    o.CustomerAddress
                })
                .Select(g => new CustomerSearchDto
                {
                    CustomerFullName = g.Key.CustomerFullName ?? string.Empty,
                    CustomerPhone = g.Key.CustomerPhone,
                    CustomerEmail = g.Key.CustomerEmail,
                    CustomerAddress = g.Key.CustomerAddress,
                    OrderCount = g.Count(),
                    LastOrderDate = g.Max(o => o.Date)
                })
                .OrderByDescending(c => c.LastOrderDate)
                .Take(20) // Limit to 20 results
                .ToListAsync();

            return customers;
        }
    }
}


