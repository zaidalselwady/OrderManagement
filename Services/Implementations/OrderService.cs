using Microsoft.Extensions.Logging;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Services.Interfaces;
using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;


namespace Order_Management_System.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IInventoryRepository inventoryRepository,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<OrderSummaryViewModel>> GetOrderSummariesAsync()
        {
            try
            {
                var orders = await _orderRepository.GetOrdersAsync();

                return orders.Select(order => new OrderSummaryViewModel
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    CustomerName = order.CustomerName ?? "N/A",
                    Amount = order.Amount,
                    Status = order.Status.ToString()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order summaries");
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            try
            {
                return await _orderRepository.GetOrderByIdAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<OrderCreationResultDto> CreateOrderAsync(CreateOrderViewModel model, int userId)
        {
            try
            {
                // Validate customer exists
                var customer = await _customerRepository.GetCustomerByIdAsync(model.CustomerId);
                if (customer == null)
                {
                    return new OrderCreationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Customer not found"
                    };
                }

                // Get next order number
                var orderNumber = await GetNextOrderNumberAsync();

                // Create order entity
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = model.OrderDate,
                    ReceivedDate = model.ReceivedDate,
                    CustomerId = model.CustomerId,
                    CustomerName = customer.NameEnglish,
                    Amount = CalculateOrderTotal(model.OrderDetails),
                    DeliveryTerms = model.DeliveryTerms,
                    PaymentTerms = model.PaymentTerms,
                    Status = OrderStatus.Pending,
                    Notes = model.Notes,
                    UserId = userId,
                    SalesmanId = 1 // Default salesman ID
                };

                // Create order master
                var orderId = await _orderRepository.CreateOrderAsync(order);

                // Create order details
                foreach (var detail in model.OrderDetails.Where(d => d.Quantity > 0))
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        ItemChildId = detail.ItemChildId,
                        ItemDescription = detail.ItemDescription,
                        OrderQuantity = detail.Quantity,
                        BonusQuantity = detail.BonusQuantity,
                        Price = detail.Price,
                        DiscountPercent = detail.DiscountPercent,
                        BarCode = detail.BarCode,
                        UnitId = detail.UnitId,
                        ItemNotes = detail.ItemNotes
                    };

                    await _orderRepository.CreateOrderDetailAsync(orderDetail);
                }

                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, OrderNumber: {OrderNumber}", orderId, orderNumber);

                return new OrderCreationResultDto
                {
                    Success = true,
                    OrderId = orderId,
                    OrderNumber = orderNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new OrderCreationResultDto
                {
                    Success = false,
                    ErrorMessage = $"Failed to create order: {ex.Message}"
                };
            }
        }

        public async Task<List<InventoryItem>> GetInventoryItemsAsync()
        {
            try
            {
                return await _inventoryRepository.GetInventoryItemsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory items");
                throw;
            }
        }

        public async Task<List<Unit>> GetUnitsAsync()
        {
            try
            {
                return await _inventoryRepository.GetUnitsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting units");
                throw;
            }
        }

        public async Task<int> GetNextOrderNumberAsync()
        {
            try
            {
                return await _orderRepository.GetNextOrderNumberAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next order number");
                throw;
            }
        }

        private decimal CalculateOrderTotal(List<CreateOrderDetailViewModel> orderDetails)
        {
            return orderDetails.Sum(detail =>
            {
                var lineTotal = detail.Quantity * detail.Price;
                var discount = lineTotal * (detail.DiscountPercent / 100);
                return lineTotal - discount;
            });
        }
    }

   
}