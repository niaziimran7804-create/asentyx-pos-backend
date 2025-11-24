using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [StringLength(100)]
        public string? BrandTag { get; set; }

        [Required]
        [StringLength(255)]
        public string BrandName { get; set; } = string.Empty;

        [Required]
        public int VendorId { get; set; }

        [StringLength(1000)]
        public string? BrandDescription { get; set; }

        [StringLength(10)]
        public string BrandStatus { get; set; } = "YES";

        public byte[]? BrandImage { get; set; }

        // Navigation properties
        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

