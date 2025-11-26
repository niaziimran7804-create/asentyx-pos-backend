namespace POS.Api.DTOs
{
    public class FinancialSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetProfit { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal CashBalance { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    public class DailySalesDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetProfit { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class SalesGraphDto
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> SalesData { get; set; } = new();
        public List<decimal> ExpensesData { get; set; } = new();
        public List<decimal> RefundsData { get; set; } = new();
        public List<decimal> ProfitData { get; set; } = new();
        public List<int> OrdersData { get; set; } = new();
    }

    public class PaymentMethodSummaryDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }
}
