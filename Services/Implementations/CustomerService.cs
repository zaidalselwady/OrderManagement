using Microsoft.Extensions.Logging;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Services.Interfaces;
using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Services.Implementations
{
   
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                return await _customerRepository.GetCustomersAsync();
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
                return await _customerRepository.GetCustomerByIdAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<CustomerCreationResultDto> CreateCustomerAsync(CreateCustomerViewModel model)
        {
            try
            {
                // Check if customer number already exists
                if (!string.IsNullOrEmpty(model.CustomerNumber))
                {
                    var exists = await IsCustomerNumberExistsAsync(model.CustomerNumber);
                    if (exists)
                    {
                        return new CustomerCreationResultDto
                        {
                            Success = false,
                            ErrorMessage = $"Customer number {model.CustomerNumber} already exists"
                        };
                    }
                }

                // Create customer entity
                var customer = new Customer
                {
                    NameEnglish = model.NameEnglish,
                    NameArabic = model.NameArabic,
                    CustomerNumber = model.CustomerNumber,
                    CountryId = model.CountryId,
                    CityId = model.CityId,
                    DiscountPercent = model.DiscountPercent,
                    ContactPerson = model.ContactPerson,
                    Address1 = model.Address1,
                    Phone1 = model.Phone1,
                    Phone2 = model.Phone2,
                    Fax = model.Fax,
                    Email = model.Email,
                    Website = model.Website,
                    ZipCode = model.ZipCode,
                    POBox = model.POBox,
                    IsReleaseTax = model.IsReleaseTax,
                    ReleaseNumber = model.ReleaseNumber,
                    ReleaseExpiryDate = model.ReleaseExpiryDate,
                    IsProjectAccount = model.IsProjectAccount,
                    SalesmanId = model.SalesmanId
                };

                var customerId = await _customerRepository.CreateCustomerAsync(customer);

                _logger.LogInformation("Customer created successfully. CustomerId: {CustomerId}", customerId);

                return new CustomerCreationResultDto
                {
                    Success = true,
                    CustomerId = customerId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return new CustomerCreationResultDto
                {
                    Success = false,
                    ErrorMessage = $"Failed to create customer: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsCustomerNumberExistsAsync(string customerNumber)
        {
            try
            {
                return await _customerRepository.IsCustomerNumberExistsAsync(customerNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if customer number exists: {CustomerNumber}", customerNumber);
                throw;
            }
        }
    }

   
}