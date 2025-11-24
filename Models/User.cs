using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        public int Age { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Salesman"; // Admin, Cashier, Salesman

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        public DateTime Birthdate { get; set; }

        [StringLength(50)]
        public string? NID { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? HomeTown { get; set; }

        [StringLength(100)]
        public string? CurrentCity { get; set; }

        [StringLength(100)]
        public string? Division { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        public int? PostalCode { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

