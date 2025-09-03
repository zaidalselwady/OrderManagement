using Microsoft.Extensions.Logging;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Services.Interfaces;
using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Services.Implementations
{
    
    public class DashboardService : IDashboardService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IOrderRepository orderRepository,
            IAuthenticationService authenticationService,
            ILogger<DashboardService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int userId)
        {
            try
            {
                var userName = await _authenticationService.GetUserNameAsync(userId);
                var orders = await _orderRepository.GetOrdersAsync();

                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                var recentOrders = orders.Where(o => o.OrderDate >= sevenDaysAgo).Count();

                var orderSummaries = orders.Take(50).Select(order => new OrderSummaryViewModel
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    CustomerName = order.CustomerName ?? "N/A",
                    Amount = order.Amount,
                    Status = order.Status.ToString()
                }).ToList();

                return new DashboardViewModel
                {
                    UserName = userName,
                    TotalOrders = orders.Count,
                    TotalAmount = orders.Sum(o => o.Amount),
                    RecentOrders = recentOrders,
                    Orders = orderSummaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for user: {UserId}", userId);
                throw;
            }
        }
    }
}