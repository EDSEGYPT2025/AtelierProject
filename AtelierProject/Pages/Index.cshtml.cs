using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AtelierProject.Pages
{
    // أزلنا [Authorize] لكي تفتح الصفحة للجميع
    public class IndexModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult OnGet()
        {
            // التحقق: هل المستخدم مسجل دخول بالفعل؟
            if (_signInManager.IsSignedIn(User))
            {
                // نعم مسجل -> وجهه فوراً إلى لوحة التحكم الداخلية (مثلاً قائمة الحجوزات)
                // يمكنك تغيير الوجهة حسب رغبتك، مثلاً "/Bookings/Index" أو صفحة داشبورد مخصصة
                return RedirectToPage("/Bookings/Index");
            }

            // لا، غير مسجل -> اعرض صفحة الهبوط الإعلانية
            return Page();
        }
    }
}