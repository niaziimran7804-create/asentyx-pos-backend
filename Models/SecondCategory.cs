using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class SecondCategory
    {
        [Key]
        public int SecondCategoryId { get; set; }

        [Required]
        public int MainCategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string SecondCategoryName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? SecondCategoryDescription { get; set; }

        public byte[]? SecondCategoryImage { get; set; }

        // Navigation properties
        [ForeignKey("MainCategoryId")]
        public virtual MainCategory? MainCategory { get; set; }

        public virtual ICollection<ThirdCategory> ThirdCategories { get; set; } = new List<ThirdCategory>();
    }
}

