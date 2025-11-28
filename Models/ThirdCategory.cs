using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class ThirdCategory
    {
        [Key]
        public int ThirdCategoryId { get; set; }

        [Required]
        public int SecondCategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string ThirdCategoryName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ThirdCategoryDescription { get; set; }

        public byte[]? ThirdCategoryImage { get; set; }

        // Multi-tenancy columns
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }

        // Navigation properties
        [ForeignKey("SecondCategoryId")]
        public virtual SecondCategory? SecondCategory { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
    }
}

