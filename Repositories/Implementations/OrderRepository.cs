using Microsoft.EntityFrameworkCore;
using Order_Management_System.Models.Domain;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Order_Management_System.Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderManagementDbContext _context;
        private readonly ILogger<OrderRepository> _logger;
        private readonly IConfiguration _configuration;

        public OrderRepository(OrderManagementDbContext context, ILogger<OrderRepository> logger, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            // Always try Entity Framework first, then fallback if it fails
            try
            {
                _logger.LogDebug("Attempting to retrieve orders using Entity Framework");

                var orders = await _context.Orders
                    .OrderByDescending(o => o.OrderNumber)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {OrderCount} orders using Entity Framework", orders.Count);
                return orders;
            }
            catch (Exception efEx)
            {
                _logger.LogWarning(efEx, "Entity Framework failed, attempting fallback to raw SQL");

                try
                {
                    return await GetOrdersWithRawSqlAsync();
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Both Entity Framework and raw SQL fallback failed");
                    // Return empty list instead of throwing to prevent application crash
                    return new List<Order>();
                }
            }
        }

        private async Task<List<Order>> GetOrdersWithRawSqlAsync()
        {
            _logger.LogInformation("Using raw SQL fallback method");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("No connection string available for fallback");
            }

            var orders = new List<Order>();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT order_Id, Order_no, Order_date, rcvd_date, Customer_id, Customer_Name,
                       amount, Delivery_Terms, Payment_Terms, Status, notes, Salesman_id, User_id
                FROM Inv_Orders_Master 
                ORDER BY Order_no DESC";

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    OrderId = Convert.ToInt32(reader["order_Id"]),
                    OrderNumber = Convert.ToInt32(reader["Order_no"]),
                    OrderDate = Convert.ToDateTime(reader["Order_date"]),
                    ReceivedDate = reader.IsDBNull("rcvd_date") ? null : Convert.ToDateTime(reader["rcvd_date"]),
                    CustomerId = Convert.ToInt32(reader["Customer_id"]),
                    CustomerName = reader["Customer_Name"]?.ToString(),
                    Amount = reader.IsDBNull("amount") ? 0 : Convert.ToDecimal(reader["amount"]),
                    DeliveryTerms = reader["Delivery_Terms"]?.ToString(),
                    PaymentTerms = reader["Payment_Terms"]?.ToString(),
                    Status = (OrderStatus)(reader.IsDBNull("Status") ? 0 : Convert.ToInt32(reader["Status"])),
                    Notes = reader["notes"]?.ToString(),
                    SalesmanId = reader.IsDBNull("Salesman_id") ? null : Convert.ToInt32(reader["Salesman_id"]),
                    UserId = reader.IsDBNull("User_id") ? null : Convert.ToInt32(reader["User_id"])
                });
            }

            _logger.LogInformation("Successfully retrieved {OrderCount} orders using raw SQL fallback", orders.Count);
            return orders;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            try
            {
                _logger.LogDebug("Retrieving order with ID: {OrderId}", orderId);

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order != null)
                {
                    _logger.LogDebug("Successfully retrieved order {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {OrderId}, attempting fallback", orderId);
                return await GetOrderByIdWithRawSqlAsync(orderId);
            }
        }

        private async Task<Order?> GetOrderByIdWithRawSqlAsync(int orderId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return null;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT order_Id, Order_no, Order_date, rcvd_date, Customer_id, Customer_Name,
                           amount, Delivery_Terms, Payment_Terms, Status, notes, Salesman_id, User_id
                    FROM Inv_Orders_Master 
                    WHERE orderId = @orderId";

                command.Parameters.Add(new SqlParameter("@orderId", orderId));

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var order = new Order
                    {
                        OrderId = Convert.ToInt32(reader["order_Id"]),
                        OrderNumber = Convert.ToInt32(reader["Order_no"]),
                        OrderDate = Convert.ToDateTime(reader["Order_date"]),
                        ReceivedDate = reader.IsDBNull("rcvd_date") ? null : Convert.ToDateTime(reader["rcvd_date"]),
                        CustomerId = Convert.ToInt32(reader["Customer_id"]),
                        CustomerName = reader["Customer_Name"]?.ToString(),
                        Amount = reader.IsDBNull("amount") ? 0 : Convert.ToDecimal(reader["amount"]),
                        DeliveryTerms = reader["Delivery_Terms"]?.ToString(),
                        PaymentTerms = reader["Payment_Terms"]?.ToString(),
                        Status = (OrderStatus)(reader.IsDBNull("Status") ? 0 : Convert.ToInt32(reader["Status"])),
                        Notes = reader["notes"]?.ToString(),
                        SalesmanId = reader.IsDBNull("Salesman_id") ? null : Convert.ToInt32(reader["Salesman_id"]),
                        UserId = reader.IsDBNull("User_id") ? null : Convert.ToInt32(reader["User_id"])
                    };

                    // Get order details
                    reader.Close();
                    order.OrderDetails = await GetOrderDetailsWithRawSqlAsync(orderId, connection);

                    return order;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raw SQL fallback also failed for order ID: {OrderId}", orderId);
                return null;
            }
        }

        public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
        {
            try
            {
                _logger.LogDebug("Retrieving order details for order: {OrderId}", orderId);

                var orderDetails = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {DetailCount} order details for order {OrderId}", orderDetails.Count, orderId);
                return orderDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details for order: {OrderId}, attempting fallback", orderId);

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return new List<OrderDetail>();
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return await GetOrderDetailsWithRawSqlAsync(orderId, connection);
            }
        }

        private async Task<List<OrderDetail>> GetOrderDetailsWithRawSqlAsync(int orderId, SqlConnection connection)
        {
            var orderDetails = new List<OrderDetail>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Order_Detail_id, Order_id, Item_Child_id, Item_Desc, Ord_Qty, Qty_Bonus,
                       Price, Discount_Percent, Bar_Code, Unit_id, Item_Notes
                FROM Inv_Orders_Details 
                WHERE Order_id = @orderId";

            command.Parameters.Add(new SqlParameter("@orderId", orderId));

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orderDetails.Add(new OrderDetail
                {
                    OrderDetailId = Convert.ToInt32(reader["Order_Detail_id"]),
                    OrderId = Convert.ToInt32(reader["Order_id"]),
                    ItemChildId = Convert.ToInt32(reader["Item_Child_id"]),
                    ItemDescription = reader["Item_Desc"]?.ToString() ?? string.Empty,
                    OrderQuantity = Convert.ToInt32(reader["Ord_Qty"]),
                    BonusQuantity = reader.IsDBNull("Qty_Bonus") ? 0 : Convert.ToInt32(reader["Qty_Bonus"]),
                    Price = reader.IsDBNull("Price") ? 0 : Convert.ToDecimal(reader["Price"]),
                    DiscountPercent = reader.IsDBNull("Discount_Percent") ? 0 : Convert.ToDecimal(reader["Discount_Percent"]),
                    BarCode = reader["Bar_Code"]?.ToString() ?? string.Empty,
                    UnitId = reader.IsDBNull("Unit_id") ? null : Convert.ToInt32(reader["Unit_id"]),
                    ItemNotes = reader["Item_Notes"]?.ToString()
                });
            }

            return orderDetails;
        }

        public async Task<int> CreateOrderAsync(Order order)
        {
            try
            {
                _logger.LogDebug("Creating new order with number: {OrderNumber}", order.OrderNumber);

                // Use a transaction for data integrity
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully created order with ID: {OrderId}", order.OrderId);
                    return order.OrderId;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order, attempting raw SQL fallback");
                return await CreateOrderWithRawSqlAsync(order);
            }
        }

        private async Task<int> CreateOrderWithRawSqlAsync(Order order)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("No connection string available for fallback");
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Inv_Orders_Master (
                        Order_no, Order_date, rcvd_date, Customer_id, Customer_Name,
                        amount, Delivery_Terms, Payment_Terms, Status, notes, Salesman_id, User_id
                    )
                    VALUES (
                        @Order_no, @Order_date, @rcvd_date, @Customer_id, @Customer_Name,
                        @amount, @Delivery_Terms, @Payment_Terms, @Status, @notes, @Salesman_id, @User_id
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                // Add parameters
                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("@Order_no", order.OrderNumber),
                    new SqlParameter("@Order_date", order.OrderDate),
                    new SqlParameter("@rcvd_date", (object)order.ReceivedDate ?? DBNull.Value),
                    new SqlParameter("@Customer_id", order.CustomerId),
                    new SqlParameter("@Customer_Name", (object)order.CustomerName ?? DBNull.Value),
                    new SqlParameter("@amount", order.Amount),
                    new SqlParameter("@Delivery_Terms", (object)order.DeliveryTerms ?? DBNull.Value),
                    new SqlParameter("@Payment_Terms", (object)order.PaymentTerms ?? DBNull.Value),
                    new SqlParameter("@Status", (int)order.Status),
                    new SqlParameter("@notes", (object)order.Notes ?? DBNull.Value),
                    new SqlParameter("@Salesman_id", (object)order.SalesmanId ?? DBNull.Value),
                    new SqlParameter("@User_id", (object)order.UserId ?? DBNull.Value)
                });

                var result = await command.ExecuteScalarAsync();
                var orderId = Convert.ToInt32(result);

                transaction.Commit();

                _logger.LogInformation("Successfully created order using raw SQL fallback. OrderId: {OrderId}", orderId);
                return orderId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CreateOrderDetailAsync(OrderDetail orderDetail)
        {
            try
            {
                _logger.LogDebug("Creating order detail for order: {OrderId}", orderDetail.OrderId);

                _context.OrderDetails.Add(orderDetail);
                var result = await _context.SaveChangesAsync();

                var success = result > 0;
                if (success)
                {
                    _logger.LogDebug("Successfully created order detail with ID: {OrderDetailId}", orderDetail.OrderDetailId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order detail, attempting fallback");
                return await CreateOrderDetailWithRawSqlAsync(orderDetail);
            }
        }

        private async Task<bool> CreateOrderDetailWithRawSqlAsync(OrderDetail orderDetail)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return false;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Inv_Orders_Details (
                        Order_id, Item_Child_id, Bar_Code, Item_Desc, Ord_Qty, Qty_Bonus,
                        Price, Discount_Percent, Unit_id, Item_Notes
                    )
                    VALUES (
                        @Order_id, @Item_Child_id, @Bar_Code, @Item_Desc, @Ord_Qty, @Qty_Bonus,
                        @Price, @Discount_Percent, @Unit_id, @Item_Notes
                    )";

                command.Parameters.AddRange(new[]
                {
                    new SqlParameter("@Order_id", orderDetail.OrderId),
                    new SqlParameter("@Item_Child_id", orderDetail.ItemChildId),
                    new SqlParameter("@Bar_Code", (object)orderDetail.BarCode ?? DBNull.Value),
                    new SqlParameter("@Item_Desc", (object)orderDetail.ItemDescription ?? DBNull.Value),
                    new SqlParameter("@Ord_Qty", orderDetail.OrderQuantity),
                    new SqlParameter("@Qty_Bonus", orderDetail.BonusQuantity),
                    new SqlParameter("@Price", orderDetail.Price),
                    new SqlParameter("@Discount_Percent", orderDetail.DiscountPercent),
                    new SqlParameter("@Unit_id", (object)orderDetail.UnitId ?? DBNull.Value),
                    new SqlParameter("@Item_Notes", (object)orderDetail.ItemNotes ?? DBNull.Value)
                });

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raw SQL fallback for order detail creation also failed");
                return false;
            }
        }

        public async Task<int> GetNextOrderNumberAsync()
        {
            try
            {
                _logger.LogDebug("Getting next order number");

                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.OrderNumber)
                    .FirstOrDefaultAsync();

                var nextOrderNumber = (lastOrder?.OrderNumber ?? 0) + 1;

                _logger.LogDebug("Next order number: {OrderNumber}", nextOrderNumber);
                return nextOrderNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next order number, trying fallback method");
                return await GetNextOrderNumberWithRawSqlAsync();
            }
        }

        private async Task<int> GetNextOrderNumberWithRawSqlAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("No connection string available");
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT ISNULL(MAX(Order_no), 0) + 1 FROM Inv_Orders_Master";

                var result = await command.ExecuteScalarAsync();
                var nextOrderNumber = Convert.ToInt32(result);

                _logger.LogInformation("Next order number from fallback: {OrderNumber}", nextOrderNumber);
                return nextOrderNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback method for getting next order number also failed");
                // Return a safe default
                return 1;
            }
        }
    }
}

























//// Replace Repositories/Implementations/OrderRepository.cs
//using Microsoft.EntityFrameworkCore;
//using Order_Management_System.Models.Domain;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Data;

//namespace Order_Management_System.Repositories.Implementations
//{
//    public class OrderRepository : IOrderRepository
//    {
//        private readonly OrderManagementDbContext _context;
//        private readonly ILogger<OrderRepository> _logger;

//        public OrderRepository(OrderManagementDbContext context, ILogger<OrderRepository> logger)
//        {
//            _context = context ?? throw new ArgumentNullException(nameof(context));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<List<Order>> GetOrdersAsync()
//        {
//            try
//            {
//                return await _context.Orders
//                    .OrderByDescending(o => o.OrderNumber)
//                    .ToListAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting orders");
//                throw;
//            }
//        }

//        public async Task<Order?> GetOrderByIdAsync(int orderId)
//        {
//            try
//            {
//                return await _context.Orders
//                    .Include(o => o.OrderDetails)
//                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting order by ID: {OrderId}", orderId);
//                throw;
//            }
//        }

//        public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
//        {
//            try
//            {
//                return await _context.OrderDetails
//                    .Where(od => od.OrderId == orderId)
//                    .ToListAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting order details for order: {OrderId}", orderId);
//                throw;
//            }
//        }

//        public async Task<int> CreateOrderAsync(Order order)
//        {
//            try
//            {
//                _context.Orders.Add(order);
//                await _context.SaveChangesAsync();
//                return order.OrderId;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating order");
//                throw;
//            }
//        }

//        public async Task<bool> CreateOrderDetailAsync(OrderDetail orderDetail)
//        {
//            try
//            {
//                _context.OrderDetails.Add(orderDetail);
//                await _context.SaveChangesAsync();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating order detail");
//                throw;
//            }
//        }

//        public async Task<int> GetNextOrderNumberAsync()
//        {
//            try
//            {
//                var lastOrder = await _context.Orders
//                    .OrderByDescending(o => o.OrderNumber)
//                    .FirstOrDefaultAsync();

//                return (lastOrder?.OrderNumber ?? 0) + 1;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting next order number");
//                throw;
//            }
//        }
//    }
//}











//using System.Text.Json;
//using Order_Management_System.Models.Domain;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Services.Interfaces;
//namespace Order_Management_System.Repositories.Implementations
//{

//    public class OrderRepository : IOrderRepository
//    {
//        private readonly ISoapClient _soapClient;
//        private readonly ILogger<OrderRepository> _logger;
//        private const string Password = "OptimalPass_optimaljo05";

//        public OrderRepository(ISoapClient soapClient, ILogger<OrderRepository> logger)
//        {
//            _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<List<Order>> GetOrdersAsync()
//        {
//            try
//            {
//                string sqlQuery = "SELECT * FROM Inv_Orders_Master ORDER BY Order_no DESC";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response)
//                {
//                    return response.Select(MapToOrder).ToList();
//                }

//                return new List<Order>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting orders");
//                throw;
//            }
//        }

//        public async Task<Order?> GetOrderByIdAsync(int orderId)
//        {
//            try
//            {
//                string sqlQuery = $"SELECT * FROM Inv_Orders_Master WHERE orderId = {orderId}";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    var order = MapToOrder(response.First());
//                    order.OrderDetails = await GetOrderDetailsAsync(orderId);
//                    return order;
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting order by ID: {OrderId}", orderId);
//                throw;
//            }
//        }

//        public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
//        {
//            try
//            {
//                string sqlQuery = $"SELECT * FROM Inv_Orders_Details WHERE Order_id = {orderId}";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response)
//                {
//                    return response.Select(MapToOrderDetail).ToList();
//                }

//                return new List<OrderDetail>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting order details for order: {OrderId}", orderId);
//                throw;
//            }
//        }

//        public async Task<int> CreateOrderAsync(Order order)
//        {
//            try
//            {
//                var parameters = new Dictionary<string, object>
//                {
//                    { "Order_no", order.OrderNumber },
//                    { "Order_date", order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss") },
//                    { "rcvd_date", order.ReceivedDate?.ToString("yyyy-MM-dd HH:mm:ss") },
//                    { "Customer_id", order.CustomerId },
//                    { "Customer_Name", order.CustomerName },
//                    { "amount", order.Amount },
//                    { "Delivery_Terms", order.DeliveryTerms },
//                    { "Payment_Terms", order.PaymentTerms },
//                    { "Status", (int)order.Status },
//                    { "notes", order.Notes },
//                    { "Salesman_id", order.SalesmanId },
//                    { "User_id", order.UserId }
//                };

//                string sqlQuery = @"
//                    INSERT INTO Inv_Orders_Master (
//                        Order_no, Order_date, rcvd_date, Customer_id, Customer_Name,
//                        amount, Delivery_Terms, Payment_Terms, Status, notes, Salesman_id, User_id
//                    )
//                    VALUES (
//                        @Order_no, @Order_date, @rcvd_date, @Customer_id, @Customer_Name,
//                        @amount, @Delivery_Terms, @Payment_Terms, @Status, @notes, @Salesman_id, @User_id
//                    );
//                    SELECT SCOPE_IDENTITY() AS orderId;";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, parameters, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    var orderIdValue = response.First()["orderId"];
//                    return orderIdValue is JsonElement jsonElement ? jsonElement.GetInt32() : Convert.ToInt32(orderIdValue);
//                }

//                throw new InvalidOperationException("Failed to create order - no ID returned");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating order");
//                throw;
//            }
//        }

//        public async Task<bool> CreateOrderDetailAsync(OrderDetail orderDetail)
//        {
//            try
//            {
//                var parameters = new Dictionary<string, object>
//                {
//                    { "Order_id", orderDetail.OrderId },
//                    { "Item_Child_id", orderDetail.ItemChildId },
//                    { "Bar_Code", orderDetail.BarCode },
//                    { "Item_Desc", orderDetail.ItemDescription },
//                    { "Ord_Qty", orderDetail.OrderQuantity },
//                    { "Qty_Bonus", orderDetail.BonusQuantity },
//                    { "Price", orderDetail.Price },
//                    { "Discount_Percent", orderDetail.DiscountPercent },
//                    { "Unit_id", orderDetail.UnitId },
//                    { "Item_Notes", orderDetail.ItemNotes }
//                };

//                string sqlQuery = @"
//                    INSERT INTO Inv_Orders_Details (
//                        Order_id, Item_Child_id, Bar_Code, Item_Desc, Ord_Qty, Qty_Bonus,
//                        Price, Discount_Percent, Unit_id, Item_Notes
//                    )
//                    VALUES (
//                        @Order_id, @Item_Child_id, @Bar_Code, @Item_Desc, @Ord_Qty, @Qty_Bonus,
//                        @Price, @Discount_Percent, @Unit_id, @Item_Notes
//                    )";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, parameters, Password);
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating order detail");
//                throw;
//            }
//        }

//        public async Task<int> GetNextOrderNumberAsync()
//        {
//            try
//            {
//                string sqlQuery = "SELECT TOP 1 Order_no FROM Inv_Orders_Master ORDER BY Order_no DESC";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    var orderNoValue = response.First()["Order_no"];
//                    int lastOrderNo = orderNoValue is JsonElement jsonElement ? jsonElement.GetInt32() : Convert.ToInt32(orderNoValue);
//                    return lastOrderNo + 1;
//                }

//                return 1;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting next order number");
//                throw;
//            }
//        }

//        private Order MapToOrder(Dictionary<string, object> data)
//        {
//            return new Order
//            {
//                OrderId = GetIntValue(data, "orderId"),
//                OrderNumber = GetIntValue(data, "Order_no"),
//                OrderDate = GetDateTimeValue(data, "Order_date") ?? DateTime.Now,
//                ReceivedDate = GetNullableDateTimeValue(data, "rcvd_date"),
//                CustomerId = GetIntValue(data, "Customer_id"),
//                CustomerName = GetStringValue(data, "Customer_Name"),
//                Amount = GetDecimalValue(data, "amount"),
//                DeliveryTerms = GetStringValue(data, "Delivery_Terms"),
//                PaymentTerms = GetStringValue(data, "Payment_Terms"),
//                Status = (OrderStatus)GetIntValue(data, "Status"),
//                Notes = GetStringValue(data, "notes"),
//                SalesmanId = GetNullableIntValue(data, "Salesman_id"),
//                UserId = GetNullableIntValue(data, "User_id")
//            };
//        }

//        private OrderDetail MapToOrderDetail(Dictionary<string, object> data)
//        {
//            return new OrderDetail
//            {
//                OrderDetailId = GetIntValue(data, "Order_Detail_id"),
//                OrderId = GetIntValue(data, "Order_id"),
//                ItemChildId = GetIntValue(data, "Item_Child_id"),
//                ItemDescription = GetStringValue(data, "Item_Desc") ?? string.Empty,
//                OrderQuantity = GetIntValue(data, "Ord_Qty"),
//                BonusQuantity = GetIntValue(data, "Qty_Bonus"),
//                Price = GetDecimalValue(data, "Price"),
//                DiscountPercent = GetDecimalValue(data, "Discount_Percent"),
//                BarCode = GetStringValue(data, "Bar_Code") ?? string.Empty,
//                UnitId = GetNullableIntValue(data, "Unit_id"),
//                ItemNotes = GetStringValue(data, "Item_Notes")
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

//        private int? GetNullableIntValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value) && value != null)
//            {
//                if (value is JsonElement jsonElement && jsonElement.ValueKind != JsonValueKind.Null)
//                {
//                    return jsonElement.GetInt32();
//                }
//                return value == DBNull.Value ? null : Convert.ToInt32(value);
//            }
//            return null;
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

//        private DateTime? GetDateTimeValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value) && value != null)
//            {
//                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
//                {
//                    var dateString = jsonElement.GetString();
//                    return DateTime.TryParse(dateString, out var date) ? date : null;
//                }
//                return value == DBNull.Value ? null : Convert.ToDateTime(value);
//            }
//            return null;
//        }

//        private DateTime? GetNullableDateTimeValue(Dictionary<string, object> data, string key)
//        {
//            return GetDateTimeValue(data, key);
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