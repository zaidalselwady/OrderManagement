using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;

namespace Order_Management_System.Repositories.Interfaces;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetInventoryItemsAsync();
    Task<List<Unit>> GetUnitsAsync();
    Task<InventoryItem?> GetInventoryItemByIdAsync(int itemId);
}

