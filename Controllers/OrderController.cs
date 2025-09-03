using Microsoft.AspNetCore.Mvc;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Controllers
{

    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            ICustomerService customerService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Add()
        {
            var userId = HttpContext.Session.GetInt32("User_ID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }
            return View();
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
                _logger.LogError(ex, "Error getting orders");
                return StatusCode(500, new { error = $"Failed to fetch orders: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Json(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details for ID: {OrderId}", orderId);
                return StatusCode(500, new { error = $"Failed to fetch order details: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid order data", errors = ModelState });
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("User_ID");
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _orderService.CreateOrderAsync(model, userId.Value);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        orderId = result.OrderId,
                        orderNumber = result.OrderNumber
                    });
                }
                else
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { error = $"Failed to create order: {ex.Message}" });
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
                _logger.LogError(ex, "Error getting inventory items");
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
                _logger.LogError(ex, "Error getting units");
                return StatusCode(500, new { error = $"Failed to fetch units: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNextOrderNumber()
        {
            try
            {
                var orderNumber = await _orderService.GetNextOrderNumberAsync();
                return Json(new { orderNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next order number");
                return StatusCode(500, new { error = $"Failed to get next order number: {ex.Message}" });
            }
        }
    }


}