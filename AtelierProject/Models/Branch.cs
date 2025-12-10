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

        [Display(Name = "العنوان")]
        [MaxLength(200)]
        public string Address { get; set; }

        [Display(Name = "رقم الهاتف")]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Display(Name = "الفرع الرئيسي")]
        public bool IsMain { get; set; } = false; // لتحديد الفرع الرئيسي للنظام

        // سنحتاج هذه القوائم لاحقاً للربط (Navigation Properties)
        // public ICollection<Booking> Bookings { get; set; }
        // public ICollection<ApplicationUser> Users { get; set; }
    }
}