namespace POS.Api.DTOs
{
    public class InvoiceFilterDto
    {
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CustomerAddress { get; set; }
        public string? Status { get; set; }
    }
}

