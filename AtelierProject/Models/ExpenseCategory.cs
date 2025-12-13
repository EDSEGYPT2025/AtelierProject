using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Models
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم البند مطلوب")]
        [Display(Name = "نوع المصروف")]
        public string Name { get; set; } = string.Empty; // مثل: كهرباء، رواتب، بوفيه

        // 🔒 ربط التصنيف بالفرع (عزل الصلاحيات)
        // لن يرى هذا التصنيف إلا موظفو هذا الفرع
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}