// Models/BookingServiceItem.cs (رأس الفاتورة)
using System.ComponentModel.DataAnnotations.Schema;

// Models/BookingServiceItem.cs (تفاصيل حجز الكوافير)
namespace AtelierProject.Models
{
    public class BookingServiceItem
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int ServiceId { get; set; }
        public Service Service { get; set; }

        public DateTime AppointmentDate { get; set; } // موعد الجلسة

        [Column(TypeName = "decimal(18,2)")]
        public decimal AgreedPrice { get; set; }
    }
}