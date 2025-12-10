// Models/Enums.cs
namespace AtelierProject.Models
{
    public enum ItemStatus
    {
        Available = 1,    // متاحة
        Reserved = 2,     // محجوزة
        Rented = 3,       // مؤجرة حالياً
        Maintenance = 4,  // صيانة/غسيل
        Lost = 5          // مفقودة/تالفة
    }
}