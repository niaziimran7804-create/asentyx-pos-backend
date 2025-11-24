using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class Expense
    {
        [Key]
        public int ExpenseId { get; set; }

        [Required]
        [StringLength(255)]
        public string ExpenseName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpenseAmount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    }
}

