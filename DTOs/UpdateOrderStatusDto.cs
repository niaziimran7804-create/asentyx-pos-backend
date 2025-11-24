namespace POS.Api.DTOs
{
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = "Pending"; // "Paid", "Pending", or "Cancelled"
        public string OrderStatus { get; set; } = "Pending"; // "Paid", "Pending", or "Cancelled"
    }
}

