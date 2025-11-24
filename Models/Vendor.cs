using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        [StringLength(100)]
        public string? VendorTag { get; set; }

        [Required]
        [StringLength(255)]
        public string VendorName { get; set; } = string.Empty;

        [Required]
        public int ThirdCategoryId { get; set; }

        [StringLength(1000)]
        public string? VendorDescription { get; set; }

        [StringLength(10)]
        public string VendorStatus { get; set; } = "YES";

        public byte[]? VendorImage { get; set; }

        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ThirdCategoryId")]
        public virtual ThirdCategory? ThirdCategory { get; set; }

        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
    }
}

