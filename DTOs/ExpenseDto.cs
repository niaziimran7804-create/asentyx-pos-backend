namespace POS.Api.DTOs
{
    public class ExpenseDto
    {
        public int ExpenseId { get; set; }
        public string ExpenseName { get; set; } = string.Empty;
        public decimal ExpenseAmount { get; set; }
        public DateTime ExpenseDate { get; set; }
    }

    public class CreateExpenseDto
    {
        public string ExpenseName { get; set; } = string.Empty;
        public decimal ExpenseAmount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    }
}

