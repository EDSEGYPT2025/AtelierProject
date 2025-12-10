using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class ProductItem
    {
        public int Id { get; set; }

        [Display(Name = "كود القطعة")]
        public string? UniqueCode { get; set; }

        [Display(Name = "اللون")]
        public string? Color { get; set; }

        [Required(ErrorMessage = "المقاس مطلوب")]
        [Display(Name = "المقاس")]
        public string Size { get; set; }

        [Display(Name = "الباركود")]
        public string? Barcode { get; set; }

        // --- تم إرجاع هذه الخاصية لحل الخطأ ---
        [Display(Name = "الحالة")]
        public ItemStatus Status { get; set; } = ItemStatus.Available;
        // -------------------------------------

        [Display(Name = "متاح للاستخدام")]
        public bool IsAvailable { get; set; } = true;

        // العلاقة مع الموديل الأب
        public int ProductDefinitionId { get; set; }
        public ProductDefinition ProductDefinition { get; set; }

        // تحديد الفرع الذي توجد فيه هذه القطعة حالياً
        public int? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch Branch { get; set; }
    }
}