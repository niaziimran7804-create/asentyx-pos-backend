using System.ComponentModel.DataAnnotations;

namespace POS.Api.Models
{
    public class BarCode
    {
        [Key]
        public int BarCodeId { get; set; }

        [Required]
        [StringLength(100)]
        public string BarCode1 { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

