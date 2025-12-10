using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class SalonService
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الخدمة مطلوب")]
        [Display(Name = "اسم الخدمة")]
        public string Name { get; set; } // مثال: مكياج عروس، سيشوار

        [Display(Name = "وصف الخدمة")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "سعر الخدمة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "المدة المتوقعة (بالدقائق)")]
        public int DurationMinutes { get; set; } // 30, 60, 90...
    }
}