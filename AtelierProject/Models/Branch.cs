using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الفرع مطلوب")]
        [Display(Name = "اسم الفرع")]
        [MaxLength(100)]
        public string Name { get; set; } // مثلا: فرع القاهرة، فرع الإسكندرية

        // --- الإضافة الجديدة ---
        [Display(Name = "الاسم التجاري (للفواتير)")]
        [MaxLength(150)]
        public string? CommercialName { get; set; } // مثال: أتيليه مارينا - للأزياء الراقية
                                                    // -----------------------

        [Display(Name = "العنوان")]
        [MaxLength(200)]
        public string Address { get; set; }

        [Display(Name = "رقم الهاتف")]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Display(Name = "الفرع الرئيسي")]
        public bool IsMain { get; set; } = false; // لتحديد الفرع الرئيسي للنظام

    }
}