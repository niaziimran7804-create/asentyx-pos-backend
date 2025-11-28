namespace POS.Api.DTOs
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int OrderId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
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
}

