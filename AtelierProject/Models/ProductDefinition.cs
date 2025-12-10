// Models/ProductDefinition.cs (تعريف الموديل: بدلة X، فستان Y)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class ProductDefinition
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الموديل مطلوب")]
        [Display(Name = "اسم الموديل")]
        public string Name { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "يرجى اختيار القسم")]
        [Display(Name = "القسم")]
        public DepartmentType Department { get; set; }

        [Required]
        [Display(Name = "سعر الإيجار")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RentalPrice { get; set; }

        [Display(Name = "مبلغ التأمين/العربون")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        [Display(Name = "كود الموديل")]
        public string? Code { get; set; }

        public virtual ICollection<ProductItem> ProductItems { get; set; } = new List<ProductItem>();

    }
}
