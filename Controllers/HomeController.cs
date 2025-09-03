
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDashboardService _dashboardService;

        public HomeController(
            ILogger<HomeController> logger,
            IAuthenticationService authenticationService,
            IDashboardService dashboardService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("User_ID");
                if (userId == null)
                {
                    _logger.LogInformation("User not logged in, redirecting to Login");
                    return RedirectToAction("Login", new { message = "Session expired. Please log in again." });
                }

                var dashboardData = await _dashboardService.GetDashboardDataAsync(userId.Value);
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return StatusCode(500, "Internal Server Error");
            }
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("User_ID") != null)
            {
                _logger.LogInformation("User already logged in, redirecting to Index");
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errorMessage = "Invalid input. Please check your username and password." });
            }

            try
            {
                var result = await _authenticationService.AuthenticateAsync(model.UserName, model.Password);

                if (result.Success)
                {
                    HttpContext.Session.SetInt32("User_ID", result.UserId!.Value);
                    HttpContext.Session.SetInt32("Company_ID", result.CompanyId!.Value);
                    HttpContext.Session.SetString("User_Name", result.UserName!);

                    return Json(new { success = true });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        errorMessage = result.ErrorMessage,
                        errorType = result.ErrorType
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {UserName}", model.UserName);
                return Json(new
                {
                    success = false,
                    errorMessage = "An error occurred while logging in. Please try again.",
                    errorType = "SERVER_ERROR"
                });
            }
        }

        public IActionResult Logout()
        {
            try
            {
                _logger.LogInformation("User logging out. Clearing session.");
                HttpContext.Session.Clear();
                return RedirectToAction("Login", new { message = "You have been logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Login", new { message = "An error occurred while logging out." });
            }
        }

        [HttpGet]
        public IActionResult CheckLogin()
        {
            bool isLoggedIn = HttpContext.Session.GetInt32("User_ID") != null;
            return Json(new { isLoggedIn });
        }

        public IActionResult Error()
        {
            return View();
        }
    }

 }