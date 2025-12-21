using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Models
{
    public enum DepartmentType
    {
        [Display(Name = "قسم الرجال")]
        Men = 1,

        [Display(Name = "قسم السيدات")]
        Women = 2,

        [Display(Name = "صالون التجميل")]
        BeautySalon = 3
    }
}