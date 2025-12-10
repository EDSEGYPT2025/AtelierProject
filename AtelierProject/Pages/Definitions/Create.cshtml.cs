using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // 1. إضافة مكتبة الهوية
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AtelierProject.Pages.Definitions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 2. تعريف المانجر

        // 3. حقن المانجر في الكونستركتور
        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public ProductDefinition ProductDefinition { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // -----------------------------------------------------------
            // 4. التحقق من الصلاحيات (Logic Check)
            // -----------------------------------------------------------
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // منع موظف الرجالي من إضافة حريمي، والعكس
            if (ProductDefinition.Department == DepartmentType.Men && !user.CanAccessMenSection)
            {
                ModelState.AddModelError("ProductDefinition.Department", "عذراً، ليس لديك صلاحية لإضافة موديلات في قسم الرجال.");
                return Page();
            }

            if (ProductDefinition.Department == DepartmentType.Women && !user.CanAccessWomenSection)
            {
                ModelState.AddModelError("ProductDefinition.Department", "عذراً، ليس لديك صلاحية لإضافة موديلات في قسم النساء.");
                return Page();
            }

            // (اختياري) إذا كان هناك منتجات للكوافير
            if (ProductDefinition.Department == DepartmentType.BeautySalon && !user.CanAccessBeautySection)
            {
                ModelState.AddModelError("ProductDefinition.Department", "عذراً، ليس لديك صلاحية لقسم الكوافير.");
                return Page();
            }
            // -----------------------------------------------------------


            // 5. توليد الكود التلقائي (الكود الأصلي الخاص بك)
            if (string.IsNullOrWhiteSpace(ProductDefinition.Code))
            {
                string prefix = ProductDefinition.Department switch
                {
                    Models.DepartmentType.Men => "MEN",
                    Models.DepartmentType.Women => "WOM",
                    Models.DepartmentType.BeautySalon => "SAL",
                    _ => "GEN"
                };

                int currentCount = await _context.ProductDefinitions
                                    .CountAsync(p => p.Department == ProductDefinition.Department);

                int nextNumber = currentCount + 1;

                ProductDefinition.Code = $"{prefix}-{nextNumber:D4}";
            }
            else
            {
                bool codeExists = await _context.ProductDefinitions
                                        .AnyAsync(p => p.Code == ProductDefinition.Code);
                if (codeExists)
                {
                    ModelState.AddModelError("ProductDefinition.Code", "هذا الكود مستخدم بالفعل، يرجى تغييره أو تركه فارغاً.");
                    return Page();
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.ProductDefinitions.Add(ProductDefinition);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}