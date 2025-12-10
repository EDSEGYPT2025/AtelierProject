using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EditModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Id { get; set; } // معرف المستخدم (مخفي)

            [Required(ErrorMessage = "الاسم الكامل مطلوب")]
            [Display(Name = "اسم الموظف")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "البريد الإلكتروني")]
            public string Email { get; set; }

            [Display(Name = "الفرع التابع له")]
            public int? BranchId { get; set; }

            [Display(Name = "حالة الحساب")]
            public bool IsActive { get; set; }

            // الصلاحيات
            [Display(Name = "صلاحية قسم الرجال")]
            public bool CanAccessMen { get; set; }

            [Display(Name = "صلاحية قسم النساء")]
            public bool CanAccessWomen { get; set; }

            [Display(Name = "صلاحية قسم الكوافير")]
            public bool CanAccessBeauty { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // ملء النموذج بالبيانات الحالية
            Input = new InputModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                BranchId = user.BranchId,
                IsActive = user.IsActive,
                CanAccessMen = user.CanAccessMenSection,
                CanAccessWomen = user.CanAccessWomenSection,
                CanAccessBeauty = user.CanAccessBeautySection
            };

            ViewData["BranchId"] = new SelectList(_context.Branches, "Id", "Name", user.BranchId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["BranchId"] = new SelectList(_context.Branches, "Id", "Name", Input.BranchId);
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null) return NotFound();

            // تحديث البيانات
            user.FullName = Input.FullName;
            user.Email = Input.Email;
            user.UserName = Input.Email; // تحديث اسم المستخدم ليطابق الإيميل
            user.BranchId = Input.BranchId;
            user.IsActive = Input.IsActive;

            // تحديث الصلاحيات
            user.CanAccessMenSection = Input.CanAccessMen;
            user.CanAccessWomenSection = Input.CanAccessWomen;
            user.CanAccessBeautySection = Input.CanAccessBeauty;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["BranchId"] = new SelectList(_context.Branches, "Id", "Name", Input.BranchId);
            return Page();
        }
    }
}