using System.ComponentModel.DataAnnotations;

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

        // Navigation properties
        public virtual ICollection<SecondCategory> SecondCategories { get; set; } = new List<SecondCategory>();
    }
}

