// Models/Client.cs
using System.ComponentModel.DataAnnotations;

// Models/Client.cs
namespace AtelierProject.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العميل مطلوب")]
        [Display(Name = "اسم العميل")]
        public string Name { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        [Phone]
        public string Phone { get; set; }

        [Display(Name = "الرقم القومي")]
        public string? NationalId { get; set; }

        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        // خاصية لحظر التعامل مع العميل
        [Display(Name = "القائمة السوداء")]
        public bool IsBlacklisted { get; set; } = false;

        // تاريخ التسجيل
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}