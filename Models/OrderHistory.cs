using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class OrderHistory
    {
        [Key]
        public int OrderHistoryId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; } // User who made the change

        [StringLength(50)]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [StringLength(50)]
        public string? PreviousOrderStatus { get; set; }

        [StringLength(50)]
        public string? NewOrderStatus { get; set; }

        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Created, Updated, Cancelled, Paid, etc.

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}

