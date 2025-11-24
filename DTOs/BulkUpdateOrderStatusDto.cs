namespace POS.Api.DTOs
{
    public class BulkUpdateOrderStatusDto
    {
        public List<int> OrderIds { get; set; } = new List<int>();
        public string Status { get; set; } = "Pending"; // "Paid", "Pending", or "Cancelled"
        public string OrderStatus { get; set; } = "Pending"; // "Paid", "Pending", or "Cancelled"
    }
}

