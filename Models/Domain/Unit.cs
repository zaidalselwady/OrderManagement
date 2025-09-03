using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order_Management_System.Models.Domain
{
    public class Unit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UnitId { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitDescriptionEnglish { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? UnitDescriptionArabic { get; set; }
    }
}
