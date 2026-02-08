using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;
        private readonly IAccountingService _accountingService;
        private readonly IProductService _productService;
        private readonly ILedgerService _ledgerService;
        private readonly TenantContext _tenantContext;

        public OrderService(ApplicationDbContext context, IInvoiceService invoiceService, 
            IAccountingService accountingService, IProductService productService, ILedgerService ledgerService, TenantContext tenantContext)
        {
            _context = context;
            _invoiceService = invoiceService;
            _accountingService = accountingService;
            _productService = productService;
            _ledgerService = ledgerService;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            // Enforce strict branch isolation - no branchId means no data
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<OrderDto>();
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .Where(o => o.BranchId == _tenantContext.BranchId.Value);

            var orders = await query.ToListAsync();

            var orderIds = orders.Select(o => o.OrderId).ToList();
            var invoices = await _context.Invoices
                .Where(i => orderIds.Contains(i.OrderId) && i.InvoiceType != "CreditNote")
                .GroupBy(i => i.OrderId)
                .Select(g => new { OrderId = g.Key, InvoiceId = g.First().InvoiceId })
                .ToDictionaryAsync(x => x.OrderId, x => x.InvoiceId);

            return orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                CustomerId = o.CustomerId,
                BarCodeId = o.BarCodeId,
                Date = o.Date,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                CustomerName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : null,
                CustomerPhone = o.Customer?.Phone,
                CustomerEmail = o.Customer?.Email,
                CustomerAddress = o.Customer?.CurrentCity,
                InvoiceId = invoices.ContainsKey(o.OrderId) ? invoices[o.OrderId] : null,
                Items = o.OrderProductMaps.Select(opm => new OrderItemDetailDto
                {
                    ProductId = opm.ProductId,
                    ProductName = opm.Product?.ProductName ?? "Unknown",
                    Quantity = opm.Quantity,
                    UnitPrice = opm.UnitPrice,
                    TotalPrice = opm.TotalPrice
                }).ToList()
            });
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            // Enforce strict branch isolation - cannot view order without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.BranchId == _tenantContext.BranchId.Value);

            if (order == null)
                return null;

            var invoice = await _context.Invoices
                .Where(i => i.OrderId == order.OrderId && i.InvoiceType != "CreditNote")
                .FirstOrDefaultAsync();

            return new OrderDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                BarCodeId = order.BarCodeId,
                Date = order.Date,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                CustomerName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}" : null,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,
                CustomerAddress = order.Customer?.CurrentCity,
                InvoiceId = invoice?.InvoiceId,
                Items = order.OrderProductMaps.Select(opm => new OrderItemDetailDto
                {
                    ProductId = opm.ProductId,
                    ProductName = opm.Product?.ProductName ?? "Unknown",
                    Quantity = opm.Quantity,
                    UnitPrice = opm.UnitPrice,
                    TotalPrice = opm.TotalPrice
                }).ToList()
            };
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            // Enforce strict branch isolation - cannot create order without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create order without branch context. User must be assigned to a branch.");
            }

            // Validate items array
            if (createOrderDto.Items == null || createOrderDto.Items.Count == 0)
            {
                throw new InvalidOperationException("Order must contain at least one item");
            }

            // Pre-validate product IDs exist and have sufficient stock in a single query
            var productIds = createOrderDto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId) && p.BranchId == _tenantContext.BranchId.Value)
                .Select(p => new { p.ProductId, p.ProductUnitStock, p.ProductName })
                .ToDictionaryAsync(p => p.ProductId);

            // Validate all products exist and have sufficient stock
            foreach (var item in createOrderDto.Items)
            {
                if (!products.ContainsKey(item.ProductId))
                {
                    throw new InvalidOperationException($"Product with ID {item.ProductId} not found");
                }
                
                var product = products[item.ProductId];
                if (product.ProductUnitStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for {product.ProductName}. Available: {product.ProductUnitStock}, Required: {item.Quantity}");
                }
            }

            // Step 1: Find or create customer with optimized single query
            int customerId;
            User? customer = null;

            if (!string.IsNullOrWhiteSpace(createOrderDto.CustomerPhone) || !string.IsNullOrWhiteSpace(createOrderDto.CustomerEmail))
            {
                customer = await _context.Users
                    .Where(u => u.Role == "Customer" && 
                               ((!string.IsNullOrEmpty(createOrderDto.CustomerPhone) && u.Phone == createOrderDto.CustomerPhone) ||
                                (!string.IsNullOrEmpty(createOrderDto.CustomerEmail) && u.Email == createOrderDto.CustomerEmail)))
                    .FirstOrDefaultAsync();
            }

            // Create new customer if not found
            if (customer == null)
            {
                if (string.IsNullOrWhiteSpace(createOrderDto.CustomerFullName))
                {
                    throw new InvalidOperationException("Customer full name is required to create a new order");
                }

                var nameParts = createOrderDto.CustomerFullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : createOrderDto.CustomerFullName;
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                customer = new User
                {
                    UserId = $"CUST_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}",
                    FirstName = firstName,
                    LastName = lastName,
                    Password = "", // Customers don't need login password
                    Email = createOrderDto.CustomerEmail,
                    Phone = createOrderDto.CustomerPhone,
                    CurrentCity = createOrderDto.CustomerAddress,
                    Role = "Customer",
                    Salary = 0,
                    Age = 0,
                    JoinDate = DateTime.UtcNow,
                    Birthdate = DateTime.UtcNow // Default, not relevant for customers
                };

                _context.Users.Add(customer);
                // Don't save yet - batch with order creation
            }

            customerId = customer.Id;

            // Step 2: Calculate total amount from items
            var totalAmount = createOrderDto.Items.Sum(item => item.Quantity * item.UnitPrice);

            // Step 3: Create order
            var order = new Models.Order
            {
                CustomerId = customerId,
                BarCodeId = createOrderDto.BarCodeId,
                Date = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                PaymentMethod = createOrderDto.PaymentMethod,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };

            _context.Orders.Add(order);

            // Step 4: Batch create all order product maps
            var orderProductMaps = createOrderDto.Items.Select(item => new Models.OrderProductMap
            {
                Order = order, // EF will resolve the OrderId after SaveChanges
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            }).ToList();

            _context.OrderProductMaps.AddRange(orderProductMaps);

            // Step 5: Create history entry
            var createHistory = new Models.OrderHistory
            {
                Order = order, // EF will resolve the OrderId after SaveChanges
                UserId = customerId,
                PreviousStatus = null,
                NewStatus = "Pending",
                PreviousOrderStatus = null,
                NewOrderStatus = "Pending",
                Action = "Created",
                Notes = $"Order created for customer {customer.FirstName} {customer.LastName}",
                ChangedDate = DateTime.UtcNow
            };
            _context.OrderHistories.Add(createHistory);

            // Batch save all changes at once
            await _context.SaveChangesAsync();

            // Step 6: Batch deduct inventory for all items
            var inventoryTasks = createOrderDto.Items.Select(item => 
                _productService.DeductInventoryAsync(item.ProductId, item.Quantity)).ToList();

            try
            {
                await Task.WhenAll(inventoryTasks);
            }
            catch (Exception ex)
            {
                // Rollback the order if inventory deduction fails
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                throw new InvalidOperationException($"Order creation failed during inventory deduction: {ex.Message}");
            }

            // Step 7: Create invoice and ledger entry (keep async but don't block order creation)
            _ = Task.Run(async () =>
            {
                try
                {
                    var createInvoiceDto = new CreateInvoiceDto
                    {
                        OrderId = order.OrderId,
                        DueDate = DateTime.UtcNow.AddDays(30)
                    };
                    var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);

                    // Create ledger entry
                    await _ledgerService.CreateSaleLedgerEntryAsync(order.OrderId, invoice.InvoiceId, "System");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create invoice/ledger for order {order.OrderId}: {ex.Message}");
                }
            });

            // Return optimized DTO without additional query
            return new OrderDto
            {
                OrderId = order.OrderId,
                CustomerId = customerId,
                BarCodeId = order.BarCodeId,
                Date = order.Date,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                CustomerPhone = customer.Phone,
                CustomerEmail = customer.Email,
                CustomerAddress = customer.CurrentCity,
                InvoiceId = null, // Invoice creation is async, will be available later
                Items = orderProductMaps.Select(opm => new OrderItemDetailDto
                {
                    ProductId = opm.ProductId,
                    ProductName = products[opm.ProductId].ProductName,
                    Quantity = opm.Quantity,
                    UnitPrice = opm.UnitPrice,
                    TotalPrice = opm.TotalPrice
                }).ToList()
            };
        }

        public async Task<bool> UpdateOrderAsync(int id, OrderDto orderDto)
        {
            // Enforce strict branch isolation - cannot update without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.BranchId == _tenantContext.BranchId.Value);
            if (order == null)
                return false;

            order.CustomerId = orderDto.CustomerId;
            order.BarCodeId = orderDto.BarCodeId;
            order.Status = orderDto.Status;
            order.TotalAmount = orderDto.TotalAmount;
            order.OrderStatus = orderDto.OrderStatus;
            order.PaymentMethod = orderDto.PaymentMethod;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto, int userId)
        {
            // Enforce strict branch isolation - cannot update status without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var order = await _context.Orders
                .Include(o => o.OrderProductMaps)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.BranchId == _tenantContext.BranchId.Value);
            
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

            // Restore inventory when order is cancelled
            if ((updateDto.Status == "Cancelled" || updateDto.OrderStatus == "Cancelled") && 
                (previousStatus != "Cancelled" && previousOrderStatus != "Cancelled"))
            {
                foreach (var orderProductMap in order.OrderProductMaps)
                {
                    try
                    {
                        await _productService.RestoreInventoryAsync(orderProductMap.ProductId, orderProductMap.Quantity);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to restore inventory for product {orderProductMap.ProductId}: {ex.Message}");
                    }
                }
            }

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

            // Get user info for accounting entries
            var user = await _context.Users.FindAsync(userId);
            var username = user != null ? $"{user.FirstName} {user.LastName}" : "System";

            // Create accounting entry for sale when order is paid
            if ((updateDto.Status == "Paid" || updateDto.OrderStatus == "Paid") && 
                (previousStatus != "Paid" && previousOrderStatus != "Paid"))
            {
                try
                {
                    await _accountingService.CreateSaleEntryFromOrderAsync(order.OrderId, username);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create accounting entry for order {order.OrderId}: {ex.Message}");
                }
            }

            // Create accounting entry for refund when order is cancelled
            if ((updateDto.Status == "Cancelled" || updateDto.OrderStatus == "Cancelled") && 
                (previousStatus != "Cancelled" && previousOrderStatus != "Cancelled") &&
                (previousStatus == "Paid" || previousOrderStatus == "Paid"))
            {
                try
                {
                    await _accountingService.CreateRefundEntryFromOrderAsync(order.OrderId, username);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create refund entry for order {order.OrderId}: {ex.Message}");
                }
            }

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
            // Enforce strict branch isolation - cannot bulk update without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return 0;
            }

            if (bulkUpdateDto.OrderIds == null || bulkUpdateDto.OrderIds.Count == 0)
                return 0;

            // Validate status values
            if (bulkUpdateDto.Status != "Paid" && bulkUpdateDto.Status != "Pending" && bulkUpdateDto.Status != "Cancelled")
                throw new ArgumentException("Status must be either 'Paid', 'Pending', or 'Cancelled'");

            if (bulkUpdateDto.OrderStatus != "Paid" && bulkUpdateDto.OrderStatus != "Pending" && bulkUpdateDto.OrderStatus != "Cancelled")
                throw new ArgumentException("OrderStatus must be either 'Paid', 'Pending', or 'Cancelled'");

            var orders = await _context.Orders
                .Include(o => o.OrderProductMaps)
                .Where(o => bulkUpdateDto.OrderIds.Contains(o.OrderId) && o.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();

            // Get user info for accounting entries
            var user = await _context.Users.FindAsync(userId);
            var username = user != null ? $"{user.FirstName} {user.LastName}" : "System";

            int updatedCount = 0;
            foreach (var order in orders)
            {
                var previousStatus = order.Status;
                var previousOrderStatus = order.OrderStatus;

                // Restore inventory when order is cancelled
                if ((bulkUpdateDto.Status == "Cancelled" || bulkUpdateDto.OrderStatus == "Cancelled") && 
                    (previousStatus != "Cancelled" && previousOrderStatus != "Cancelled"))
                {
                    foreach (var orderProductMap in order.OrderProductMaps)
                    {
                        try
                        {
                            await _productService.RestoreInventoryAsync(orderProductMap.ProductId, orderProductMap.Quantity);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to restore inventory for product {orderProductMap.ProductId}: {ex.Message}");
                        }
                    }
                }

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

                // Create accounting entry for sale when order is paid
                if ((bulkUpdateDto.Status == "Paid" || bulkUpdateDto.OrderStatus == "Paid") && 
                    (previousStatus != "Paid" && previousOrderStatus != "Paid"))
                {
                    try
                    {
                        await _accountingService.CreateSaleEntryFromOrderAsync(order.OrderId, username);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create accounting entry for order {order.OrderId}: {ex.Message}");
                    }
                }

                // Create accounting entry for refund when order is cancelled
                if ((bulkUpdateDto.Status == "Cancelled" || bulkUpdateDto.OrderStatus == "Cancelled") && 
                    (previousStatus != "Cancelled" && previousOrderStatus != "Cancelled") &&
                    (previousStatus == "Paid" || previousOrderStatus == "Paid"))
                {
                    try
                    {
                        await _accountingService.CreateRefundEntryFromOrderAsync(order.OrderId, username);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create refund entry for order {order.OrderId}: {ex.Message}");
                    }
                }

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
            // Enforce strict branch isolation - cannot delete without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.BranchId == _tenantContext.BranchId.Value);
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

            // Search in Users table where Role = "Customer"
            var customers = await _context.Users
                .Where(u => u.Role == "Customer" &&
                           ((u.FirstName + " " + u.LastName).ToLower().Contains(searchLower) ||
                            (u.Phone != null && u.Phone.Contains(searchTerm)) ||
                            (u.Email != null && u.Email.ToLower().Contains(searchLower))))
                .Select(u => new
                {
                    u.Id,
                    CustomerFullName = u.FirstName + " " + u.LastName,
                    u.Phone,
                    u.Email,
                    u.CurrentCity
                })
                .ToListAsync();

            // Get order counts and last order dates for these customers
            var customerIds = customers.Select(c => c.Id).ToList();
            var orderStats = await _context.Orders
                .Where(o => customerIds.Contains(o.CustomerId))
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    LastOrderDate = g.Max(o => o.Date)
                })
                .ToDictionaryAsync(x => x.CustomerId, x => x);

            return customers.Select(c => new CustomerSearchDto
            {
                CustomerFullName = c.CustomerFullName,
                CustomerPhone = c.Phone,
                CustomerEmail = c.Email,
                CustomerAddress = c.CurrentCity,
                OrderCount = orderStats.ContainsKey(c.Id) ? orderStats[c.Id].OrderCount : 0,
                LastOrderDate = orderStats.ContainsKey(c.Id) ? orderStats[c.Id].LastOrderDate : DateTime.MinValue
            }).OrderByDescending(c => c.LastOrderDate)
              .Take(20)
              .ToList();
        }
    }
}





















































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































