using Order_Management_System.Models.Domain;

namespace Order_Management_System.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int customerId);
    Task<int> CreateCustomerAsync(Customer customer);
    Task<bool> IsCustomerNumberExistsAsync(string customerNumber);
}



