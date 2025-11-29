using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;
using System.Text;

namespace POS.Api.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantContext _tenantContext;
        private readonly IAccountingService _accountingService;

        public InvoiceService(ApplicationDbContext context, TenantContext tenantContext, IAccountingService accountingService)
        {
            _context = context;
            _tenantContext = tenantContext;
            _accountingService = accountingService;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto)
        {
            // Enforce strict branch isolation - cannot create invoice without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create invoice without branch context. User must be assigned to a branch.");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == createInvoiceDto.OrderId && o.BranchId == _tenantContext.BranchId.Value);

            if (order == null)
                throw new ArgumentException("Order not found in your branch");

            // Check if invoice already exists for this order
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.OrderId == createInvoiceDto.OrderId);

            if (existingInvoice != null)
            {
                return await GetInvoiceByIdAsync(existingInvoice.InvoiceId) ?? throw new Exception("Failed to retrieve invoice");
            }

            // Generate invoice number
            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new Invoice
            {
                OrderId = createInvoiceDto.OrderId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.UtcNow,
                DueDate = createInvoiceDto.DueDate ?? DateTime.UtcNow.AddDays(30),
                Status = "Pending",
                TotalAmount = order.TotalAmount,
                AmountPaid = 0,
                Balance = order.TotalAmount
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return await GetInvoiceByIdAsync(invoice.InvoiceId) ?? throw new Exception("Failed to retrieve created invoice");
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            // Enforce strict branch isolation - cannot view invoice without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderProductMaps)
                        .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            if (invoice == null || invoice.Order == null)
                return null;

            var shopConfig = await GetShopConfigurationAsync();

            return new InvoiceDto
            {
                InvoiceId = invoice.InvoiceId,
                OrderId = invoice.OrderId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceType = invoice.InvoiceType,
                OriginalInvoiceId = invoice.OriginalInvoiceId,
                ReturnId = invoice.ReturnId,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                Balance = invoice.Balance,
                Order = new OrderDto
                {
                    OrderId = invoice.Order.OrderId,
                    CustomerId = invoice.Order.CustomerId,
                    BarCodeId = invoice.Order.BarCodeId,
                    Date = invoice.Order.Date,
                    Status = invoice.Order.Status,
                    TotalAmount = invoice.Order.TotalAmount,
                    PaymentMethod = invoice.Order.PaymentMethod,
                    OrderStatus = invoice.Order.OrderStatus,
                    CustomerName = invoice.Order.Customer != null 
                        ? $"{invoice.Order.Customer.FirstName} {invoice.Order.Customer.LastName}" 
                        : null,
                    CustomerPhone = invoice.Order.Customer?.Phone,
                    CustomerEmail = invoice.Order.Customer?.Email,
                    CustomerAddress = invoice.Order.Customer?.CurrentCity
                },
                Items = invoice.Order.OrderProductMaps.Select(opm => new InvoiceItemDto
                {
                    ProductId = opm.ProductId,
                    ProductName = opm.Product?.ProductName ?? "Unknown",
                    Quantity = opm.Quantity,
                    UnitPrice = opm.UnitPrice,
                    TotalPrice = opm.TotalPrice
                }).ToList(),
                ShopConfig = shopConfig
            };
        }

        public async Task<InvoiceDto?> GetInvoiceByOrderIdAsync(int orderId)
        {
            // Enforce strict branch isolation - cannot view invoice without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.OrderId == orderId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            if (invoice == null)
                return null;

            return await GetInvoiceByIdAsync(invoice.InvoiceId);
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            // Enforce strict branch isolation - cannot view invoices without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<InvoiceDto>();
            }

            // Filter for last 14 days only
            var fourteenDaysAgo = DateTime.UtcNow.AddDays(-14);
            
            var query = _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderProductMaps)
                        .ThenInclude(opm => opm.Product)
                .Where(i => i.InvoiceDate >= fourteenDaysAgo && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            var invoices = await query.ToListAsync();

            var shopConfig = await GetShopConfigurationAsync();
            var result = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                if (invoice.Order == null) continue;

                result.Add(new InvoiceDto
                {
                    InvoiceId = invoice.InvoiceId,
                    OrderId = invoice.OrderId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = invoice.AmountPaid,
                    Balance = invoice.Balance,
                    Order = new OrderDto
                    {
                        OrderId = invoice.Order.OrderId,
                        CustomerId = invoice.Order.CustomerId,
                        BarCodeId = invoice.Order.BarCodeId,
                        Date = invoice.Order.Date,
                        Status = invoice.Order.Status,
                        TotalAmount = invoice.Order.TotalAmount,
                        PaymentMethod = invoice.Order.PaymentMethod,
                        OrderStatus = invoice.Order.OrderStatus,
                        CustomerName = invoice.Order.Customer != null 
                            ? $"{invoice.Order.Customer.FirstName} {invoice.Order.Customer.LastName}" 
                            : null,
                        CustomerPhone = invoice.Order.Customer?.Phone,
                        CustomerEmail = invoice.Order.Customer?.Email,
                        CustomerAddress = invoice.Order.Customer?.CurrentCity
                    },
                    Items = invoice.Order.OrderProductMaps.Select(opm => new InvoiceItemDto
                    {
                        ProductId = opm.ProductId,
                        ProductName = opm.Product?.ProductName ?? "Unknown",
                        Quantity = opm.Quantity,
                        UnitPrice = opm.UnitPrice,
                        TotalPrice = opm.TotalPrice
                    }).ToList(),
                    ShopConfig = shopConfig
                });
            }

            return result;
        }

        public async Task<IEnumerable<InvoiceDto>> GetFilteredInvoicesAsync(InvoiceFilterDto filter)
        {
            // Enforce strict branch isolation - cannot view invoices without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<InvoiceDto>();
            }

            var query = _context.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer)
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderProductMaps)
                        .ThenInclude(opm => opm.Product)
                .Where(i => i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            // Filter by amount
            if (filter.MinAmount.HasValue)
            {
                query = query.Where(i => i.Order != null && i.Order.TotalAmount >= filter.MinAmount.Value);
            }

            if (filter.MaxAmount.HasValue)
            {
                query = query.Where(i => i.Order != null && i.Order.TotalAmount <= filter.MaxAmount.Value);
            }

            // Filter by date
            if (filter.StartDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= filter.EndDate.Value.AddDays(1).AddTicks(-1)); // Include entire end date
            }

            // Filter by customer address
            if (!string.IsNullOrWhiteSpace(filter.CustomerAddress))
            {
                var addressLower = filter.CustomerAddress.ToLower();
                query = query.Where(i => i.Order != null && 
                    i.Order.Customer != null &&
                    i.Order.Customer.CurrentCity != null && 
                    i.Order.Customer.CurrentCity.ToLower().Contains(addressLower));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(i => i.Status == filter.Status);
            }

            var invoices = await query.ToListAsync();

            var shopConfig = await GetShopConfigurationAsync();
            var result = new List<InvoiceDto>();

            foreach (var invoice in invoices)
            {
                if (invoice.Order == null) continue;

                result.Add(new InvoiceDto
                {
                    InvoiceId = invoice.InvoiceId,
                    OrderId = invoice.OrderId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = invoice.DueDate,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = invoice.AmountPaid,
                    Balance = invoice.Balance,
                    Order = new OrderDto
                    {
                        OrderId = invoice.Order.OrderId,
                        CustomerId = invoice.Order.CustomerId,
                        BarCodeId = invoice.Order.BarCodeId,
                        Date = invoice.Order.Date,
                        Status = invoice.Order.Status,
                        TotalAmount = invoice.Order.TotalAmount,
                        PaymentMethod = invoice.Order.PaymentMethod,
                        OrderStatus = invoice.Order.OrderStatus,
                        CustomerName = invoice.Order.Customer != null 
                            ? $"{invoice.Order.Customer.FirstName} {invoice.Order.Customer.LastName}" 
                            : null,
                        CustomerPhone = invoice.Order.Customer?.Phone,
                        CustomerEmail = invoice.Order.Customer?.Email,
                        CustomerAddress = invoice.Order.Customer?.CurrentCity
                    },
                    Items = invoice.Order.OrderProductMaps.Select(opm => new InvoiceItemDto
                    {
                        ProductId = opm.ProductId,
                        ProductName = opm.Product?.ProductName ?? "Unknown",
                        Quantity = opm.Quantity,
                        UnitPrice = opm.UnitPrice,
                        TotalPrice = opm.TotalPrice
                    }).ToList(),
                    ShopConfig = shopConfig
                });
            }

            return result;
        }

        public async Task<string> GenerateInvoiceHtmlAsync(int invoiceId)
        {
            // Enforce strict branch isolation - cannot generate HTML without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot generate invoice HTML without branch context.");
            }

            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                throw new ArgumentException("Invoice not found in your branch");

            var order = await _context.Orders
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == invoice.OrderId);

            if (order == null)
                throw new ArgumentException("Order not found");

            var items = order.OrderProductMaps.Select(opm => new InvoiceItemDto
            {
                ProductId = opm.ProductId,
                ProductName = opm.Product?.ProductName ?? "Unknown",
                Quantity = opm.Quantity,
                UnitPrice = opm.UnitPrice,
                TotalPrice = opm.TotalPrice
            }).ToList();

            var subTotal = items.Sum(i => i.TotalPrice);
            var tax = 0m; // Can be calculated based on shop configuration
            var discount = 0m; // Can be calculated based on order
            var total = subTotal + tax - discount;

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Invoice " + invoice.InvoiceNumber + "</title>");
            html.AppendLine("<style>");
            html.AppendLine(GetInvoiceStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine(GenerateInvoiceBody(invoice, items, subTotal, tax, discount, total));
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        public async Task<string> GenerateBulkInvoiceHtmlAsync(List<int> invoiceIds)
        {
            // Enforce strict branch isolation - cannot generate bulk HTML without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot generate bulk invoice HTML without branch context.");
            }

            if (invoiceIds == null || invoiceIds.Count == 0)
                throw new ArgumentException("At least one invoice ID is required");

            var shopConfig = await GetShopConfigurationAsync();
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Bulk Invoice Print</title>");
            html.AppendLine("<style>");
            html.AppendLine(GetInvoiceStyles());
            html.AppendLine(@"
                .invoice-page {
                    page-break-after: always;
                    margin-bottom: 50px;
                }
                .invoice-page:last-child {
                    page-break-after: auto;
                }
                @media print {
                    .invoice-page {
                        page-break-after: always;
                    }
                    .invoice-page:last-child {
                        page-break-after: auto;
                    }
                }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            foreach (var invoiceId in invoiceIds)
            {
                try
                {
                    var invoice = await GetInvoiceByIdAsync(invoiceId);
                    if (invoice == null) continue;

                    var order = await _context.Orders
                        .Include(o => o.OrderProductMaps)
                            .ThenInclude(opm => opm.Product)
                        .FirstOrDefaultAsync(o => o.OrderId == invoice.OrderId);

                    if (order == null) continue;

                    var items = order.OrderProductMaps.Select(opm => new InvoiceItemDto
                    {
                        ProductId = opm.ProductId,
                        ProductName = opm.Product?.ProductName ?? "Unknown",
                        Quantity = opm.Quantity,
                        UnitPrice = opm.UnitPrice,
                        TotalPrice = opm.TotalPrice
                    }).ToList();

                    var subTotal = items.Sum(i => i.TotalPrice);
                    var tax = 0m;
                    var discount = 0m;
                    var total = subTotal + tax - discount;

                    // Wrap each invoice in a page-break div
                    html.AppendLine("<div class='invoice-page'>");
                    html.AppendLine(GenerateInvoiceBody(invoice, items, subTotal, tax, discount, total));
                    html.AppendLine("</div>");
                }
                catch
                {
                    // Skip invoices that fail to generate
                    continue;
                }
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateInvoiceBody(InvoiceDto invoice, List<InvoiceItemDto> items, decimal subTotal, decimal tax, decimal discount, decimal total)
        {
            var sb = new StringBuilder();
            var shop = invoice.ShopConfig;

            sb.AppendLine("<div class='invoice-container'>");
            
            // Header
            sb.AppendLine("<div class='invoice-header'>");
            if (!string.IsNullOrEmpty(shop.LogoBase64))
            {
                sb.AppendLine($"<img src='data:image/png;base64,{shop.LogoBase64}' alt='Logo' class='logo' />");
            }
            sb.AppendLine("<div class='shop-info'>");
            sb.AppendLine($"<h1>{shop.ShopName}</h1>");
            if (!string.IsNullOrEmpty(shop.ShopAddress))
                sb.AppendLine($"<p>{shop.ShopAddress}</p>");
            if (!string.IsNullOrEmpty(shop.ShopPhone))
                sb.AppendLine($"<p>Phone: {shop.ShopPhone}</p>");
            if (!string.IsNullOrEmpty(shop.ShopEmail))
                sb.AppendLine($"<p>Email: {shop.ShopEmail}</p>");
            if (!string.IsNullOrEmpty(shop.ShopWebsite))
                sb.AppendLine($"<p>Website: {shop.ShopWebsite}</p>");
            if (!string.IsNullOrEmpty(shop.TaxId))
                sb.AppendLine($"<p>Tax ID: {shop.TaxId}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            if (!string.IsNullOrEmpty(shop.HeaderMessage))
            {
                sb.AppendLine($"<div class='header-message'>{shop.HeaderMessage}</div>");
            }

            // Invoice Info
            sb.AppendLine("<div class='invoice-info'>");
            sb.AppendLine("<div class='invoice-details'>");
            sb.AppendLine($"<h2>INVOICE</h2>");
            sb.AppendLine($"<p><strong>Invoice #:</strong> {invoice.InvoiceNumber}</p>");
            sb.AppendLine($"<p><strong>Date:</strong> {invoice.InvoiceDate:yyyy-MM-dd}</p>");
            sb.AppendLine($"<p><strong>Due Date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>");
            sb.AppendLine($"<p><strong>Status:</strong> {invoice.Status}</p>");
            sb.AppendLine("</div>");

            // Customer Info
            sb.AppendLine("<div class='customer-details'>");
            sb.AppendLine("<h3>Bill To:</h3>");
            if (!string.IsNullOrEmpty(invoice.Order.CustomerName))
                sb.AppendLine($"<p><strong>{invoice.Order.CustomerName}</strong></p>");
            if (!string.IsNullOrEmpty(invoice.Order.CustomerAddress))
                sb.AppendLine($"<p>{invoice.Order.CustomerAddress}</p>");
            if (!string.IsNullOrEmpty(invoice.Order.CustomerPhone))
                sb.AppendLine($"<p>Phone: {invoice.Order.CustomerPhone}</p>");
            if (!string.IsNullOrEmpty(invoice.Order.CustomerEmail))
                sb.AppendLine($"<p>Email: {invoice.Order.CustomerEmail}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            // Items Table
            sb.AppendLine("<table class='items-table'>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>#</th>");
            sb.AppendLine("<th>Description</th>");
            sb.AppendLine("<th>Quantity</th>");
            sb.AppendLine("<th>Unit Price</th>");
            sb.AppendLine("<th>Total</th>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");
            
            int index = 1;
            foreach (var item in items)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{index++}</td>");
                sb.AppendLine($"<td>{item.ProductName}</td>");
                sb.AppendLine($"<td>{item.Quantity}</td>");
                sb.AppendLine($"<td>${item.UnitPrice:F2}</td>");
                sb.AppendLine($"<td>${item.TotalPrice:F2}</td>");
                sb.AppendLine("</tr>");
            }
            
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            // Totals
            sb.AppendLine("<div class='totals-section'>");
            sb.AppendLine("<table class='totals-table'>");
            sb.AppendLine("<tr><td>Subtotal:</td><td>$" + subTotal.ToString("F2") + "</td></tr>");
            if (discount > 0)
                sb.AppendLine("<tr><td>Discount:</td><td>-$" + discount.ToString("F2") + "</td></tr>");
            if (tax > 0)
                sb.AppendLine("<tr><td>Tax:</td><td>$" + tax.ToString("F2") + "</td></tr>");
            sb.AppendLine("<tr class='total-row'><td><strong>Total:</strong></td><td><strong>$" + total.ToString("F2") + "</strong></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            // Payment Method
            sb.AppendLine("<div class='payment-info'>");
            sb.AppendLine($"<p><strong>Payment Method:</strong> {invoice.Order.PaymentMethod}</p>");
            sb.AppendLine("</div>");

            // Footer
            if (!string.IsNullOrEmpty(shop.FooterMessage))
            {
                sb.AppendLine($"<div class='footer-message'>{shop.FooterMessage}</div>");
            }

            sb.AppendLine("<div class='invoice-footer'>");
            sb.AppendLine($"<p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private string GetInvoiceStyles()
        {
            return @"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }
                .invoice-container { max-width: 800px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
                .invoice-header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }
                .logo { max-width: 150px; max-height: 100px; }
                .shop-info h1 { color: #333; margin-bottom: 10px; }
                .shop-info p { margin: 5px 0; color: #666; }
                .header-message { background: #e3f2fd; padding: 15px; margin-bottom: 20px; border-radius: 4px; text-align: center; }
                .invoice-info { display: flex; justify-content: space-between; margin-bottom: 30px; }
                .invoice-details h2 { color: #1976d2; margin-bottom: 10px; }
                .invoice-details p { margin: 5px 0; }
                .customer-details h3 { margin-bottom: 10px; color: #333; }
                .customer-details p { margin: 3px 0; }
                .items-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                .items-table th { background: #1976d2; color: white; padding: 12px; text-align: left; }
                .items-table td { padding: 10px; border-bottom: 1px solid #ddd; }
                .items-table tr:hover { background: #f5f5f5; }
                .totals-section { margin-top: 20px; }
                .totals-table { width: 100%; max-width: 300px; margin-left: auto; }
                .totals-table td { padding: 8px; text-align: right; }
                .total-row { border-top: 2px solid #333; font-size: 1.2em; }
                .payment-info { margin-top: 20px; padding: 15px; background: #f9f9f9; border-radius: 4px; }
                .footer-message { margin-top: 30px; padding: 15px; background: #fff3cd; border-radius: 4px; text-align: center; }
                .invoice-footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #999; font-size: 0.9em; }
                @media print { body { background: white; padding: 0; } .invoice-container { box-shadow: none; } }
            ";
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var prefix = $"INV-{year}{month:D2}";

            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastInvoice != null)
            {
                var parts = lastInvoice.InvoiceNumber.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"{prefix}-{sequence:D4}";
        }

        public async Task<ShopConfigurationDto> GetShopConfigurationAsync()
        {
            var config = await _context.ShopConfigurations.FirstOrDefaultAsync();
            
            if (config == null)
            {
                // Create default configuration
                config = new ShopConfiguration
                {
                    ShopName = "POS System",
                    ShopAddress = "123 Main Street",
                    ShopPhone = "+1 234 567 8900",
                    ShopEmail = "info@possystem.com",
                    FooterMessage = "Thank you for your business!",
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ShopConfigurations.Add(config);
                await _context.SaveChangesAsync();
            }

            return new ShopConfigurationDto
            {
                Id = config.Id,
                ShopName = config.ShopName,
                ShopAddress = config.ShopAddress,
                ShopPhone = config.ShopPhone,
                ShopEmail = config.ShopEmail,
                ShopWebsite = config.ShopWebsite,
                TaxId = config.TaxId,
                FooterMessage = config.FooterMessage,
                HeaderMessage = config.HeaderMessage,
                LogoBase64 = config.Logo != null ? Convert.ToBase64String(config.Logo) : null
            };
        }

        public async Task<ShopConfigurationDto> UpdateShopConfigurationAsync(UpdateShopConfigurationDto dto)
        {
            var config = await _context.ShopConfigurations.FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ShopConfiguration();
                _context.ShopConfigurations.Add(config);
            }

            config.ShopName = dto.ShopName;
            config.ShopAddress = dto.ShopAddress;
            config.ShopPhone = dto.ShopPhone;
            config.ShopEmail = dto.ShopEmail;
            config.ShopWebsite = dto.ShopWebsite;
            config.TaxId = dto.TaxId;
            config.FooterMessage = dto.FooterMessage;
            config.HeaderMessage = dto.HeaderMessage;
            config.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.LogoBase64))
            {
                try
                {
                    config.Logo = Convert.FromBase64String(dto.LogoBase64);
                }
                catch
                {
                    // Invalid base64, skip logo update
                }
            }

            await _context.SaveChangesAsync();

            return await GetShopConfigurationAsync();
        }

        public async Task<bool> UpdateInvoiceStatusByOrderIdAsync(int orderId, string status)
        {
            // Enforce strict branch isolation - cannot update without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.OrderId == orderId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            if (invoice == null)
                return false;

            invoice.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InvoicePaymentDto> AddPaymentAsync(int invoiceId, CreateInvoicePaymentDto paymentDto, string receivedBy)
        {
            // Enforce strict branch isolation - cannot add payment without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot add payment without branch context.");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
            
            if (invoice == null)
                throw new ArgumentException("Invoice not found in your branch");

            // Validate payment amount
            if (paymentDto.Amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero");

            if (paymentDto.Amount > invoice.Balance)
                throw new ArgumentException($"Payment amount ({paymentDto.Amount:C}) cannot exceed invoice balance ({invoice.Balance:C})");

            if (invoice.Status == "Paid")
                throw new InvalidOperationException("Invoice is already fully paid");

            if (invoice.Status == "Cancelled")
                throw new InvalidOperationException("Cannot add payment to cancelled invoice");

            // Create payment record
            var payment = new InvoicePayment
            {
                InvoiceId = invoiceId,
                Amount = paymentDto.Amount,
                PaymentMethod = paymentDto.PaymentMethod,
                Notes = paymentDto.Notes,
                PaymentDate = paymentDto.PaymentDate,
                ReceivedBy = receivedBy,
                TransactionReference = paymentDto.TransactionReference,
                CreatedAt = DateTime.UtcNow
            };

            _context.InvoicePayments.Add(payment);

            // Update invoice amounts
            invoice.AmountPaid += paymentDto.Amount;
            invoice.Balance = invoice.TotalAmount - invoice.AmountPaid;

            // Update invoice status
            string newInvoiceStatus = invoice.Status;
            if (invoice.Balance == 0)
            {
                invoice.Status = "Paid";
                newInvoiceStatus = "Paid";
            }
            else if (invoice.AmountPaid > 0 && invoice.Balance > 0)
            {
                invoice.Status = "PartiallyPaid";
                newInvoiceStatus = "PartiallyPaid";
            }

            // Update corresponding order status to match invoice status
            if (invoice.Order != null)
            {
                invoice.Order.Status = newInvoiceStatus;
                invoice.Order.OrderStatus = newInvoiceStatus;
                System.Diagnostics.Debug.WriteLine($"Updated Order {invoice.Order.OrderId} status to '{newInvoiceStatus}' based on invoice payment");
            }

            await _context.SaveChangesAsync();

            // Create accounting entry for the payment
            try
            {
                await _accountingService.CreatePaymentEntryAsync(invoiceId, payment.PaymentId, receivedBy);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create accounting entry for payment {payment.PaymentId} on invoice {invoiceId}: {ex.Message}");
            }

            return new InvoicePaymentDto
            {
                PaymentId = payment.PaymentId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Notes = payment.Notes,
                PaymentDate = payment.PaymentDate,
                ReceivedBy = payment.ReceivedBy,
                TransactionReference = payment.TransactionReference,
                CreatedAt = payment.CreatedAt
            };
        }

        public async Task<InvoicePaymentSummaryDto> GetInvoicePaymentsAsync(int invoiceId)
        {
            // Enforce strict branch isolation - cannot view payments without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot view invoice payments without branch context.");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            if (invoice == null)
                throw new ArgumentException("Invoice not found in your branch");

            var payments = invoice.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new InvoicePaymentDto
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Notes = p.Notes,
                    PaymentDate = p.PaymentDate,
                    ReceivedBy = p.ReceivedBy,
                    TransactionReference = p.TransactionReference,
                    CreatedAt = p.CreatedAt
                })
                .ToList();

            return new InvoicePaymentSummaryDto
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                Balance = invoice.Balance,
                Status = invoice.Status,
                Payments = payments
            };
        }

        public async Task<List<InvoicePaymentDto>> GetAllPaymentsAsync(int invoiceId)
        {
            // Enforce strict branch isolation - cannot view payments without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return new List<InvoicePaymentDto>();
            }

            // Verify invoice belongs to user's branch
            var invoiceExists = await _context.Invoices
                .Include(i => i.Order)
                .AnyAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);

            if (!invoiceExists)
            {
                return new List<InvoicePaymentDto>();
            }

            var payments = await _context.InvoicePayments
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(p => new InvoicePaymentDto
            {
                PaymentId = p.PaymentId,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                PaymentDate = p.PaymentDate,
                ReceivedBy = p.ReceivedBy,
                TransactionReference = p.TransactionReference,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<bool> UpdateDueDateAsync(int invoiceId, DateTime dueDate)
        {
            // Enforce strict branch isolation - cannot update without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var invoice = await _context.Invoices
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Order != null && i.Order.BranchId == _tenantContext.BranchId.Value);
            
            if (invoice == null)
                return false;
            
            invoice.DueDate = dueDate;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CreditNoteDto> CreateCreditNoteInvoiceAsync(int returnId)
        {
            // Enforce strict branch isolation - cannot create credit note without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create credit note without branch context.");
            }

            var returnEntity = await _context.Returns
                .Include(r => r.Invoice)
                    .ThenInclude(i => i!.Order)
                        .ThenInclude(o => o.Customer)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId && r.BranchId == _tenantContext.BranchId.Value);

            if (returnEntity == null)
                throw new ArgumentException("Return not found in your branch");

            if (returnEntity.CreditNoteInvoiceId != null)
                throw new InvalidOperationException("Credit note already exists for this return");

            // Generate credit note number
            var creditNoteNumber = await GenerateCreditNoteNumberAsync();

            // Create credit note invoice
            var creditNote = new Invoice
            {
                OrderId = returnEntity.OrderId,
                InvoiceNumber = creditNoteNumber,
                InvoiceType = "CreditNote",
                OriginalInvoiceId = returnEntity.InvoiceId,
                ReturnId = returnId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow,
                Status = "Issued",
                TotalAmount = -returnEntity.TotalReturnAmount, // Negative amount for credit
                AmountPaid = 0,
                Balance = -returnEntity.TotalReturnAmount
            };

            _context.Invoices.Add(creditNote);
            await _context.SaveChangesAsync();

            // Update return with credit note ID
            returnEntity.CreditNoteInvoiceId = creditNote.InvoiceId;
            await _context.SaveChangesAsync();

            // Build credit note DTO
            var customer = returnEntity.Invoice?.Order?.Customer;
            var creditNoteDto = new CreditNoteDto
            {
                CreditNoteId = creditNote.InvoiceId,
                CreditNoteNumber = creditNote.InvoiceNumber,
                CreditNoteDate = creditNote.InvoiceDate,
                CreditAmount = returnEntity.TotalReturnAmount,
                OriginalInvoiceId = returnEntity.InvoiceId,
                OriginalInvoiceNumber = returnEntity.Invoice?.InvoiceNumber ?? "",
                ReturnId = returnId,
                CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "",
                CustomerPhone = customer?.Phone ?? "",
                CustomerEmail = customer?.Email ?? "",
                ReturnReason = returnEntity.ReturnReason,
                Items = returnEntity.ReturnItems.Select(ri => new InvoiceItemDto
                {
                    ProductId = ri.ProductId,
                    ProductName = ri.Product?.ProductName ?? "Unknown",
                    Quantity = ri.ReturnQuantity,
                    UnitPrice = ri.ReturnAmount / ri.ReturnQuantity,
                    TotalPrice = ri.ReturnAmount
                }).ToList()
            };

            return creditNoteDto;
        }

        public async Task<CreditNoteDto?> GetCreditNoteByReturnIdAsync(int returnId)
        {
            // Enforce strict branch isolation - cannot view credit note without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var returnEntity = await _context.Returns
                .Include(r => r.CreditNoteInvoice)
                .Include(r => r.Invoice)
                    .ThenInclude(i => i!.Order)
                        .ThenInclude(o => o.Customer)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId && r.BranchId == _tenantContext.BranchId.Value);

            if (returnEntity == null || returnEntity.CreditNoteInvoice == null)
                return null;

            var creditNote = returnEntity.CreditNoteInvoice;
            var customer = returnEntity.Invoice?.Order?.Customer;

            return new CreditNoteDto
            {
                CreditNoteId = creditNote.InvoiceId,
                CreditNoteNumber = creditNote.InvoiceNumber,
                CreditNoteDate = creditNote.InvoiceDate,
                CreditAmount = returnEntity.TotalReturnAmount,
                OriginalInvoiceId = returnEntity.InvoiceId,
                OriginalInvoiceNumber = returnEntity.Invoice?.InvoiceNumber ?? "",
                ReturnId = returnId,
                CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "",
                CustomerPhone = customer?.Phone ?? "",
                CustomerEmail = customer?.Email ?? "",
                ReturnReason = returnEntity.ReturnReason,
                Items = returnEntity.ReturnItems.Select(ri => new InvoiceItemDto
                {
                    ProductId = ri.ProductId,
                    ProductName = ri.Product?.ProductName ?? "Unknown",
                    Quantity = ri.ReturnQuantity,
                    UnitPrice = ri.ReturnAmount / ri.ReturnQuantity,
                    TotalPrice = ri.ReturnAmount
                }).ToList()
            };
        }

        private async Task<string> GenerateCreditNoteNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var prefix = $"CN-{year}{month:D2}";

            var lastCreditNote = await _context.Invoices
                .Where(i => i.InvoiceType == "CreditNote" && i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastCreditNote != null)
            {
                var parts = lastCreditNote.InvoiceNumber.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"{prefix}-{sequence:D4}";
        }
    }
}

