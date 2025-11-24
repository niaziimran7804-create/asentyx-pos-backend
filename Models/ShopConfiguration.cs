using System.ComponentModel.DataAnnotations;

namespace POS.Api.Models
{
    public class ShopConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string ShopName { get; set; } = "POS System";

        [StringLength(500)]
        public string? ShopAddress { get; set; }

        [StringLength(20)]
        public string? ShopPhone { get; set; }

        [StringLength(255)]
        public string? ShopEmail { get; set; }

        [StringLength(100)]
        public string? ShopWebsite { get; set; }

        [StringLength(100)]
        public string? TaxId { get; set; }

        [StringLength(500)]
        public string? FooterMessage { get; set; }

        [StringLength(500)]
        public string? HeaderMessage { get; set; }

        public byte[]? Logo { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

