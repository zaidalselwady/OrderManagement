using System.ComponentModel.DataAnnotations;

namespace Order_Management_System.Models.ViewModels
{

    public class CreateOrderDetailViewModel
    {
        [Required]
        public string BarCode { get; set; } = string.Empty;
        [Required]
        public int ItemChildId { get; set; }
        [Required]
        public string ItemDescription { get; set; } = string.Empty;
        [Required]
        public int UnitId { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Bonus quantity must be non-negative")]
        public int BonusQuantity { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal DiscountPercent { get; set; }
        public string? ItemNotes { get; set; }
    }

    
}
