using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Expenses.Categories
{
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

        public IList<ExpenseCategory> ExpenseCategories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 🔒 الاستعلام الآمن: جلب الفئات الخاصة بفرع المستخدم فقط
            var query = _context.ExpenseCategories.AsQueryable();

            if (user.BranchId != null)
            {
                query = query.Where(c => c.BranchId == user.BranchId);
            }
            // (اختياري) يمكن للأدمن العام رؤية كل شيء أو إضافة فلتر للفرع

            ExpenseCategories = await query.OrderBy(c => c.Name).ToListAsync();
        }

        // دالة الحذف السريع من نفس الصفحة
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var category = await _context.ExpenseCategories.FindAsync(id);

            if (category != null)
            {
                // 🔒 حماية: منع حذف بند يخص فرعاً آخر
                if (user.BranchId != null && category.BranchId != user.BranchId)
                {
                    return Forbid();
                }

                _context.ExpenseCategories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}