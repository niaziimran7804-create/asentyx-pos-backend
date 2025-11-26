using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class CustomerLedger
    {
        [Key]
        public int LedgerId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = string.Empty; // Sale, Payment, Refund, Credit, Debit, Adjustment

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        public int? OrderId { get; set; }

        public int? ReturnId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebitAmount { get; set; } // Amount customer owes

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditAmount { get; set; } // Amount customer paid or credited

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } // Running balance

        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Bank Transfer, etc.

        [StringLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty; // Check number, transaction ID, etc.

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("ReturnId")]
        public virtual Return? Return { get; set; }
    }
}
