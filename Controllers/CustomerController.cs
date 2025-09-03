using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order_Management_System.Models.ViewModels;
using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Controllers
{

    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _customerService.GetCustomersAsync();
                return Json(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(500, new { error = $"Failed to fetch customers: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return NotFound(new { error = "Customer not found" });
                }
                return Json(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
                return StatusCode(500, new { error = $"Failed to fetch customer: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid customer data", errors = ModelState });
            }

            try
            {
                var result = await _customerService.CreateCustomerAsync(model);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        customerId = result.CustomerId
                    });
                }
                else
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, new { error = $"Failed to create customer: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckCustomerNumber(string customerNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(customerNumber))
                {
                    return Json(new { exists = false });
                }

                var exists = await _customerService.IsCustomerNumberExistsAsync(customerNumber);
                return Json(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer number: {CustomerNumber}", customerNumber);
                return StatusCode(500, new { error = $"Failed to check customer number: {ex.Message}" });
            }
        }
    }


}