using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Expenses
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

        public IList<Expense> Expenses { get; set; } = default!;

        // متغيرات الفلترة
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        public decimal TotalAmount { get; set; } // لعرض المجموع

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // تحديد قيم افتراضية للتاريخ (أول الشهر لآخره) إذا لم يحدد المستخدم
            if (!FromDate.HasValue) FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!ToDate.HasValue) ToDate = DateTime.Now.Date;

            // بناء الاستعلام
            var query = _context.Expenses
                .Include(e => e.ExpenseCategory)
                .Include(e => e.CreatedByUser)
                .AsQueryable();

            // 1. فلتر الفرع (إجباري)
            if (user.BranchId != null)
            {
                query = query.Where(e => e.BranchId == user.BranchId);

                // ب) ✅ التعديل الجديد: يرى مصروفاته هو فقط
                query = query.Where(e => e.CreatedByUserId == user.Id);
            }
            else
            {

            }

            // 2. فلتر التاريخ
            // نضيف يوم لنهاية الفترة ليشمل اليوم الأخير كاملاً
            var endDate = ToDate.Value.AddDays(1);
            query = query.Where(e => e.ExpenseDate >= FromDate && e.ExpenseDate < endDate);

            // 3. فلتر النوع
            if (CategoryId.HasValue)
            {
                query = query.Where(e => e.ExpenseCategoryId == CategoryId);
            }

            // تنفيذ الاستعلام
            Expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();

            // حساب الإجمالي
            TotalAmount = Expenses.Sum(e => e.Amount);

            // تحميل قائمة الفئات للفلتر
            var catsQuery = _context.ExpenseCategories.AsQueryable();
            if (user.BranchId != null) catsQuery = catsQuery.Where(c => c.BranchId == user.BranchId);

            ViewData["CategoryId"] = new SelectList(await catsQuery.ToListAsync(), "Id", "Name");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var expense = await _context.Expenses.FindAsync(id);

            if (expense != null)
            {
                // حماية الحذف: الفرع فقط
                if (user.BranchId != null && expense.BranchId != user.BranchId) return Forbid();

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}