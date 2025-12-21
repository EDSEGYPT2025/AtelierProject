using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Expenses
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Expense Expense { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // 1. تحديد تاريخ اليوم كقيمة افتراضية
            Expense = new Expense { ExpenseDate = DateTime.Now };

            // 2. تحميل قائمة بنود المصروفات (الخاصة بفرع المستخدم فقط)
            var categoriesQuery = _context.ExpenseCategories.AsQueryable();

            if (user.BranchId != null)
            {
                categoriesQuery = categoriesQuery.Where(c => c.BranchId == user.BranchId);
            }

            ViewData["ExpenseCategoryId"] = new SelectList(await categoriesQuery.ToListAsync(), "Id", "Name");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // إزالة الحقول التلقائية من التحقق
            ModelState.Remove("Expense.Branch");
            ModelState.Remove("Expense.BranchId");
            ModelState.Remove("Expense.ExpenseCategory");
            ModelState.Remove("Expense.CreatedByUser");
            ModelState.Remove("Expense.CreatedByUserId");

            if (!ModelState.IsValid)
            {
                // إعادة تحميل القائمة عند الخطأ
                var categoriesQuery = _context.ExpenseCategories.AsQueryable();
                if (user.BranchId != null)
                {
                    categoriesQuery = categoriesQuery.Where(c => c.BranchId == user.BranchId);
                }
                ViewData["ExpenseCategoryId"] = new SelectList(await categoriesQuery.ToListAsync(), "Id", "Name");
                return Page();
            }

            // 🔒 3. البيانات الأمنية التلقائية
            Expense.BranchId = user.BranchId;       // ربط بالفرع
            Expense.CreatedByUserId = user.Id;      // ربط بالموظف (Audit)

            // حفظ المصروف أولاً
            _context.Expenses.Add(Expense);
            await _context.SaveChangesAsync();

            // ============================================================
            // ✅ إضافة جديدة: تسجيل المصروف في خزنة القسم المختص
            // ============================================================
            // إذا لم يحدد المستخدم قسماً (null)، نعتبره مصروفاً من الخزنة الرئيسية (الحريمي مثلاً)
            var targetDepartment = Expense.Department ?? DepartmentType.Women;

            var transaction = new SafeTransaction
            {
                Amount = Expense.Amount,
                Type = TransactionType.Expense, // نوع الحركة: مصروف
                Department = targetDepartment,  // الخزنة التي سيخرج منها المال
                BranchId = Expense.BranchId ?? 1,
                TransactionDate = Expense.ExpenseDate, // نفس تاريخ المصروف
                Description = $"مصروف: {Expense.Description ?? "بدون وصف"}",
                ReferenceId = Expense.Id.ToString(), // ربط برقم المصروف
                CreatedByUserId = user.Id
            };

            _context.SafeTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            // ============================================================

            return RedirectToPage("./Index");
        }
    }
}