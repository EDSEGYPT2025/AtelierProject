// Models/BookingRentalItem.cs (رأس الفاتورة)
using System.ComponentModel.DataAnnotations.Schema;

// Models/BookingRentalItem.cs (تفاصيل تأجير الملابس)
namespace AtelierProject.Models
{
    public class BookingRentalItem
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int ProductDefinitionId { get; set; } // الموديل المحجوز
        public ProductDefinition ProductDefinition { get; set; }

        public int? ProductItemId { get; set; } // القطعة التي استلمها (قد تكون null وقت الحجز)
        public ProductItem? ProductItem { get; set; }

        public DateTime PickupDate { get; set; } // يوم الاستلام
        public DateTime ReturnDate { get; set; } // يوم الإرجاع المتوقع

        [Column(TypeName = "decimal(18,2)")]
        public decimal AgreedPrice { get; set; } // السعر وقت الاتفاق
    }
}
