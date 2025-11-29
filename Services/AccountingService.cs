using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class AccountingService : IAccountingService
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantContext _tenantContext;

        public AccountingService(ApplicationDbContext context, TenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
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
            // Enforce strict branch isolation - no branchId means no data
            if (!_tenantContext.BranchId.HasValue)
            {
                return new AccountingEntriesResponseDto
                {
                    Entries = new List<AccountingEntryDto>(),
                    Pagination = new PaginationDto
                    {
                        Total = 0,
                        Page = page,
                        Limit = limit,
                        TotalPages = 0
                    }
                };
            }

            var query = _context.AccountingEntries
                .Where(e => e.BranchId == _tenantContext.BranchId.Value);

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
            // Enforce strict branch isolation - cannot create without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create accounting entry without branch context. User must be assigned to a branch.");
            }

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
                UpdatedAt = DateTime.UtcNow,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
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
            // Enforce strict branch isolation - cannot delete without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var entry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryId == entryId && e.BranchId == _tenantContext.BranchId.Value);
            
            if (entry == null)
                return false;

            _context.AccountingEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Enforce strict branch isolation - no branchId means return empty summary
            if (!_tenantContext.BranchId.HasValue)
            {
                return new FinancialSummaryDto
                {
                    TotalIncome = 0,
                    TotalExpenses = 0,
                    TotalRefunds = 0,
                    NetProfit = 0,
                    TotalSales = 0,
                    TotalPurchases = 0,
                    CashBalance = 0,
                    Period = "No Data"
                };
            }

            // Get accounting entries for income/expenses/refunds
            var accountingQuery = _context.AccountingEntries
                .Where(e => e.BranchId == _tenantContext.BranchId.Value);

            if (startDate.HasValue)
                accountingQuery = accountingQuery.Where(e => e.EntryDate >= startDate.Value);

            if (endDate.HasValue)
                accountingQuery = accountingQuery.Where(e => e.EntryDate <= endDate.Value);

            // Total Income = Actual cash received (from Income and Sale entries in accounting)
            var totalIncome = await accountingQuery
                .Where(e => e.EntryType == EntryType.Income || e.EntryType == EntryType.Sale)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;
            var cashInHand = await accountingQuery
                .Where(e => e.EntryType == EntryType.Income)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;
            var totalExpenses = await accountingQuery
                .Where(e => e.EntryType == EntryType.Expense)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalRefunds = await accountingQuery
                .Where(e => e.EntryType == EntryType.Refund)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var totalPurchases = await accountingQuery
                .Where(e => e.EntryType == EntryType.Purchase)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            // Total Sales = Total order amounts (includes pending/partial payments)
            var ordersQuery = _context.Orders
                .Where(o => o.BranchId == _tenantContext.BranchId.Value &&
                           (o.OrderStatus == "Completed" || o.OrderStatus == "Paid" || o.OrderStatus == "PartiallyPaid"));

            if (startDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Date >= startDate.Value);

            if (endDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Date <= endDate.Value);

            var totalSales = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Cash Balance = Actual cash on hand (Income received - Expenses - Refunds)
            var cashBalance = cashInHand - totalExpenses - totalRefunds;

            // Net Profit = Business profitability based on total sales (Sales - Expenses - Refunds)
            var netProfit = totalSales - totalRefunds;

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
                TotalIncome = totalIncome,        // Actual cash received
                TotalExpenses = totalExpenses,    // Actual expenses
                TotalRefunds = totalRefunds,      // Actual refunds
                NetProfit = netProfit,            // Income - Expenses - Refunds
                TotalSales = totalSales,          // Total order amounts (includes pending)
                TotalPurchases = totalPurchases,  // Purchases from accounting
                CashBalance = cashBalance,        // Actual cash on hand
                Period = period
            };
        }

        public async Task<List<DailySalesDto>> GetDailySalesAsync(int days = 7)
        {
            // Enforce strict branch isolation - no branchId means return empty list
            if (!_tenantContext.BranchId.HasValue)
            {
                return new List<DailySalesDto>();
            }

            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            // Get order count from Orders table
            var ordersQuery = _context.Orders
                .Where(o => o.Date >= startDate && (o.OrderStatus == "Completed" || o.OrderStatus == "Paid" || o.OrderStatus == "PartiallyPaid") &&
                       o.BranchId == _tenantContext.BranchId.Value);

            var ordersData = await ordersQuery
                .GroupBy(o => o.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalOrders = g.Count(),
                    TotalSalesAmount = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            // Get actual cash received from AccountingEntries (Income only - partial payments)
            var incomeQuery = _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && 
                       e.EntryType == EntryType.Income &&
                       e.BranchId == _tenantContext.BranchId.Value);

            var incomeData = await incomeQuery
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalIncome = g.Sum(e => e.Amount),
                    CashIncome = g.Where(e => e.PaymentMethod == "Cash").Sum(e => e.Amount),
                    CardIncome = g.Where(e => e.PaymentMethod != "Cash" && e.PaymentMethod != null).Sum(e => e.Amount)
                })
                .ToListAsync();

            var expensesQuery = _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryType == EntryType.Expense &&
                       e.BranchId == _tenantContext.BranchId.Value);

            var expensesData = await expensesQuery
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalExpenses = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            var refundsQuery = _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryType == EntryType.Refund &&
                       e.BranchId == _tenantContext.BranchId.Value);

            var refundsData = await refundsQuery
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
                var orders = ordersData.FirstOrDefault(o => o.Date == date);
                var income = incomeData.FirstOrDefault(s => s.Date == date);
                var expenses = expensesData.FirstOrDefault(e => e.Date == date);
                var refunds = refundsData.FirstOrDefault(r => r.Date == date);

                var totalSales = orders?.TotalSalesAmount ?? 0;
                var totalIncome = income?.TotalIncome ?? 0;
                var totalExpenses = expenses?.TotalExpenses ?? 0;
                var totalRefunds = refunds?.TotalRefunds ?? 0;
                var totalOrders = orders?.TotalOrders ?? 0;

                result.Add(new DailySalesDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    TotalSales = totalSales,  // Total order amounts (includes pending)
                    TotalOrders = totalOrders,
                    TotalExpenses = totalExpenses,
                    TotalRefunds = totalRefunds,
                    NetProfit = totalSales - totalExpenses - totalRefunds,  // Based on total sales
                    CashSales = income?.CashIncome ?? 0,  // Actual cash received
                    CardSales = income?.CardIncome ?? 0,  // Actual card received
                    AverageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0
                });
            }

            return result.OrderBy(r => r.Date).ToList();
        }

        public async Task<SalesGraphDto> GetSalesGraphAsync(DateTime startDate, DateTime endDate)
        {
            // Enforce strict branch isolation - no branchId means return empty graph
            if (!_tenantContext.BranchId.HasValue)
            {
                return new SalesGraphDto();
            }

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

            // Get sales data from Orders (includes all orders - pending/partial)
            var salesQueryGraph = _context.Orders
                .Where(o => o.Date >= startDate && o.Date <= endDate.AddDays(1).AddTicks(-1) && 
                       (o.OrderStatus == "Completed" || o.OrderStatus == "Paid" || o.OrderStatus == "PartiallyPaid") &&
                       o.BranchId == _tenantContext.BranchId.Value);

            var salesData = await salesQueryGraph
                .GroupBy(o => o.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    TotalOrders = g.Count()
                })
                .ToDictionaryAsync(x => x.Date, x => x);

            // Get expenses data
            var expensesQueryGraph = _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate.AddDays(1).AddTicks(-1) && 
                       e.EntryType == EntryType.Expense &&
                       e.BranchId == _tenantContext.BranchId.Value);

            var expensesData = await expensesQueryGraph
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalExpenses = g.Sum(e => e.Amount)
                })
                .ToDictionaryAsync(x => x.Date, x => x.TotalExpenses);

            // Get refunds data
            var refundsQueryGraph = _context.AccountingEntries
                .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate.AddDays(1).AddTicks(-1) && 
                       e.EntryType == EntryType.Refund &&
                       e.BranchId == _tenantContext.BranchId.Value);

            var refundsData = await refundsQueryGraph
                .GroupBy(e => e.EntryDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRefunds = g.Sum(e => e.Amount)
                })
                .ToDictionaryAsync(x => x.Date, x => x.TotalRefunds);

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

                // Get refunds for this day (0 if no refunds)
                var dailyRefunds = refundsData.ContainsKey(date) ? refundsData[date] : 0m;
                result.RefundsData.Add(dailyRefunds);

                // Calculate profit (sales - expenses - refunds)
                result.ProfitData.Add(dailySales - dailyExpenses - dailyRefunds);

                // Get order count for this day (0 if no orders)
                var dailyOrders = salesData.ContainsKey(date) ? salesData[date].TotalOrders : 0;
                result.OrdersData.Add(dailyOrders);
            }

            return result;
        }

        public async Task<List<PaymentMethodSummaryDto>> GetPaymentMethodsSummaryAsync(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            // Enforce strict branch isolation - no branchId means return empty list
            if (!_tenantContext.BranchId.HasValue)
            {
                return new List<PaymentMethodSummaryDto>();
            }

            var query = _context.Orders
                .Where(o => (o.OrderStatus == "Completed" || o.OrderStatus == "Paid" || o.OrderStatus == "PartiallyPaid") &&
                       o.BranchId == _tenantContext.BranchId.Value);

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
            // Enforce strict branch isolation - no branchId means return empty list
            if (!_tenantContext.BranchId.HasValue)
            {
                return new List<TopProductDto>();
            }

            var query = _context.OrderProductMaps
                .Include(opm => opm.Order)
                .Include(opm => opm.Product)
                .Where(opm => (opm.Order.OrderStatus == "Completed" || opm.Order.OrderStatus == "Paid" || opm.Order.OrderStatus == "PartiallyPaid") &&
                       opm.Order.BranchId == _tenantContext.BranchId.Value);

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

            // Verify order belongs to current branch (if branch isolation is active)
            if (order.BranchId != _tenantContext.BranchId)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot create sale entry: Order {orderId} belongs to branch {order.BranchId}, but current context is branch {_tenantContext.BranchId}");
                return;
            }

            // Check if entry already exists for THIS BRANCH to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Sale && 
                                         e.Description.Contains($"Order #{orderId}") &&
                                         e.BranchId == order.BranchId);
            if (existingEntry != null)
            {
                System.Diagnostics.Debug.WriteLine($"Sale entry already exists for Order {orderId} in branch {order.BranchId}");
                return;
            }

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
                UpdatedAt = DateTime.UtcNow,
                CompanyId = order.CompanyId,
                BranchId = order.BranchId
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"Created sale accounting entry for Order {orderId} in branch {order.BranchId}, Amount: {order.TotalAmount}");
        }

        public async Task CreateRefundEntryFromOrderAsync(int orderId, string createdBy)
        {
            var order = await _context.Orders
                .Include(o => o.OrderProductMaps)
                    .ThenInclude(opm => opm.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return;

            // Verify order belongs to current branch (if branch isolation is active)
            if (order.BranchId != _tenantContext.BranchId)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot create refund entry: Order {orderId} belongs to branch {order.BranchId}, but current context is branch {_tenantContext.BranchId}");
                return;
            }

            // Check if entry already exists for THIS BRANCH to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Refund && 
                                         e.Description.Contains($"Refund for Order #{orderId}") &&
                                         e.BranchId == order.BranchId);
            if (existingEntry != null)
            {
                System.Diagnostics.Debug.WriteLine($"Refund entry already exists for Order {orderId} in branch {order.BranchId}");
                return;
            }

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
                UpdatedAt = DateTime.UtcNow,
                CompanyId = order.CompanyId,
                BranchId = order.BranchId
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"Created refund accounting entry for Order {orderId} in branch {order.BranchId}, Amount: {order.TotalAmount}");
        }

        public async Task CreateExpenseEntryAsync(int expenseId, string createdBy)
        {
            // Enforce strict branch isolation - cannot create without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create expense entry without branch context.");
            }

            var expense = await _context.Expenses.FindAsync(expenseId);
            if (expense == null)
                return;

            // Verify expense belongs to current branch
            if (expense.BranchId != _tenantContext.BranchId.Value)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot create expense entry: Expense {expenseId} belongs to branch {expense.BranchId}, but current context is branch {_tenantContext.BranchId}");
                return;
            }

            // Check if entry already exists for THIS BRANCH to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Expense && 
                                         e.Description.Contains($"Expense #{expenseId}") &&
                                         e.BranchId == expense.BranchId);
            if (existingEntry != null)
            {
                System.Diagnostics.Debug.WriteLine($"Expense entry already exists for Expense {expenseId} in branch {expense.BranchId}");
                return;
            }

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
                UpdatedAt = DateTime.UtcNow,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"Created expense accounting entry for Expense {expenseId} in branch {expense.BranchId}, Amount: {expense.ExpenseAmount}");
        }

        public async Task CreatePaymentEntryAsync(int invoiceId, int paymentId, string createdBy)
        {
            // Enforce strict branch isolation - cannot create without branchId
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create payment entry without branch context.");
            }

            var payment = await _context.InvoicePayments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Order)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null || payment.Invoice == null || payment.Invoice.Order == null)
                return;

            var order = payment.Invoice.Order;

            // Verify payment belongs to current branch
            if (order.BranchId != _tenantContext.BranchId.Value)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot create payment entry: Payment {paymentId} for Invoice {invoiceId} belongs to branch {order.BranchId}, but current context is branch {_tenantContext.BranchId}");
                return;
            }

            // Check if entry already exists for THIS PAYMENT to avoid duplicates
            var existingEntry = await _context.AccountingEntries
                .FirstOrDefaultAsync(e => e.EntryType == EntryType.Income && 
                                         e.Description.Contains($"Payment #{paymentId}") &&
                                         e.Description.Contains($"Invoice #{payment.Invoice.InvoiceNumber}") &&
                                         e.BranchId == order.BranchId);
            if (existingEntry != null)
            {
                System.Diagnostics.Debug.WriteLine($"Payment entry already exists for Payment {paymentId} on Invoice {payment.Invoice.InvoiceNumber} in branch {order.BranchId}");
                return;
            }

            // Determine if this is a partial or full payment
            var paymentType = payment.Invoice.Balance == 0 ? "Full Payment" : "Partial Payment";

            var entry = new AccountingEntry
            {
                EntryType = EntryType.Income,
                Amount = payment.Amount,
                Description = $"{paymentType} #{paymentId} - Invoice #{payment.Invoice.InvoiceNumber} (Order #{order.OrderId})",
                PaymentMethod = payment.PaymentMethod,
                Category = "Payment Received",
                EntryDate = payment.PaymentDate,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };

            _context.AccountingEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"Created {paymentType.ToLower()} accounting entry for Payment {paymentId} on Invoice {payment.Invoice.InvoiceNumber} in branch {order.BranchId}, Amount: {payment.Amount}");
        }
    }
}
