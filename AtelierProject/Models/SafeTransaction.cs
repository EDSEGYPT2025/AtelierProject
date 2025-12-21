using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class SafeTransaction
    {
        public int Id { get; set; }

        [Display(Name = "التاريخ")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Display(Name = "المبلغ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "نوع الحركة")]
        public TransactionType Type { get; set; }

        [Display(Name = "القسم")]
        public DepartmentType Department { get; set; } // (رجالي، حريمي، كوافير)

        [Display(Name = "الفرع")]
        public int BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [Display(Name = "بيان الحركة")]
        public string Description { get; set; } // (مثلاً: عربون حجز رقم 105)

        [Display(Name = "رقم المرجع")]
        public string? ReferenceId { get; set; } // رقم الحجز أو رقم الفاتورة للرجوع إليها

        public string? CreatedByUserId { get; set; } // الموظف الذي قام بالعملية
    }
}