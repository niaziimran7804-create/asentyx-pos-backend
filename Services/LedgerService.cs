using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;

namespace POS.Api.Services
{
    public class LedgerService : ILedgerService
    {
        private readonly ApplicationDbContext _context;

        public LedgerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerLedgerDto> CreateLedgerEntryAsync(CreateLedgerEntryDto dto, string createdBy)
        {
            // Get current balance
            var currentBalance = await GetCustomerBalanceAsync(dto.CustomerId);

            // Calculate new balance
            var newBalance = currentBalance + dto.DebitAmount - dto.CreditAmount;

            var entry = new CustomerLedger
            {
                CustomerId = dto.CustomerId,
                TransactionDate = dto.TransactionDate,
                TransactionType = dto.TransactionType,
                Description = dto.Description,
                InvoiceId = dto.InvoiceId,
                OrderId = dto.OrderId,
                ReturnId = dto.ReturnId,
                DebitAmount = dto.DebitAmount,
                CreditAmount = dto.CreditAmount,
                Balance = newBalance,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNumber = dto.ReferenceNumber,
                Notes = dto.Notes,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerLedgers.Add(entry);
            await _context.SaveChangesAsync();

            return await GetLedgerEntryByIdAsync(entry.LedgerId);
        }

        public async Task<List<CustomerLedgerDto>> GetCustomerLedgerAsync(int customerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.CustomerLedgers
                .Include(l => l.Customer)
                .Where(l => l.CustomerId == customerId);

            if (startDate.HasValue)
                query = query.Where(l => l.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.TransactionDate <= endDate.Value);

            var entries = await query
                .OrderBy(l => l.TransactionDate)
                .ThenBy(l => l.LedgerId)
                .ToListAsync();

            return entries.Select(MapToDto).ToList();
        }

        public async Task<CustomerStatementDto> GetCustomerStatementAsync(int customerId, DateTime startDate, DateTime endDate)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == "Customer");

            if (customer == null)
                throw new ArgumentException("Customer not found");

            // Get opening balance (balance before start date)
            var openingBalance = await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId && l.TransactionDate < startDate)
                .OrderByDescending(l => l.TransactionDate)
                .ThenByDescending(l => l.LedgerId)
                .Select(l => l.Balance)
                .FirstOrDefaultAsync();

            // Get transactions in period
            var transactions = await GetCustomerLedgerAsync(customerId, startDate, endDate);

            var totalDebits = transactions.Sum(t => t.DebitAmount);
            var totalCredits = transactions.Sum(t => t.CreditAmount);
            var closingBalance = transactions.LastOrDefault()?.Balance ?? openingBalance;

            return new CustomerStatementDto
            {
                CustomerId = customerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                CustomerPhone = customer.Phone,
                CustomerEmail = customer.Email,
                CustomerAddress = customer.CurrentCity,
                StatementDate = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                OpeningBalance = openingBalance,
                TotalDebits = totalDebits,
                TotalCredits = totalCredits,
                ClosingBalance = closingBalance,
                Transactions = transactions
            };
        }

        public async Task<decimal> GetCustomerBalanceAsync(int customerId)
        {
            var lastEntry = await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.TransactionDate)
                .ThenByDescending(l => l.LedgerId)
                .FirstOrDefaultAsync();

            return lastEntry?.Balance ?? 0;
        }

        public async Task<LedgerSummaryDto> GetLedgerSummaryAsync(int customerId)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == "Customer");

            if (customer == null)
                throw new ArgumentException("Customer not found");

            var ledgerEntries = await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId)
                .ToListAsync();

            var currentBalance = await GetCustomerBalanceAsync(customerId);

            var totalSales = ledgerEntries
                .Where(l => l.TransactionType == "Sale")
                .Sum(l => l.DebitAmount);

            var totalPayments = ledgerEntries
                .Where(l => l.TransactionType == "Payment")
                .Sum(l => l.CreditAmount);

            var totalRefunds = ledgerEntries
                .Where(l => l.TransactionType == "Refund")
                .Sum(l => l.CreditAmount);

            return new LedgerSummaryDto
            {
                CustomerId = customerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                CurrentBalance = currentBalance,
                TotalSales = totalSales,
                TotalPayments = totalPayments,
                TotalRefunds = totalRefunds,
                TotalTransactions = ledgerEntries.Count,
                FirstTransactionDate = ledgerEntries.OrderBy(l => l.TransactionDate).FirstOrDefault()?.TransactionDate,
                LastTransactionDate = ledgerEntries.OrderByDescending(l => l.TransactionDate).FirstOrDefault()?.TransactionDate,
                CreditLimit = 0, // Can be added to User model
                AvailableCredit = 0 - currentBalance
            };
        }

        public async Task CreateSaleLedgerEntryAsync(int orderId, int invoiceId, string createdBy)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return;

            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                return;

            // Check if entry already exists
            var existingEntry = await _context.CustomerLedgers
                .FirstOrDefaultAsync(l => l.OrderId == orderId && l.InvoiceId == invoiceId);

            if (existingEntry != null)
                return;

            var productNames = string.Join(", ", order.OrderProductMaps.Select(opm => opm.Product?.ProductName ?? "Unknown"));

            var dto = new CreateLedgerEntryDto
            {
                CustomerId = order.CustomerId,
                TransactionDate = order.Date,
                TransactionType = "Sale",
                Description = $"Invoice #{invoice.InvoiceNumber} - Order #{orderId} - {productNames}",
                InvoiceId = invoiceId,
                OrderId = orderId,
                DebitAmount = order.TotalAmount, // Customer owes this amount
                CreditAmount = 0,
                PaymentMethod = order.PaymentMethod,
                ReferenceNumber = invoice.InvoiceNumber,
                Notes = $"Sale transaction for {order.OrderProductMaps.Count} items"
            };

            await CreateLedgerEntryAsync(dto, createdBy);
        }

        public async Task CreatePaymentLedgerEntryAsync(int customerId, decimal amount, string paymentMethod, 
            string referenceNumber, int? invoiceId, string createdBy)
        {
            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null)
                return;

            var description = "Payment received";
            if (invoiceId.HasValue)
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId.Value);
                if (invoice != null)
                    description = $"Payment for Invoice #{invoice.InvoiceNumber}";
            }

            var dto = new CreateLedgerEntryDto
            {
                CustomerId = customerId,
                TransactionDate = DateTime.UtcNow,
                TransactionType = "Payment",
                Description = description,
                InvoiceId = invoiceId,
                DebitAmount = 0,
                CreditAmount = amount, // Customer paid this amount
                PaymentMethod = paymentMethod,
                ReferenceNumber = referenceNumber,
                Notes = $"Payment received via {paymentMethod}"
            };

            await CreateLedgerEntryAsync(dto, createdBy);
        }

        public async Task CreateRefundLedgerEntryAsync(int returnId, string createdBy)
        {
            var returnEntity = await _context.Returns
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.Invoice)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId);

            if (returnEntity == null)
                return;

            // Check if entry already exists
            var existingEntry = await _context.CustomerLedgers
                .FirstOrDefaultAsync(l => l.ReturnId == returnId);

            if (existingEntry != null)
                return;

            var productNames = string.Join(", ", returnEntity.ReturnItems.Select(ri => ri.Product?.ProductName ?? "Unknown"));

            var dto = new CreateLedgerEntryDto
            {
                CustomerId = returnEntity.Order.CustomerId,
                TransactionDate = returnEntity.ReturnDate,
                TransactionType = "Refund",
                Description = $"Return #{returnId} - {returnEntity.ReturnType} return - {productNames}",
                InvoiceId = returnEntity.InvoiceId,
                OrderId = returnEntity.OrderId,
                ReturnId = returnId,
                DebitAmount = 0,
                CreditAmount = returnEntity.TotalReturnAmount, // Credit customer account
                PaymentMethod = returnEntity.RefundMethod,
                ReferenceNumber = $"RET-{returnId}",
                Notes = $"Refund for {returnEntity.ReturnItems.Count} items - {returnEntity.ReturnReason}"
            };

            await CreateLedgerEntryAsync(dto, createdBy);
        }

        public async Task<AgingReportDto> GetAgingReportAsync(DateTime? asOfDate = null)
        {
            var reportDate = asOfDate ?? DateTime.UtcNow.Date;

            // Get all customers with transactions
            var customerIds = await _context.CustomerLedgers
                .Select(l => l.CustomerId)
                .Distinct()
                .ToListAsync();

            var customerAgings = new List<CustomerAgingDto>();

            foreach (var customerId in customerIds)
            {
                var aging = await GetCustomerAgingAsync(customerId, reportDate);
                if (aging.TotalOutstanding > 0) // Only include customers with outstanding balance
                {
                    customerAgings.Add(aging);
                }
            }

            return new AgingReportDto
            {
                ReportDate = DateTime.UtcNow,
                AsOfDate = reportDate,
                Customers = customerAgings.OrderByDescending(c => c.TotalOutstanding).ToList(),
                TotalDays0To30 = customerAgings.Sum(c => c.Days0To30),
                TotalDays31To60 = customerAgings.Sum(c => c.Days31To60),
                TotalDays61To90 = customerAgings.Sum(c => c.Days61To90),
                TotalDays91Plus = customerAgings.Sum(c => c.Days91Plus),
                GrandTotal = customerAgings.Sum(c => c.TotalOutstanding),
                TotalCustomers = customerIds.Count,
                CustomersWithBalance = customerAgings.Count
            };
        }

        public async Task<CustomerAgingDto> GetCustomerAgingAsync(int customerId, DateTime? asOfDate = null)
        {
            var reportDate = asOfDate ?? DateTime.UtcNow.Date;

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == "Customer");

            if (customer == null)
                throw new ArgumentException("Customer not found");

            // Get all unpaid invoices
            var unpaidInvoices = await _context.Invoices
                .Include(i => i.Order)
                .Where(i => i.Order.CustomerId == customerId && i.Balance > 0 && i.DueDate <= reportDate)
                .ToListAsync();

            var aging = new CustomerAgingDto
            {
                CustomerId = customerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                CustomerPhone = customer.Phone ?? string.Empty,
                CustomerEmail = customer.Email ?? string.Empty,
                CustomerAddress = customer.CurrentCity ?? string.Empty,
                CurrentBalance = await GetCustomerBalanceAsync(customerId)
            };

            foreach (var invoice in unpaidInvoices)
            {
                var daysOverdue = (reportDate - invoice.DueDate.Date).Days;
                var balance = invoice.Balance;

                if (daysOverdue <= 30)
                    aging.Days0To30 += balance;
                else if (daysOverdue <= 60)
                    aging.Days31To60 += balance;
                else if (daysOverdue <= 90)
                    aging.Days61To90 += balance;
                else
                    aging.Days91Plus += balance;
            }

            aging.TotalOutstanding = aging.Days0To30 + aging.Days31To60 + aging.Days61To90 + aging.Days91Plus;
            aging.TotalInvoices = unpaidInvoices.Count;
            aging.UnpaidInvoices = unpaidInvoices.Count(i => i.Balance > 0);

            var lastTransaction = await _context.CustomerLedgers
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.TransactionDate)
                .FirstOrDefaultAsync();

            aging.LastTransactionDate = lastTransaction?.TransactionDate;

            return aging;
        }

        public async Task<List<CustomerAgingDto>> GetCustomersWithOutstandingBalanceAsync()
        {
            var customerIds = await _context.CustomerLedgers
                .Select(l => l.CustomerId)
                .Distinct()
                .ToListAsync();

            var result = new List<CustomerAgingDto>();

            foreach (var customerId in customerIds)
            {
                var balance = await GetCustomerBalanceAsync(customerId);
                if (balance > 0)
                {
                    var aging = await GetCustomerAgingAsync(customerId);
                    result.Add(aging);
                }
            }

            return result.OrderByDescending(c => c.TotalOutstanding).ToList();
        }

        public async Task<List<LedgerSummaryDto>> GetAllCustomerSummariesAsync()
        {
            var customerIds = await _context.Users
                .Where(u => u.Role == "Customer")
                .Select(u => u.Id)
                .ToListAsync();

            var summaries = new List<LedgerSummaryDto>();

            foreach (var customerId in customerIds)
            {
                try
                {
                    var summary = await GetLedgerSummaryAsync(customerId);
                    summaries.Add(summary);
                }
                catch
                {
                    // Skip customers with errors
                    continue;
                }
            }

            return summaries.OrderByDescending(s => s.CurrentBalance).ToList();
        }

        private async Task<CustomerLedgerDto> GetLedgerEntryByIdAsync(int ledgerId)
        {
            var entry = await _context.CustomerLedgers
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.LedgerId == ledgerId);

            if (entry == null)
                throw new ArgumentException("Ledger entry not found");

            return MapToDto(entry);
        }

        private static CustomerLedgerDto MapToDto(CustomerLedger entry)
        {
            return new CustomerLedgerDto
            {
                LedgerId = entry.LedgerId,
                CustomerId = entry.CustomerId,
                CustomerName = entry.Customer != null ? $"{entry.Customer.FirstName} {entry.Customer.LastName}" : "",
                TransactionDate = entry.TransactionDate,
                TransactionType = entry.TransactionType,
                Description = entry.Description,
                InvoiceId = entry.InvoiceId,
                OrderId = entry.OrderId,
                ReturnId = entry.ReturnId,
                DebitAmount = entry.DebitAmount,
                CreditAmount = entry.CreditAmount,
                Balance = entry.Balance,
                PaymentMethod = entry.PaymentMethod,
                ReferenceNumber = entry.ReferenceNumber,
                Notes = entry.Notes,
                CreatedBy = entry.CreatedBy,
                CreatedAt = entry.CreatedAt
            };
        }
    }
}
