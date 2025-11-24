using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int Id { get; set; } // UserId

        public int? BarCodeId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public int OrderQuantity { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductMSRP { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending";

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        // Customer Information
        [StringLength(255)]
        public string? CustomerFullName { get; set; }

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        [StringLength(500)]
        public string? CustomerAddress { get; set; }

        [StringLength(255)]
        public string? CustomerEmail { get; set; }

        // Navigation properties
        [ForeignKey("Id")]
        public virtual User? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("BarCodeId")]
        public virtual BarCode? BarCode { get; set; }

        public virtual ICollection<OrderProductMap> OrderProductMaps { get; set; } = new List<OrderProductMap>();
    }
}

