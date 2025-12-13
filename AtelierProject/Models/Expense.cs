using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "تاريخ المصروف")]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(1, 1000000, ErrorMessage = "أدخل قيمة صحيحة")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Display(Name = "الوصف / ملاحظات")]
        public string? Description { get; set; }

        // 🔗 العلاقات
        [Display(Name = "نوع المصروف")]
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        // 🔒 العزل الأمني: المصروف يتبع فرعاً محدداً
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // 🕵️ Audit: من قام بتسجيل المصروف؟
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
    }
}