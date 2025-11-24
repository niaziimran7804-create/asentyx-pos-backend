namespace POS.Api.DTOs
{
    public class CustomerSearchDto
    {
        public string CustomerFullName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}

