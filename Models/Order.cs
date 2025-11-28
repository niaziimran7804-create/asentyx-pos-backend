using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int? CompanyId { get; set; }

        public int? BranchId { get; set; }

        [Required]
        public int CustomerId { get; set; } // References User.Id where Role = "Customer"

        public int? BarCodeId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending";

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [ForeignKey("BarCodeId")]
        public virtual BarCode? BarCode { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<OrderProductMap> OrderProductMaps { get; set; } = new List<OrderProductMap>();
    }
}

