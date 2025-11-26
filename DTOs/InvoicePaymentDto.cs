namespace POS.Api.DTOs
{
    public class InvoicePaymentDto
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInvoicePaymentDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? TransactionReference { get; set; }
    }

    public class InvoicePaymentSummaryDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<InvoicePaymentDto> Payments { get; set; } = new();
    }
}
