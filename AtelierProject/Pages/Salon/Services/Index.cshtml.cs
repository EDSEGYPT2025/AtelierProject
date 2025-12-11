using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // هام
using Microsoft.AspNetCore.Authorization; // هام
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Services
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 1. المانجر

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<SalonService> Services { get; set; } = default!;

        [BindProperty]
        public SalonService NewService { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            var query = _context.SalonServices.AsQueryable();

            // 🛑 فلترة الخدمات حسب الفرع
            if (user.BranchId.HasValue)
            {
                query = query.Where(s => s.BranchId == user.BranchId);
            }

            Services = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            // استبعاد الفرع من التحقق لأننا سنضيفه برمجياً
            ModelState.Remove("NewService.BranchId");

            if (!ModelState.IsValid)
            {
                // إعادة تحميل القائمة في حال الخطأ
                await OnGetAsync();
                return Page();
            }

            // 🛑 ربط الخدمة الجديدة بفرع الموظف
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                NewService.BranchId = user.BranchId;
            }

            _context.SalonServices.Add(NewService);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.SalonServices.FindAsync(id);

            // 🛑 حماية الحذف: التأكد أن الخدمة تتبع فرع الموظف
            var user = await _userManager.GetUserAsync(User);
            if (service != null)
            {
                if (user.BranchId.HasValue && service.BranchId != user.BranchId)
                {
                    return Forbid(); // منع الحذف إذا كانت لفرع آخر
                }

                _context.SalonServices.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}