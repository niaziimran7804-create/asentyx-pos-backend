namespace POS.Api.DTOs
{
    public class ReturnSummaryDto
    {
        public int TotalReturns { get; set; }
        public int PendingReturns { get; set; }
        public int ApprovedReturns { get; set; }
        public int CompletedReturns { get; set; }
        public decimal TotalReturnAmount { get; set; }
        public int WholeReturnsCount { get; set; }
        public int PartialReturnsCount { get; set; }
    }
}
