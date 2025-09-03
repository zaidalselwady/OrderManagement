using System.ComponentModel.DataAnnotations;

namespace Order_Management_System.Models.ViewModels
{

    public class CreateOrderViewModel
    {
        [Required]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public DateTime? ReceivedDate { get; set; }
        public string? DeliveryTerms { get; set; }
        public string? PaymentTerms { get; set; }
        public string? Notes { get; set; }
        public List<CreateOrderDetailViewModel> OrderDetails { get; set; } = new List<CreateOrderDetailViewModel>();
    }

    
}
