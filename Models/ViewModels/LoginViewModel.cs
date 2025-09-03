using System.ComponentModel.DataAnnotations;

namespace Order_Management_System.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(50, ErrorMessage = "Password cannot exceed 50 characters")]
        public string Password { get; set; } = string.Empty;
    }

   
}
