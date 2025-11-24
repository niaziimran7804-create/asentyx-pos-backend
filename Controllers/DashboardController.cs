using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            var today = DateTime.UtcNow.Date;
            var last7Days = today.AddDays(-7);
            var last30Days = today.AddDays(-30);

            var stats = new DashboardStatsDto
            {
                // Expense Statistics
                ExpenseToday = await _context.Expenses
                    .Where(e => e.ExpenseDate.Date == today)
                    .SumAsync(e => e.ExpenseAmount),
                ExpenseLast7Days = await _context.Expenses
                    .Where(e => e.ExpenseDate >= last7Days)
                    .SumAsync(e => e.ExpenseAmount),
                ExpenseLast30Days = await _context.Expenses
                    .Where(e => e.ExpenseDate >= last30Days)
                    .SumAsync(e => e.ExpenseAmount),
                ExpenseTotal = await _context.Expenses
                    .SumAsync(e => e.ExpenseAmount),

                // Sales Statistics
                SalesToday = await _context.Orders
                    .Where(o => o.Date.Date == today)
                    .SumAsync(o => o.TotalAmount),
                SalesLast7Days = await _context.Orders
                    .Where(o => o.Date >= last7Days)
                    .SumAsync(o => o.TotalAmount),
                SalesLast30Days = await _context.Orders
                    .Where(o => o.Date >= last30Days)
                    .SumAsync(o => o.TotalAmount),
                SalesTotal = await _context.Orders
                    .SumAsync(o => o.TotalAmount),

                // User Statistics
                TotalUsers = await _context.Users.CountAsync(),
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin"),
                TotalCashiers = await _context.Users.CountAsync(u => u.Role == "Cashier"),
                TotalSalesmen = await _context.Users.CountAsync(u => u.Role == "Salesman"),

                // Product Statistics
                TotalProducts = await _context.Products.CountAsync(),
                AvailableProducts = await _context.Products.CountAsync(p => p.ProductStatus == "YES" && p.ProductUnitStock > 0),
                UnavailableProducts = await _context.Products.CountAsync(p => p.ProductStatus != "YES" || p.ProductUnitStock == 0),

                // Salary Statistics
                AdminSalaryTotal = await _context.Users
                    .Where(u => u.Role == "Admin")
                    .SumAsync(u => u.Salary),
                CashierSalaryTotal = await _context.Users
                    .Where(u => u.Role == "Cashier")
                    .SumAsync(u => u.Salary),
                SalesmanSalaryTotal = await _context.Users
                    .Where(u => u.Role == "Salesman")
                    .SumAsync(u => u.Salary),
                TotalSalary = await _context.Users
                    .SumAsync(u => u.Salary),

                // Category Statistics
                TotalMainCategories = await _context.MainCategories.CountAsync(),
                TotalSecondCategories = await _context.SecondCategories.CountAsync(),
                TotalThirdCategories = await _context.ThirdCategories.CountAsync(),
                TotalVendors = await _context.Vendors.CountAsync(),
                TotalBrands = await _context.Brands.CountAsync()
            };

            return Ok(stats);
        }
    }

    public class DashboardStatsDto
    {
        // Expenses
        public decimal ExpenseToday { get; set; }
        public decimal ExpenseLast7Days { get; set; }
        public decimal ExpenseLast30Days { get; set; }
        public decimal ExpenseTotal { get; set; }

        // Sales
        public decimal SalesToday { get; set; }
        public decimal SalesLast7Days { get; set; }
        public decimal SalesLast30Days { get; set; }
        public decimal SalesTotal { get; set; }

        // Users
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalCashiers { get; set; }
        public int TotalSalesmen { get; set; }

        // Products
        public int TotalProducts { get; set; }
        public int AvailableProducts { get; set; }
        public int UnavailableProducts { get; set; }

        // Salaries
        public decimal AdminSalaryTotal { get; set; }
        public decimal CashierSalaryTotal { get; set; }
        public decimal SalesmanSalaryTotal { get; set; }
        public decimal TotalSalary { get; set; }

        // Categories
        public int TotalMainCategories { get; set; }
        public int TotalSecondCategories { get; set; }
        public int TotalThirdCategories { get; set; }
        public int TotalVendors { get; set; }
        public int TotalBrands { get; set; }
    }
}

