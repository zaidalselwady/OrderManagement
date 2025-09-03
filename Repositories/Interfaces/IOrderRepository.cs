using Order_Management_System.Models.Domain;

namespace Order_Management_System.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId);
    Task<int> CreateOrderAsync(Order order);
    Task<bool> CreateOrderDetailAsync(OrderDetail orderDetail);
    Task<int> GetNextOrderNumberAsync();
}

