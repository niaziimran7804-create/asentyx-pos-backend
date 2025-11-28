using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class MainCategory
    {
        [Key]
        public int MainCategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string MainCategoryName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MainCategoryDescription { get; set; }

        public byte[]? MainCategoryImage { get; set; }

        // Multi-tenancy columns
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        public virtual ICollection<SecondCategory> SecondCategories { get; set; } = new List<SecondCategory>();
    }
}

