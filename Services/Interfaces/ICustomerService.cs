using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;

namespace Order_Management_System.Services.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int customerId);
    Task<CustomerCreationResultDto> CreateCustomerAsync(CreateCustomerViewModel model);
    Task<bool> IsCustomerNumberExistsAsync(string customerNumber);
}

