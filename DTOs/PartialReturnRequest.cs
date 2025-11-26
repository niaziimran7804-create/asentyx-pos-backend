using System.ComponentModel.DataAnnotations;

namespace POS.Api.DTOs
{
    public class PartialReturnRequest
    {
        [Required]
        public string ReturnType { get; set; } = "partial";

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [MinLength(5, ErrorMessage = "Return reason must be at least 5 characters")]
        [StringLength(500)]
        public string ReturnReason { get; set; } = string.Empty;

        [Required]
        public string RefundMethod { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item must be selected for return")]
        public List<ReturnItemRequest> Items { get; set; } = new();

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total return amount must be greater than 0")]
        public decimal TotalReturnAmount { get; set; }
    }

    public class ReturnItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Return quantity must be at least 1")]
        public int ReturnQuantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Return amount must be greater than 0")]
        public decimal ReturnAmount { get; set; }
    }
}
