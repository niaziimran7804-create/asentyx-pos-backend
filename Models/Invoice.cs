using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue, Cancelled

        // Navigation property
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}

