// Replace your Repositories/Implementations/CustomerRepository.cs with this:
using Microsoft.EntityFrameworkCore;
using Order_Management_System.Models.Domain;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Data;
using System.Data;

namespace Order_Management_System.Repositories.Implementations
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly OrderManagementDbContext _context;
        private readonly ILogger<CustomerRepository> _logger;

        public CustomerRepository(OrderManagementDbContext context, ILogger<CustomerRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Cust_Sup_id, Name_e, Name_a, Cust_Sup_no, Country_id, City_id, 
                           Discount_Percent, Contact_Person, Address, Phone1, Phone2, Fax, 
                           E_mail, Web_site, Zip_Code, P_O_Box, Is_Release_Tax, Release_No,
                           Release_Expiry_Date, Is_Project_Account, Salesman_id
                    FROM Cust_Sup 
                    ORDER BY Name_e";

                using var reader = await command.ExecuteReaderAsync();
                var customers = new List<Customer>();

                while (await reader.ReadAsync())
                {
                    customers.Add(new Customer
                    {
                        CustSupId = Convert.ToInt32(reader["Cust_Sup_id"]),
                        NameEnglish = reader["Name_e"]?.ToString() ?? "",
                        NameArabic = reader["Name_a"]?.ToString(),
                        CustomerNumber = reader["Cust_Sup_no"]?.ToString(),
                        CountryId = reader["Country_id"]?.ToString(),
                        CityId = reader["City_id"]?.ToString(),
                        DiscountPercent = reader.IsDBNull("Discount_Percent") ? null : Convert.ToDecimal(reader["Discount_Percent"]),
                        ContactPerson = reader["Contact_Person"]?.ToString(),
                        Address1 = reader["Address"]?.ToString(),
                        Phone1 = reader["Phone1"]?.ToString(),
                        Phone2 = reader["Phone2"]?.ToString(),
                        Fax = reader["Fax"]?.ToString(),
                        Email = reader["E_mail"]?.ToString(),
                        Website = reader["Web_site"]?.ToString(),
                        ZipCode = reader["Zip_Code"]?.ToString(),
                        POBox = reader["P_O_Box"]?.ToString(),
                        IsReleaseTax = !reader.IsDBNull("Is_Release_Tax") && Convert.ToBoolean(reader["Is_Release_Tax"]),
                        ReleaseNumber = reader["Release_No"]?.ToString(),
                        ReleaseExpiryDate = reader.IsDBNull("Release_Expiry_Date") ? null : Convert.ToDateTime(reader["Release_Expiry_Date"]),
                        IsProjectAccount = !reader.IsDBNull("Is_Project_Account") && Convert.ToBoolean(reader["Is_Project_Account"]),
                        SalesmanId = reader.IsDBNull("Salesman_id") ? null : Convert.ToInt32(reader["Salesman_id"])
                    });
                }

                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Cust_Sup_id, Name_e, Name_a, Cust_Sup_no, Country_id, City_id, 
                           Discount_Percent, Contact_Person, Address, Phone1, Phone2, Fax, 
                           E_mail, Web_site, Zip_Code, P_O_Box, Is_Release_Tax, Release_No,
                           Release_Expiry_Date, Is_Project_Account, Salesman_id
                    FROM Cust_Sup 
                    WHERE Cust_Sup_id = @customerId";

                var customerIdParam = command.CreateParameter();
                customerIdParam.ParameterName = "@customerId";
                customerIdParam.Value = customerId;
                command.Parameters.Add(customerIdParam);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new Customer
                    {
                        CustSupId = Convert.ToInt32(reader["Cust_Sup_id"]),
                        NameEnglish = reader["Name_e"]?.ToString() ?? "",
                        NameArabic = reader["Name_a"]?.ToString(),
                        CustomerNumber = reader["Cust_Sup_no"]?.ToString(),
                        CountryId = reader["Country_id"]?.ToString(),
                        CityId = reader["City_id"]?.ToString(),
                        DiscountPercent = reader.IsDBNull("Discount_Percent") ? null : Convert.ToDecimal(reader["Discount_Percent"]),
                        ContactPerson = reader["Contact_Person"]?.ToString(),
                        Address1 = reader["Address"]?.ToString(),
                        Phone1 = reader["Phone1"]?.ToString(),
                        Phone2 = reader["Phone2"]?.ToString(),
                        Fax = reader["Fax"]?.ToString(),
                        Email = reader["E_mail"]?.ToString(),
                        Website = reader["Web_site"]?.ToString(),
                        ZipCode = reader["Zip_Code"]?.ToString(),
                        POBox = reader["P_O_Box"]?.ToString(),
                        IsReleaseTax = !reader.IsDBNull("Is_Release_Tax") && Convert.ToBoolean(reader["Is_Release_Tax"]),
                        ReleaseNumber = reader["Release_No"]?.ToString(),
                        ReleaseExpiryDate = reader.IsDBNull("Release_Expiry_Date") ? null : Convert.ToDateTime(reader["Release_Expiry_Date"]),
                        IsProjectAccount = !reader.IsDBNull("Is_Project_Account") && Convert.ToBoolean(reader["Is_Project_Account"]),
                        SalesmanId = reader.IsDBNull("Salesman_id") ? null : Convert.ToInt32(reader["Salesman_id"])
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<int> CreateCustomerAsync(Customer customer)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Cust_Sup (
                        Name_e, Name_a, Cust_Sup_no, Country_id, City_id, Discount_Percent,
                        Contact_Person, Address, Phone1, Phone2, Fax, E_mail,
                        Web_site, Zip_Code, P_O_Box, Is_Release_Tax, Release_No,
                        Release_Expiry_Date, Is_Project_Account, Salesman_id
                    )
                    VALUES (
                        @Name_e, @Name_a, @Cust_Sup_no, @Country_id, @City_id, @Discount_Percent,
                        @Contact_Person, @Address, @Phone1, @Phone2, @Fax, @E_mail,
                        @Web_site, @Zip_Code, @P_O_Box, @Is_Release_Tax, @Release_No,
                        @Release_Expiry_Date, @Is_Project_Account, @Salesman_id
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                // Add parameters
                command.Parameters.Add(CreateParameter(command, "@Name_e", customer.NameEnglish));
                command.Parameters.Add(CreateParameter(command, "@Name_a", customer.NameArabic));
                command.Parameters.Add(CreateParameter(command, "@Cust_Sup_no", customer.CustomerNumber));
                command.Parameters.Add(CreateParameter(command, "@Country_id", customer.CountryId));
                command.Parameters.Add(CreateParameter(command, "@City_id", customer.CityId));
                command.Parameters.Add(CreateParameter(command, "@Discount_Percent", customer.DiscountPercent));
                command.Parameters.Add(CreateParameter(command, "@Contact_Person", customer.ContactPerson));
                command.Parameters.Add(CreateParameter(command, "@Address", customer.Address1));
                command.Parameters.Add(CreateParameter(command, "@Phone1", customer.Phone1));
                command.Parameters.Add(CreateParameter(command, "@Phone2", customer.Phone2));
                command.Parameters.Add(CreateParameter(command, "@Fax", customer.Fax));
                command.Parameters.Add(CreateParameter(command, "@E_mail", customer.Email));
                command.Parameters.Add(CreateParameter(command, "@Web_site", customer.Website));
                command.Parameters.Add(CreateParameter(command, "@Zip_Code", customer.ZipCode));
                command.Parameters.Add(CreateParameter(command, "@P_O_Box", customer.POBox));
                command.Parameters.Add(CreateParameter(command, "@Is_Release_Tax", customer.IsReleaseTax));
                command.Parameters.Add(CreateParameter(command, "@Release_No", customer.ReleaseNumber));
                command.Parameters.Add(CreateParameter(command, "@Release_Expiry_Date", customer.ReleaseExpiryDate));
                command.Parameters.Add(CreateParameter(command, "@Is_Project_Account", customer.IsProjectAccount));
                command.Parameters.Add(CreateParameter(command, "@Salesman_id", customer.SalesmanId));

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw;
            }
        }

        public async Task<bool> IsCustomerNumberExistsAsync(string customerNumber)
        {
            try
            {
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Cust_Sup WHERE Cust_Sup_no = @customerNumber";

                var customerNumberParam = command.CreateParameter();
                customerNumberParam.ParameterName = "@customerNumber";
                customerNumberParam.Value = customerNumber ?? (object)DBNull.Value;
                command.Parameters.Add(customerNumberParam);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if customer number exists: {CustomerNumber}", customerNumber);
                throw;
            }
        }

        private static object CreateParameter(System.Data.Common.DbCommand command, string paramName, object? value)
        {
            var param = command.CreateParameter();
            param.ParameterName = paramName;
            param.Value = value ?? DBNull.Value;
            return param;
        }
    }
}





//using System.Text.Json;
//using Order_Management_System.Models.Domain;
//using Microsoft.Extensions.Logging;
//using Order_Management_System.Models.Domain;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Services.Interfaces;

//namespace Order_Management_System.Repositories.Implementations
//{

//    public class CustomerRepository : ICustomerRepository
//    {
//        private readonly ISoapClient _soapClient;
//        private readonly ILogger<CustomerRepository> _logger;
//        private const string Password = "OptimalPass_optimaljo05";

//        public CustomerRepository(ISoapClient soapClient, ILogger<CustomerRepository> logger)
//        {
//            _soapClient = soapClient ?? throw new ArgumentNullException(nameof(soapClient));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<List<Customer>> GetCustomersAsync()
//        {
//            try
//            {
//                string sqlQuery = "SELECT * FROM Cust_Sup ORDER BY Name_e";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response)
//                {
//                    return response.Select(MapToCustomer).ToList();
//                }

//                return new List<Customer>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting customers");
//                throw;
//            }
//        }

//        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
//        {
//            try
//            {
//                string sqlQuery = $"SELECT * FROM Cust_Sup WHERE Cust_Sup_id = {customerId}";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, null, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    return MapToCustomer(response.First());
//                }

//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", customerId);
//                throw;
//            }
//        }

//        public async Task<int> CreateCustomerAsync(Customer customer)
//        {
//            try
//            {
//                var parameters = new Dictionary<string, object>
//                {
//                    { "Name_e", customer.NameEnglish },
//                    { "Name_a", customer.NameArabic },
//                    { "Cust_Sup_no", customer.CustomerNumber },
//                    { "Country_id", customer.CountryId },
//                    { "City_id", customer.CityId },
//                    { "Discount_Percent", customer.DiscountPercent },
//                    { "Contact_Person", customer.ContactPerson },
//                    { "Address", customer.Address1 },
//                    { "Phone1", customer.Phone1 },
//                    { "Phone2", customer.Phone2 },
//                    { "Fax", customer.Fax },
//                    { "E_mail", customer.Email },
//                    { "Web_site", customer.Website },
//                    { "Zip_Code", customer.ZipCode },
//                    { "P_O_Box", customer.POBox },
//                    { "Is_Release_Tax", customer.IsReleaseTax },
//                    { "Release_No", customer.ReleaseNumber },
//                    { "Release_Expiry_Date", customer.ReleaseExpiryDate?.ToString("yyyy-MM-dd") },
//                    { "Is_Project_Account", customer.IsProjectAccount },
//                    { "Salesman_id", customer.SalesmanId }
//                };

//                string sqlQuery = @"
//                    INSERT INTO Cust_Sup (
//                        Name_e, Name_a, Cust_Sup_no, Country_id, City_id, Discount_Percent,
//                        Contact_Person, Address, Phone1, Phone2, Fax, E_mail,
//                        Web_site, Zip_Code, P_O_Box, Is_Release_Tax, Release_No,
//                        Release_Expiry_Date, Is_Project_Account, Salesman_id
//                    )
//                    VALUES (
//                        @Name_e, @Name_a, @Cust_Sup_no, @Country_id, @City_id, @Discount_Percent,
//                        @Contact_Person, @Address, @Phone1, @Phone2, @Fax, @E_mail,
//                        @Web_site, @Zip_Code, @P_O_Box, @Is_Release_Tax, @Release_No,
//                        @Release_Expiry_Date, @Is_Project_Account, @Salesman_id
//                    );
//                    SELECT SCOPE_IDENTITY() AS Cust_Sup_id;";

//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, parameters, Password);

//                if (result is List<Dictionary<string, object>> response && response.Any())
//                {
//                    var customerIdValue = response.First()["Cust_Sup_id"];
//                    return customerIdValue is JsonElement jsonElement ? jsonElement.GetInt32() : Convert.ToInt32(customerIdValue);
//                }

//                throw new InvalidOperationException("Failed to create customer - no ID returned");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating customer");
//                throw;
//            }
//        }

//        public async Task<bool> IsCustomerNumberExistsAsync(string customerNumber)
//        {
//            try
//            {
//                var parameters = new Dictionary<string, object>
//                {
//                    { "Cust_Sup_no", customerNumber }
//                };

//                string sqlQuery = "SELECT COUNT(*) AS Count FROM Cust_Sup WHERE Cust_Sup_no = @Cust_Sup_no";
//                var result = await _soapClient.ExecuteQueryWithParametersAsync(sqlQuery, parameters, Password);

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
//                _logger.LogError(ex, "Error checking if customer number exists: {CustomerNumber}", customerNumber);
//                throw;
//            }
//        }

//        private Customer MapToCustomer(Dictionary<string, object> data)
//        {
//            return new Customer
//            {
//                CustSupId = GetIntValue(data, "Cust_Sup_id"),
//                NameEnglish = GetStringValue(data, "Name_e") ?? string.Empty,
//                NameArabic = GetStringValue(data, "Name_a"),
//                CustomerNumber = GetStringValue(data, "Cust_Sup_no"),
//                CountryId = GetStringValue(data, "Country_id"),
//                CityId = GetStringValue(data, "City_id"),
//                DiscountPercent = GetNullableDecimalValue(data, "Discount_Percent"),
//                ContactPerson = GetStringValue(data, "Contact_Person"),
//                Address1 = GetStringValue(data, "Address"),
//                Phone1 = GetStringValue(data, "Phone1"),
//                Phone2 = GetStringValue(data, "Phone2"),
//                Fax = GetStringValue(data, "Fax"),
//                Email = GetStringValue(data, "E_mail"),
//                Website = GetStringValue(data, "Web_site"),
//                ZipCode = GetStringValue(data, "Zip_Code"),
//                POBox = GetStringValue(data, "P_O_Box"),
//                IsReleaseTax = GetBoolValue(data, "Is_Release_Tax"),
//                ReleaseNumber = GetStringValue(data, "Release_No"),
//                ReleaseExpiryDate = GetNullableDateTimeValue(data, "Release_Expiry_Date"),
//                IsProjectAccount = GetBoolValue(data, "Is_Project_Account"),
//                SalesmanId = GetNullableIntValue(data, "Salesman_id")
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

//        private DateTime? GetNullableDateTimeValue(Dictionary<string, object> data, string key)
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

//        private decimal? GetNullableDecimalValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value) && value != null)
//            {
//                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
//                {
//                    return jsonElement.GetDecimal();
//                }
//                return value == DBNull.Value ? null : Convert.ToDecimal(value);
//            }
//            return null;
//        }

//        private bool GetBoolValue(Dictionary<string, object> data, string key)
//        {
//            if (data.TryGetValue(key, out var value))
//            {
//                if (value is JsonElement jsonElement)
//                {
//                    return jsonElement.ValueKind == JsonValueKind.True ||
//                           jsonElement.ValueKind == JsonValueKind.Number && jsonElement.GetInt32() == 1;
//                }
//                return Convert.ToBoolean(value);
//            }
//            return false;
//        }
//    }


//}