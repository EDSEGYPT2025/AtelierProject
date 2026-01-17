using System.ComponentModel.DataAnnotations; // 👈 مكتبة ضرورية للعرض

namespace AtelierProject.Models
{
    public enum BookingStatus
    {
        [Display(Name = "حجز جديد (مبدئي)")]
        New = 1,

        [Display(Name = "تم تأكيد الحجز (دفع عربون)")]
        Confirmed = 2,

        [Display(Name = "تم الاستلام (خرج من الأتيليه)")]
        PickedUp = 3,

        [Display(Name = "تم الإرجاع (انتهت العملية)")]
        Returned = 4,

        [Display(Name = "ملغي")]
        Cancelled = 5,

        [Display(Name = "متأخر عن موعد التسليم")]
        Late = 6
    }
}