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

        // Multi-tenancy columns
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }

        // Navigation properties
        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

