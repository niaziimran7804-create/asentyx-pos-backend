using System.ComponentModel.DataAnnotations;

namespace POS.Api.DTOs
{
    public class WholeReturnRequest
    {
        [Required]
        public string ReturnType { get; set; } = "whole";

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
        [Range(0.01, double.MaxValue, ErrorMessage = "Total return amount must be greater than 0")]
        public decimal TotalReturnAmount { get; set; }
    }
}
