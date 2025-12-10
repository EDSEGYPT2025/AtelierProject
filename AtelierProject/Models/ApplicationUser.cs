using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtelierProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string FullName { get; set; } // اسم الموظف بالكامل

        // ربط الموظف بالفرع
        public int? BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch Branch { get; set; }

        // صلاحيات الأقسام (كما هو مطلوب في التحليل)
        public bool CanAccessMenSection { get; set; } = false;    // قسم الرجالي
        public bool CanAccessWomenSection { get; set; } = false;  // قسم الحريمي
        public bool CanAccessBeautySection { get; set; } = false; // قسم الكوافير

        // حالة الموظف (بدلاً من حذفه، نقوم بتعطيله فقط)
        public bool IsActive { get; set; } = true;
    }
}