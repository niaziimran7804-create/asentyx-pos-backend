using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Api.Models
{
    public class DailySales
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime SaleDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; } = 0;

        public int TotalOrders { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalExpenses { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetProfit { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSales { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSales { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageOrderValue { get; set; } = 0;
    }
}
