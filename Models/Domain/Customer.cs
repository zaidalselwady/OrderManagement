using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order_Management_System.Models.Domain
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustSupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEnglish { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NameArabic { get; set; }

        [MaxLength(50)]
        public string? CustomerNumber { get; set; }

        public string? CountryId { get; set; }
        public string? CityId { get; set; }
        public decimal? DiscountPercent { get; set; }
        public string? ContactPerson { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }

        [MaxLength(20)]
        public string? Phone1 { get; set; }

        [MaxLength(20)]
        public string? Phone2 { get; set; }

        public string? Fax { get; set; }

        [MaxLength(100)]
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
