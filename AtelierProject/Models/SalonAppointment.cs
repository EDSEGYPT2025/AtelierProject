using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class SalonAppointment
    {
        public int Id { get; set; }

        [Display(Name = "العميل")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        [Required]
        [Display(Name = "موعد الحجز")]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public SalonAppointmentStatus Status { get; set; } = SalonAppointmentStatus.Confirmed;

        // --- الإضافات المالية ---

        // 1. إجمالي الفاتورة (حقل حقيقي يخزن في قاعدة البيانات)
        [Display(Name = "إجمالي الفاتورة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        // 2. العربون / المدفوع
        [Display(Name = "العربون / المدفوع")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        // 3. المتبقي (حقل محسوب فقط، لا يخزن)
        // يقوم بطرح المدفوع من الإجمالي المخزن
        [NotMapped]
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        // قائمة الخدمات في هذا الحجز
        public virtual ICollection<SalonAppointmentItem> Items { get; set; } = new List<SalonAppointmentItem>();

        // ربط موعد الصالون بالفرع
        public int? BranchId { get; set; }
        [ForeignKey("BranchId")]
        public Branch Branch { get; set; }
    }
}