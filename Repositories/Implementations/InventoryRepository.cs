// Replace Repositories/Implementations/InventoryRepository.cs
using Microsoft.EntityFrameworkCore;
using Order_Management_System.Models.Domain;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Data;

namespace Order_Management_System.Repositories.Implementations
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly OrderManagementDbContext _context;
        private readonly ILogger<InventoryRepository> _logger;

        public InventoryRepository(OrderManagementDbContext context, ILogger<InventoryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<InventoryItem>> GetInventoryItemsAsync()
        {
            try
            {
                return await _context.InventoryItems.ToListAsync();
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
                return await _context.Units.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting units");
                throw;
            }
        }

        public async Task<InventoryItem?> GetInventoryItemByIdAsync(int itemId)
        {
            try
            {
                return await _context.InventoryItems.FindAsync(itemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory item by ID: {ItemId}", itemId);
                throw;
            }
        }
    }
}





//using System.Text.Json;
//using Order_Management_System.Models.Domain;

//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Services.Interfaces;

//namespace Order_Management_System.Repositories.Implementations
//{

//    public class InventoryRepository : IInventoryRepository
//    {
//        private readonly ISoapClient _soapClient;
//        private readonly ILogger<InventoryRepository> _logger;
//        private const string Password = "OptimalPass_optimaljo05";

//        public InventoryRepository(ISoapClient soapClient, ILogger<InventoryRepository> logger)
//        {
//            _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<List<InventoryItem>> GetInventoryItemsAsync()
//        {
//            try
//            {
//                string sqlQuery = "SELECT Item_Child_id, Item_Child_Desc_e, Price, Bar_Code, Unit_id FROM Inv_Item_Index_Child";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response)
//                {
//                    return response.Select(MapToInventoryItem).ToList();
//                }

//                return new List<InventoryItem>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting inventory items");
//                throw;
//            }
//        }

//        public async Task<List<Unit>> GetUnitsAsync()
//        {
//            try
//            {
//                string sqlQuery = "SELECT Unit_id, Unit_Desc_e, Unit_Desc_a FROM Inv_Units";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response)
//                {
//                    return response.Select(MapToUnit).ToList();
//                }

//                return new List<Unit>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting units");
//                throw;
//            }
//        }

//        public async Task<InventoryItem?> GetInventoryItemByIdAsync(int itemId)
//        {
//            try
//            {
//                string sqlQuery = $"SELECT Item_Child_id, Item_Child_Desc_e, Price, Bar_Code, Unit_id FROM Inv_Item_Index_Child WHERE Item_Child_id = {itemId}";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    return MapToInventoryItem(response.First());
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting inventory item by ID: {ItemId}", itemId);
//                throw;
//            }
//        }

//        private InventoryItem MapToInventoryItem(Dictionary<string, object> data)
//        {
//            return new InventoryItem
//            {
//                ItemChildId = GetIntValue(data, "Item_Child_id"),
//                ItemDescription = GetStringValue(data, "Item_Child_Desc_e") ?? string.Empty,
//                Price = GetDecimalValue(data, "Price"),
//                BarCode = GetStringValue(data, "Bar_Code") ?? string.Empty,
//                UnitId = GetIntValue(data, "Unit_id")
//            };
//        }

//        private Unit MapToUnit(Dictionary<string, object> data)
//        {
//            return new Unit
//            {
//                UnitId = GetIntValue(data, "Unit_id"),
//                UnitDescriptionEnglish = GetStringValue(data, "Unit_Desc_e") ?? string.Empty,
//                UnitDescriptionArabic = GetStringValue(data, "Unit_Desc_a")
//            };
//        }

//        private int GetIntValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value))
//            {
//                return value is JsonElement jsonElement ? jsonElement.GetInt32() : Convert.ToInt32(value);
//            }
//            return 0;
//        }

//        private string? GetStringValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value) && value != null)
//            {
//                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
//                {
//                    return jsonElement.GetString();
//                }
//                return value == DBNull.Value ? null : value.ToString();
//            }
//            return null;
//        }

//        private decimal GetDecimalValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value))
//            {
//                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
//                {
//                    return jsonElement.GetDecimal();
//                }
//                return Convert.ToDecimal(value);
//            }
//            return 0;
//        }
//    }
//}