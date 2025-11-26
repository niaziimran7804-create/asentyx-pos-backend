using POS.Api.Models;

namespace POS.Api.DTOs
{
    public class AccountingEntryDto
    {
        public int EntryId { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? Category { get; set; }
        public DateTime EntryDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateAccountingEntryDto
    {
        public string EntryType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? Category { get; set; }
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    }

    public class AccountingEntriesResponseDto
    {
        public List<AccountingEntryDto> Entries { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class PaginationDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
    }
}
