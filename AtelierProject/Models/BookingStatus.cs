// Models/Enums.cs
namespace AtelierProject.Models
{
    public enum BookingStatus
    {
        New = 1,        // حجز جديد (مبدئي)
        Confirmed = 2,  // تم تأكيد الحجز (دفع عربون)
        PickedUp = 3,   // تم الاستلام (خرج من الأتيليه)
        Returned = 4,   // تم الإرجاع (انتهت العملية)
        Cancelled = 5,  // ملغي
        Late = 6        // متأخر عن موعد التسليم
    }
}