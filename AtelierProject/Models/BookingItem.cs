using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    // الأصناف داخل الحجز (مثلاً: فستان زفاف + تاج)
    public class BookingItem
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        public int ProductItemId { get; set; }
        [ForeignKey("ProductItemId")]
        public virtual ProductItem ProductItem { get; set; }

        [Display(Name = "سعر الإيجار")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RentalPrice { get; set; } // السعر المتفق عليه لهذه القطعة
    }
}