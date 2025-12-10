using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "الاسم الكامل مطلوب")]
            [Display(Name = "اسم الموظف")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
            [EmailAddress]
            [Display(Name = "البريد الإلكتروني")]
            public string Email { get; set; }

            [Required(ErrorMessage = "كلمة المرور مطلوبة")]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور")]
            public string Password { get; set; }

            [Display(Name = "الفرع التابع له")]
            public int? BranchId { get; set; }

            // الصلاحيات
            [Display(Name = "صلاحية قسم الرجال")]
            public bool CanAccessMen { get; set; }

            [Display(Name = "صلاحية قسم النساء")]
            public bool CanAccessWomen { get; set; }

            [Display(Name = "صلاحية قسم الكوافير")]
            public bool CanAccessBeauty { get; set; }
        }

        public void OnGet()
        {
            // تحميل قائمة الفروع
            ViewData["BranchId"] = new SelectList(_context.Branches, "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email, // اسم المستخدم هو الايميل
                    Email = Input.Email,
                    FullName = Input.FullName,
                    BranchId = Input.BranchId,
                    CanAccessMenSection = Input.CanAccessMen,
                    CanAccessWomenSection = Input.CanAccessWomen,
                    CanAccessBeautySection = Input.CanAccessBeauty,
                    EmailConfirmed = true, // تفعيل الحساب مباشرة
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // نجاح العملية
                    return RedirectToPage("./Index"); // سننشئ هذه الصفحة لاحقاً
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // في حالة الفشل، أعد تحميل القائمة
            ViewData["BranchId"] = new SelectList(_context.Branches, "Id", "Name");
            return Page();
        }
    }
}