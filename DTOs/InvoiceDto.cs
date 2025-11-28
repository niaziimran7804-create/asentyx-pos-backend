namespace POS.Api.DTOs
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = "Invoice";
        public int? OriginalInvoiceId { get; set; }
        public int? ReturnId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public OrderDto Order { get; set; } = null!;
        public ShopConfigurationDto ShopConfig { get; set; } = null!;
        public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
    }

    public class CreateInvoiceDto
    {
        public int OrderId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class InvoicePrintDto
    {
        public InvoiceDto Invoice { get; set; } = null!;
        public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    public class InvoiceItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class UpdateInvoiceDueDateDto
    {
        public DateTime DueDate { get; set; }
    }

    public class CreateCreditNoteDto
    {
        public int ReturnId { get; set; }
        public int OriginalInvoiceId { get; set; }
        public int OrderId { get; set; }
    }

    public class CreditNoteDto
    {
        public int CreditNoteId { get; set; }
        public string CreditNoteNumber { get; set; } = string.Empty;
        public DateTime CreditNoteDate { get; set; }
        public decimal CreditAmount { get; set; }
        public int OriginalInvoiceId { get; set; }
        public string OriginalInvoiceNumber { get; set; } = string.Empty;
        public int ReturnId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ReturnReason { get; set; } = string.Empty;
        public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
    }
}

