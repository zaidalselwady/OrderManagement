using Order_Management_System.Models.Domain;

namespace Order_Management_System.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByCredentialsAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> IsUserExistsAsync(string username);
}

