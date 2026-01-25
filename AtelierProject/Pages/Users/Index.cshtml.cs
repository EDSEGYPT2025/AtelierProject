using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AtelierProject.Pages.Users
{
    // [Authorize] تضمن أن المستخدم مسجل دخول أولاً
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<ApplicationUser> Users { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. معرفة المستخدم الحالي
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // 2. ⛔ التحقق الأمني: إذا كان المستخدم تابعاً لفرع (ليس المدير العام)
            if (currentUser.BranchId != null)
            {
                // إعادته للوحة التحكم الرئيسية مع رسالة خطأ (اختياري)
                return RedirectToPage("/Index");
                // أو يمكن استخدام: return Forbid();
            }

            // 3. جلب المستخدمين (يتم التنفيذ فقط للمدير العام)
            Users = await _context.Users
                .Include(u => u.Branch)
                .OrderBy(u => u.BranchId) // ترتيب حسب الفرع
                .ToListAsync();

            return Page();
        }
    }
}