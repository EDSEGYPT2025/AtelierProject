using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Models
{
    public enum SalonAppointmentStatus
    {
        [Display(Name = "قيد الانتظار")]
        Pending,

        [Display(Name = "مؤكد")]
        Confirmed,

        [Display(Name = "مكتمل")]
        Completed,

        [Display(Name = "ملغي")]
        Cancelled
    }
}