using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;

namespace Order_Management_System.Services.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResultDto> AuthenticateAsync(string username, string password);
    Task<bool> IsUserLoggedInAsync(int userId);
    Task<string> GetUserNameAsync(int userId);
}







