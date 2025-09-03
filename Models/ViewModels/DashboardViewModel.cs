

namespace Order_Management_System.Models.ViewModels
{
   
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalAmount { get; set; }
        public int RecentOrders { get; set; }
        public List<OrderSummaryViewModel> Orders { get; set; } = new List<OrderSummaryViewModel>();
    }

   }
