using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Expenses.Categories
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
        public ExpenseCategory ExpenseCategory { get; set; } = default!;

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // إزالة BranchId من التحقق لأننا سنملؤه يدوياً
            ModelState.Remove("ExpenseCategory.Branch");
            ModelState.Remove("ExpenseCategory.BranchId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // 🔒 تعيين الفرع تلقائياً للمستخدم الحالي
            ExpenseCategory.BranchId = user.BranchId;

            // حماية إضافية: إذا كان أدمن (بدون فرع)، يمكنه تركه فارغاً ليكون "عام" أو اختيار فرع
            // في هذا الكود البسيط، ما يضيفه الأدمن سيصبح "عاماً" لكل الفروع إذا لم نغير المنطق

            _context.ExpenseCategories.Add(ExpenseCategory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}