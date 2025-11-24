namespace POS.Api.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int? BarCodeId { get; set; }
        public DateTime Date { get; set; }
        public int OrderQuantity { get; set; }
        public int ProductId { get; set; }
        public decimal ProductMSRP { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? UserName { get; set; }
        public string? CustomerFullName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        public int? InvoiceId { get; set; }
    }

    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public int? BarCodeId { get; set; }
        public int OrderQuantity { get; set; }
        public int ProductId { get; set; }
        public decimal ProductMSRP { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? CustomerFullName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

