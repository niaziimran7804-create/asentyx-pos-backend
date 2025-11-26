namespace POS.Api.DTOs
{
    public class ReturnDto
    {
        public int ReturnId { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnStatus { get; set; } = string.Empty;
        public decimal TotalReturnAmount { get; set; }
        public string RefundMethod { get; set; } = string.Empty;
        public string ReturnReason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? CustomerFullName { get; set; }
        public string? CustomerPhone { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int? ItemsCount { get; set; }
        public List<ReturnedItemDto>? ReturnedItems { get; set; }
        public string? Message { get; set; }
    }

    public class ReturnedItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ReturnQuantity { get; set; }
        public decimal ReturnAmount { get; set; }
    }
}
