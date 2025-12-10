// Models/Service.cs (تعريف الموديل: بدلة X، فستان Y)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



// Models/Service.cs (خدمات الكوافير)
namespace AtelierProject.Models
{
    public class Service
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // ميك أب، شعر، الخ

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int DurationMinutes { get; set; } // وقت التنفيذ بالدقائق
    }
}