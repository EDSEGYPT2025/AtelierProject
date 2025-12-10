using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class SalonAppointmentItem
    {
        public int Id { get; set; }

        public int SalonAppointmentId { get; set; }
        [ForeignKey("SalonAppointmentId")]
        public virtual SalonAppointment SalonAppointment { get; set; }

        public int SalonServiceId { get; set; }
        [ForeignKey("SalonServiceId")]
        public virtual SalonService SalonService { get; set; }

        // نسجل السعر وقت الحجز تحسباً لتغير سعر الخدمة مستقبلاً
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
    }
}