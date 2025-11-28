using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Return
    {
        [Key]
        public int ReturnId { get; set; }

        [Required]
        [StringLength(20)]
        public string ReturnType { get; set; } = string.Empty; // "whole" or "partial"

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        public string ReturnStatus { get; set; } = "Pending"; // Pending, Approved, Completed, Rejected

        [Required]
        [StringLength(500)]
        public string ReturnReason { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RefundMethod { get; set; } = string.Empty; // Cash, Card, Store Credit

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalReturnAmount { get; set; }

        public int? ProcessedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public int? CreditNoteInvoiceId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Invoice? Invoice { get; set; }
        public virtual Order? Order { get; set; }
        public virtual User? ProcessedByUser { get; set; }
        public virtual Invoice? CreditNoteInvoice { get; set; }
        public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
    }
}
