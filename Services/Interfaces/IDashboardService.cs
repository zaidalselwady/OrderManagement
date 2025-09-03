using Order_Management_System.Models.Domain;
using Order_Management_System.Models.DTOs;
using Order_Management_System.Models.ViewModels;

namespace Order_Management_System.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(int userId);
}

