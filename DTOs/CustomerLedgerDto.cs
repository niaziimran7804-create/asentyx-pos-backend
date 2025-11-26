namespace POS.Api.DTOs
{
    public class CustomerLedgerDto
    {
        public int LedgerId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public int? OrderId { get; set; }
        public int? ReturnId { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLedgerEntryDto
    {
        public int CustomerId { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string TransactionType { get; set; } = string.Empty; // Sale, Payment, Refund, Credit, Debit, Adjustment
        public string Description { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public int? OrderId { get; set; }
        public int? ReturnId { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class CustomerAgingDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal Days0To30 { get; set; }  // 0-30 days
        public decimal Days31To60 { get; set; } // 31-60 days
        public decimal Days61To90 { get; set; } // 61-90 days
        public decimal Days91Plus { get; set; }  // 91+ days
        public decimal TotalOutstanding { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public int TotalInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
    }

    public class CustomerStatementDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public DateTime StatementDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<CustomerLedgerDto> Transactions { get; set; } = new();
    }

    public class AgingReportDto
    {
        public DateTime ReportDate { get; set; }
        public DateTime AsOfDate { get; set; }
        public List<CustomerAgingDto> Customers { get; set; } = new();
        public decimal TotalDays0To30 { get; set; }
        public decimal TotalDays31To60 { get; set; }
        public decimal TotalDays61To90 { get; set; }
        public decimal TotalDays91Plus { get; set; }
        public decimal GrandTotal { get; set; }
        public int TotalCustomers { get; set; }
        public int CustomersWithBalance { get; set; }
    }

    public class LedgerSummaryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalRefunds { get; set; }
        public int TotalTransactions { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
    }
}
