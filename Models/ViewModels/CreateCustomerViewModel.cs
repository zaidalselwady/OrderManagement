using System.ComponentModel.DataAnnotations;

namespace Order_Management_System.Models.ViewModels
{

    public class CreateCustomerViewModel
    {
        [Required(ErrorMessage = "English name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string NameEnglish { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Arabic name cannot exceed 100 characters")]
        public string? NameArabic { get; set; }

        public string? CustomerNumber { get; set; }
        public string? CountryId { get; set; }
        public string? CityId { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
        public decimal? DiscountPercent { get; set; }

        public string? ContactPerson { get; set; }
        public string? Address1 { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Fax { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        public string? Website { get; set; }
        public string? ZipCode { get; set; }
        public string? POBox { get; set; }
        public bool IsReleaseTax { get; set; }
        public string? ReleaseNumber { get; set; }
        public DateTime? ReleaseExpiryDate { get; set; }
        public bool IsProjectAccount { get; set; }
        public int? SalesmanId { get; set; }
    }
}
