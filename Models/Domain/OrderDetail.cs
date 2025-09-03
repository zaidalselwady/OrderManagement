using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order_Management_System.Models.Domain
{
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }
        public int ItemChildId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemDescription { get; set; } = string.Empty;

        public int OrderQuantity { get; set; }
        public int BonusQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountPercent { get; set; }

        [MaxLength(50)]
        public string BarCode { get; set; } = string.Empty;

        public int? UnitId { get; set; }

        [MaxLength(500)]
        public string? ItemNotes { get; set; }
    }
}
