using System.ComponentModel.DataAnnotations;

namespace POS.Api.Models
{
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? SubscriptionEndDate { get; set; }

        [StringLength(50)]
        public string SubscriptionPlan { get; set; } = "Basic"; // Basic, Premium, Enterprise

        // Navigation properties
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
