using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;

namespace Order_Management_System.Services.Interfaces;

public interface IOrderService
{
    Task<List<OrderSummaryViewModel>> GetOrderSummariesAsync();
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<OrderCreationResultDto> CreateOrderAsync(CreateOrderViewModel model, int userId);
    Task<List<InventoryItem>> GetInventoryItemsAsync();
    Task<List<Unit>> GetUnitsAsync();
    Task<int> GetNextOrderNumberAsync();
}

