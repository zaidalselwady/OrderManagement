using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Controllers
{

    public class DataController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<DataController> _logger;

        public DataController(
            IOrderService orderService,
            ICustomerService customerService,
            ILogger<DataController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var orders = await _orderService.GetOrderSummariesAsync();
                return Json(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders data");
                return StatusCode(500, new { error = $"Failed to fetch orders: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _customerService.GetCustomersAsync();
                return Json(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers data");
                return StatusCode(500, new { error = $"Failed to fetch customers: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInventoryItems()
        {
            try
            {
                var items = await _orderService.GetInventoryItemsAsync();
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory items data");
                return StatusCode(500, new { error = $"Failed to fetch inventory items: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            try
            {
                var units = await _orderService.GetUnitsAsync();
                return Json(units);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting units data");
                return StatusCode(500, new { error = $"Failed to fetch units: {ex.Message}" });
            }
        }
    }
}