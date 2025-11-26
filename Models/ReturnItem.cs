using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class ReturnItem
    {
        [Key]
        public int ReturnItemId { get; set; }

        [Required]
        public int ReturnId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ReturnQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReturnAmount { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Return? Return { get; set; }
        public virtual Product? Product { get; set; }
    }
}
