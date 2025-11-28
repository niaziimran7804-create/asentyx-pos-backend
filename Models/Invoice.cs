using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        public int? CompanyId { get; set; }

        public int? BranchId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string InvoiceType { get; set; } = "Invoice"; // Invoice, CreditNote

        public int? OriginalInvoiceId { get; set; }

        public int? ReturnId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, PartiallyPaid, Overdue, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("OriginalInvoiceId")]
        public virtual Invoice? OriginalInvoice { get; set; }

        [ForeignKey("ReturnId")]
        public virtual Return? Return { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
    }
}

