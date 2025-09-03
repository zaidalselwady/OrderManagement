// Replace your entire Repositories/Implementations/UserRepository.cs with this:
using Microsoft.EntityFrameworkCore;
using Order_Management_System.Models.Domain;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Data;
using System.Data;

namespace Order_Management_System.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly OrderManagementDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(OrderManagementDbContext context, ILogger<UserRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User?> GetUserByCredentialsAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Looking for user: {Username}", username);

                // Use raw SQL to avoid EF type conversion issues
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT User_ID, User_Name, Password, Password_Date, Password_Period, Company_ID 
                    FROM Users 
                    WHERE User_Name = @username AND Password = @password";

                var usernameParam = command.CreateParameter();
                usernameParam.ParameterName = "@username";
                usernameParam.Value = username;
                command.Parameters.Add(usernameParam);

                var passwordParam = command.CreateParameter();
                passwordParam.ParameterName = "@password";
                passwordParam.Value = password;
                command.Parameters.Add(passwordParam);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        UserId = Convert.ToInt32(reader["User_ID"]),
                        UserName = reader["User_Name"]?.ToString() ?? "",
                        Password = reader["Password"]?.ToString() ?? "",
                        PasswordDate = reader.IsDBNull("Password_Date") ? null : Convert.ToDateTime(reader["Password_Date"]),
                        PasswordPeriod = reader.IsDBNull("Password_Period") ? null : Convert.ToInt32(reader["Password_Period"]),
                        CompanyId = reader.IsDBNull("Company_ID") ? null : Convert.ToInt32(reader["Company_ID"])
                    };

                    _logger.LogInformation("User found: {UserId}", user.UserId);
                    return user;
                }
                else
                {
                    _logger.LogWarning("User not found for username: {Username}", username);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by credentials for username: {Username}", username);
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT User_ID, User_Name, Password, Password_Date, Password_Period, Company_ID 
                    FROM Users 
                    WHERE User_ID = @userId";

                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@userId";
                userIdParam.Value = userId;
                command.Parameters.Add(userIdParam);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = Convert.ToInt32(reader["User_ID"]),
                        UserName = reader["User_Name"]?.ToString() ?? "",
                        Password = reader["Password"]?.ToString() ?? "",
                        PasswordDate = reader.IsDBNull("Password_Date") ? null : Convert.ToDateTime(reader["Password_Date"]),
                        PasswordPeriod = reader.IsDBNull("Password_Period") ? null : Convert.ToInt32(reader["Password_Period"]),
                        CompanyId = reader.IsDBNull("Company_ID") ? null : Convert.ToInt32(reader["Company_ID"])
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsUserExistsAsync(string username)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Users WHERE User_Name = @username";

                var usernameParam = command.CreateParameter();
                usernameParam.ParameterName = "@username";
                usernameParam.Value = username;
                command.Parameters.Add(usernameParam);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Username}", username);
                throw;
            }
        }
    }
}











//// Replace Repositories/Implementations/UserRepository.cs
//using Microsoft.EntityFrameworkCore;
//using Order_Management_System.Models.Domain;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Data;

//namespace Order_Management_System.Repositories.Implementations
//{
//    public class UserRepository : IUserRepository
//    {
//        private readonly OrderManagementDbContext _context;
//        private readonly ILogger<UserRepository> _logger;

//        public UserRepository(OrderManagementDbContext context, ILogger<UserRepository> logger)
//        {
//            _context = context ?? throw new ArgumentNullException(nameof(context));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<User?> GetUserByCredentialsAsync(string username, string password)
//        {
//            try
//            {
//                return await _context.Users
//                    .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting user by credentials for username: {Username}", username);
//                throw;
//            }
//        }

//        public async Task<User?> GetUserByIdAsync(int userId)
//        {
//            try
//            {
//                return await _context.Users.FindAsync(userId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
//                throw;
//            }
//        }

//        public async Task<bool> IsUserExistsAsync(string username)
//        {
//            try
//            {
//                return await _context.Users.AnyAsync(u => u.UserName == username);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error checking if user exists: {Username}", username);
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
//    public class UserRepository : IUserRepository
//    {
//        private readonly ISoapClient _soapClient;
//        private readonly ILogger<UserRepository> _logger;
//        private const string Password = "OptimalPass_optimaljo05";

//        public UserRepository(ISoapClient soapClient, ILogger<UserRepository> logger)
//        {
//            _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<User?> GetUserByCredentialsAsync(string username, string password)
//        {
//            try
//            {
//                string escapedUsername = username.Replace("'", "''");
//                string escapedPassword = password.Replace("'", "''");

//                string sqlQuery = $@"
//                    SELECT User_ID, Password_Date, Password_Period, Company_ID, User_Name 
//                    FROM Users 
//                    WHERE User_Name = N'{escapedUsername}' AND Password = N'{escapedPassword}'";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    return MapToUser(response.First());
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting user by credentials for username: {Username}", username);
//                throw;
//            }
//        }

//        public async Task<User?> GetUserByIdAsync(int userId)
//        {
//            try
//            {
//                string sqlQuery = $"SELECT User_ID, User_Name, Company_ID FROM Users WHERE User_ID = {userId}";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    return MapToUser(response.First());
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
//                throw;
//            }
//        }

//        public async Task<bool> IsUserExistsAsync(string username)
//        {
//            try
//            {
//                string escapedUsername = username.Replace("'", "''");
//                string sqlQuery = $"SELECT COUNT(*) AS Count FROM Users WHERE User_Name = N'{escapedUsername}'";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    var countValue = response.First()["Count"];
//                    int count = countValue is JsonElement jsonElement ? jsonElement.GetInt32() : Convert.ToInt32(countValue);
//                    return count > 0;
//                }

//                return false;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error checking if user exists: {Username}", username);
//                throw;
//            }
//        }

//        private User MapToUser(Dictionary<string, object> data)
//        {
//            return new User
//            {
//                UserId = GetIntValue(data, "User_ID"),
//                UserName = GetStringValue(data, "User_Name") ?? string.Empty,
//                PasswordDate = GetDateTimeValue(data, "Password_Date"),
//                PasswordPeriod = GetNullableIntValue(data, "Password_Period"),
//                CompanyId = GetNullableIntValue(data, "Company_ID")
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
//    }

//    }