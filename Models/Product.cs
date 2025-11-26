using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [StringLength(100)]
        public string? ProductIdTag { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int BrandId { get; set; }

        [StringLength(1000)]
        public string? ProductDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductQuantityPerUnit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductPerUnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductMSRP { get; set; }

        [StringLength(10)]
        public string ProductStatus { get; set; } = "YES";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductDiscountRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductSize { get; set; }

        [StringLength(50)]
        public string? ProductColor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductWeight { get; set; }

        public int ProductUnitStock { get; set; }

        public int StockThreshold { get; set; } = 10;

        public byte[]? ProductImage { get; set; }

        // Navigation properties
        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }
    }
}

