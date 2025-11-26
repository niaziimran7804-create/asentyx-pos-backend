using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;

namespace POS.Api.Services
{
    public class AccountingService : IAccountingService
    {
        private readonly ApplicationDbContext _context;

        public AccountingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccountingEntriesResponseDto> GetAccountingEntriesAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? entryType = null,
            string? paymentMethod = null,
            string? category = null,
            int page = 1,
            int limit = 50)
        {
            var query = _context.AccountingEntries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.EntryDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EntryDate <= endDate.Value);

            if (!string.IsNullOrEmpty(entryType))
            {
                if (Enum.TryParse<EntryType>(entryType, true, out var parsedType))
                    query = query.Where(e => e.EntryType == parsedType);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(e => e.PaymentMethod == paymentMethod);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)limit);

            var entries = await query
                .OrderByDescending(e => e.EntryDate)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(e => new AccountingEntryDto
                {
                    EntryId = e.EntryId,
                    EntryType = e.EntryType.ToString(),
                    Amount = e.Amount,
                    Description = e.Description,
                    PaymentMethod = e.PaymentMethod,
                    Category = e.Category,
                    EntryDate = e.EntryDate,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .ToListAsync();

            return new AccountingEntriesResponseDto
            {
                Entries = entries,
                Pagination = new PaginationDto
                {
                    Total = total,
                    Page = page,
                    Limit = limit,
                    TotalPages = totalPages
                }
            };
        }

        public async Task<AccountingEntryDto> CreateAccountingEntryAsync(CreateAccountingEntryDto dto, string createdBy)
        {
            if (!Enum.TryParse<EntryType>(dto.EntryType, true, out var entryType))
                throw new ArgumentException("Invalid entry type");

            if (dto.Amount <= 0)
                throw new ArgumentException("Amount must be positive");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 3)
                throw new ArgumentException("Description must be at least 3 characters");

            if (dto.EntryDate > DateTime.UtcNow)
                throw new ArgumentException("Entry date cannot be in the future");

            var entry = new AccountingEntry
            {
                EntryType = entryType,
                Amount = dto.Amount,
                Description = dto.Description,
                PaymentMethod = dto.PaymentMethod,
                Category = dto.Category,
                EntryDate = dto.EntryDate,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();

            return new AccountingEntryDto
            {
                EntryId = entry.EntryId,
                EntryType = entry.EntryType.ToString(),
                Amount = entry.Amount,
                Description = entry.Description,
                PaymentMethod = entry.PaymentMethod,
                Category = entry.Category,
                EntryDate = entry.EntryDate,
                CreatedBy = entry.CreatedBy,
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt
            };
        }

        public async Task<bool> DeleteAccountingEntryAsync(int entryId)
        {
            var entry = await _context.AccountingEntries.FindAsync(entryId);
            if (entry == null)
                return false;

            _context.AccountingEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AccountingEntries.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.EntryDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EntryDate <= endDate.Value);

            var totalIncome = await query
                .Where(e => e.EntryType == EntryType.Income || e.EntryType == EntryType.Sale)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalExpenses = await query
                .Where(e => e.EntryType == EntryType.Expense)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalRefunds = await query
                .Where(e => e.EntryType == EntryType.Refund)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalSales = await query
                .Where(e => e.EntryType == EntryType.Sale)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalPurchases = await query
                .Where(e => e.EntryType == EntryType.Purchase)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            // Net profit = Income - Expenses - Refunds
            var netProfit = totalIncome - totalExpenses - totalRefunds;

            var period = "All Time";
            if (startDate.HasValue && endDate.HasValue)
            {
                period = $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}";
            }
            else if (startDate.HasValue)
            {
                period = $"From {startDate.Value:yyyy-MM-dd}";
            }
            else if (endDate.HasValue)
            {
                period = $"Until {endDate.Value:yyyy-MM-dd}";
            }

            return new FinancialSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetProfit = netProfit,
                TotalSales = totalSales,
                TotalPurchases = totalPurchases,
                CashBalance = netProfit,
                Period = period
            };
        }

        public async Task<List<DailySalesDto>> GetDailySalesAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var salesData = await _context.Orders
                .Where(o => o.Date >= startDate && (o.OrderStatus == "Completed" || o.OrderStatus == "Paid"))
                .GroupBy(o => o.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    TotalOrders = g.Count(),
                    CashSales = g.Where(o => o.PaymentMethod == "Cash").Sum(o => o.TotalAmount),
                    CardSales = g.Where(o => o.PaymentMethod != "Cash").Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var expensesData = await _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryType == EntryType.Expense)
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalExpenses = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            var refundsData = await _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryType == EntryType.Refund)
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRefunds = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            var result = new List<DailySalesDto>();

            for (int i = 0; i < days; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var sales = salesData.FirstOrDefault(s => s.Date == date);
                var expenses = expensesData.FirstOrDefault(e => e.Date == date);
                var refunds = refundsData.FirstOrDefault(r => r.Date == date);

                var totalSales = sales?.TotalSales ?? 0;
                var totalExpenses = expenses?.TotalExpenses ?? 0;
                var totalRefunds = refunds?.TotalRefunds ?? 0;
                var totalOrders = sales?.TotalOrders ?? 0;

                result.Add(new DailySalesDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    TotalSales = totalSales,
                    TotalOrders = totalOrders,
                    TotalExpenses = totalExpenses,
                    NetProfit = totalSales - totalExpenses - totalRefunds,
                    CashSales = sales?.CashSales ?? 0,
                    CardSales = sales?.CardSales ?? 0,
                    AverageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0
                });
            }

            return result.OrderBy(r => r.Date).ToList();
        }

        public async Task<SalesGraphDto> GetSalesGraphAsync(DateTime startDate, DateTime endDate)
        {
            // Validate date range
            if (startDate > endDate)
                throw new ArgumentException("startDate cannot be after endDate");

            if ((endDate - startDate).TotalDays > 90)
                throw new ArgumentException("Date range cannot exceed 90 days");

            // Normalize dates to start and end of day
            startDate = startDate.Date;
            endDate = endDate.Date;

            // Generate all dates in the range (including days with no data)
            var dateRange = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateRange.Add(date);
            }

            // Get sales data
            var salesData = await _context.Orders
                .Where(o => o.Date >= startDate && o.Date <= endDate.AddDays(1).AddTicks(-1) && 
                       (o.OrderStatus == "Completed" || o.OrderStatus == "Paid"))
                .GroupBy(o => o.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    TotalOrders = g.Count()
                })
                .ToDictionaryAsync(x => x.Date, x => x);

            // Get expenses data
            var expensesData = await _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate.AddDays(1).AddTicks(-1) && 
                       e.EntryType == EntryType.Expense)
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalExpenses = g.Sum(e => e.Amount)
                })
                .ToDictionaryAsync(x => x.Date, x => x.TotalExpenses);

            // Build result with all dates in range
            var result = new SalesGraphDto();

            foreach (var date in dateRange)
            {
                // Format date label as "MMM dd"
                result.Labels.Add(date.ToString("MMM dd"));

                // Get sales for this day (0 if no sales)
                var dailySales = salesData.ContainsKey(date) ? salesData[date].TotalSales : 0m;
                result.SalesData.Add(dailySales);

                // Get expenses for this day (0 if no expenses)
                var dailyExpenses = expensesData.ContainsKey(date) ? expensesData[date] : 0m;
                result.ExpensesData.Add(dailyExpenses);

                // Calculate profit (sales - expenses)
                result.ProfitData.Add(dailySales - dailyExpenses);

                // Get order count for this day (0 if no orders)
                var dailyOrders = salesData.ContainsKey(date) ? salesData[date].TotalOrders : 0;
                result.OrdersData.Add(dailyOrders);
            }

            return result;
        }

        public async Task<List<PaymentMethodSummaryDto>> GetPaymentMethodsSummaryAsync(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Paid");

            if (startDate.HasValue)
                query = query.Where(o => o.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.Date <= endDate.Value);

            var totalAmount = await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var paymentMethods = await query
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new PaymentMethodSummaryDto
                {
                    PaymentMethod = g.Key,
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    TransactionCount = g.Count(),
                    Percentage = totalAmount > 0 ? (g.Sum(o => o.TotalAmount) / totalAmount) * 100 : 0
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToListAsync();

            return paymentMethods;
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(
            int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.OrderProductMaps
                .Include(opm => opm.Order)
                .Include(opm => opm.Product)
                .Where(opm => opm.Order.OrderStatus == "Completed" || opm.Order.OrderStatus == "Paid");

            if (startDate.HasValue)
                query = query.Where(opm => opm.Order.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(opm => opm.Order.Date <= endDate.Value);

            var topProducts = await query
                .GroupBy(opm => new { opm.ProductId, opm.Product.ProductName })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(opm => opm.Quantity),
                    TotalRevenue = g.Sum(opm => opm.TotalPrice),
                    OrderCount = g.Select(opm => opm.OrderId).Distinct().Count()
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(limit)
                .ToListAsync();

            return topProducts;
        }

        public async Task CreateSaleEntryFromOrderAsync(int orderId, string createdBy)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return;

            // Check if entry already exists to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Sale && 
                                         e.Description.Contains($"Order #{orderId}"));
            if (existingEntry != null)
                return;

            var entry = new AccountingEntry
            {
                EntryType = EntryType.Sale,
                Amount = order.TotalAmount,
                Description = $"Order #{orderId}",
                PaymentMethod = order.PaymentMethod,
                Category = "Sales",
                EntryDate = order.Date,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task CreateRefundEntryFromOrderAsync(int orderId, string createdBy)
        {
            var order = await _context.Orders
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return;

            var productNames = order.OrderProductMaps.Any() 
                ? string.Join(", ", order.OrderProductMaps.Select(opm => opm.Product?.ProductName ?? "Unknown"))
                : "Products";

            var entry = new AccountingEntry
            {
                EntryType = EntryType.Refund,
                Amount = order.TotalAmount,
                Description = $"Refund for Order #{orderId} - {productNames}",
                PaymentMethod = order.PaymentMethod,
                Category = "Sales Refund",
                EntryDate = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task CreateExpenseEntryAsync(int expenseId, string createdBy)
        {
            var expense = await _context.Expenses.FindAsync(expenseId);
            if (expense == null)
                return;

            // Check if entry already exists to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Expense && 
                                         e.Description.Contains($"Expense #{expenseId}"));
            if (existingEntry != null)
                return;

            var entry = new AccountingEntry
            {
                EntryType = EntryType.Expense,
                Amount = expense.ExpenseAmount,
                Description = $"Expense #{expenseId} - {expense.ExpenseName}",
                PaymentMethod = "Cash", // Default
                Category = "General Expense",
                EntryDate = expense.ExpenseDate,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}
