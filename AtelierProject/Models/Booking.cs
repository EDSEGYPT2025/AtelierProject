using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Display(Name = "العميل")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [Display(Name = "تاريخ الحجز")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "تاريخ الاستلام مطلوب")]
        [Display(Name = "تاريخ الاستلام")]
        public DateTime PickupDate { get; set; }

        [Required(ErrorMessage = "تاريخ الإرجاع مطلوب")]
        [Display(Name = "تاريخ الإرجاع")]
        public DateTime ReturnDate { get; set; }

        // --- الأمور المالية ---

        [Display(Name = "إجمالي مبلغ الإيجار")] // تم تغيير الاسم للتوضيح
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // ✅ 1. إضافة عمود الخصم
        [Display(Name = "قيمة الخصم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;


        [Display(Name = "العربون المدفوع")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Display(Name = "مبلغ التأمين المستلم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InsuranceAmount { get; set; } = 0; // المبلغ الذي دفعه العميل كتأمين

        [Display(Name = "المبلغ المخصوم من التأمين")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InsuranceDeduction { get; set; } = 0; // المبلغ الذي أخذه الأتيليه كغرامة

        // --- الحالة والملاحظات ---

        [Display(Name = "حالة الحجز")]
        public BookingStatus Status { get; set; } = BookingStatus.New;

        [Display(Name = "ملاحظات وتعديلات المقاسات")]
        [DataType(DataType.MultilineText)] // ليظهر كمربع نص كبير
        public string? Notes { get; set; }

        public virtual ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();

        // --- خصائص محسوبة (للعرض فقط) ---

        // المبلغ المتبقي من الإيجار (بدون التأمين)
        // --- خصائص محسوبة (للعرض فقط) ---

        // المبلغ المتبقي من الإيجار (بدون التأمين)
        // ✅ 2. تعديل معادلة المتبقي لتشمل الخصم
        [NotMapped]
        public decimal RemainingRentalAmount
        {
            get
            {
                if (Status == BookingStatus.Cancelled)
                {
                    return 0;
                }

                // المعادلة: (الإجمالي - الخصم) - المدفوع
                var netTotal = TotalAmount - Discount;

                // منع ظهور المتبقي بالسالب (في حالة كان الخصم + المدفوع أكبر من الإجمالي)
                var remaining = netTotal - PaidAmount;
                return remaining < 0 ? 0 : remaining;
            }
        }

        // خاصية إضافية مفيدة للعرض: صافي المبلغ بعد الخصم
        [NotMapped]
        public decimal NetAmount => TotalAmount - Discount;


        // صافي المبلغ المسترد للعميل عند الإرجاع
        // (التأمين المدفوع - الخصم)
        [NotMapped]
        public decimal RefundAmount => InsuranceAmount - InsuranceDeduction;

        // ربط الحجز بالفرع
        public int? BranchId { get; set; } // جعلناه يقبل null مؤقتاً لتجنب مشاكل البيانات القديمة
        [ForeignKey("BranchId")]
        public Branch Branch { get; set; }
    }
}