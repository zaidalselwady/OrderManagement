using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order_Management_System.Models.Domain
{
    public class InventoryItem
    {
        [Key]
        public int ItemChildId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemDescription { get; set; } = string.Empty;

        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string BarCode { get; set; } = string.Empty;

        public int UnitId { get; set; }
    }
}
