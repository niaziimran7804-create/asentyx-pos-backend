namespace POS.Api.DTOs
{
    public class BulkPrintInvoicesDto
    {
        public List<int> InvoiceIds { get; set; } = new List<int>();
    }
}

