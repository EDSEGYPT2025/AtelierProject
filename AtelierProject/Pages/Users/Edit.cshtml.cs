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
            public string Id { get; set; }

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

            // تحديد حالة الحساب
            bool isActiveAccount = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now;

            Input = new InputModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                BranchId = user.BranchId,
                IsActive = isActiveAccount,
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

            // 1. تحديث البيانات الأساسية
            user.FullName = Input.FullName;
            user.Email = Input.Email;
            user.UserName = Input.Email;
            user.BranchId = Input.BranchId;

            // 2. تحديث الصلاحيات
            user.CanAccessMenSection = Input.CanAccessMen;
            user.CanAccessWomenSection = Input.CanAccessWomen;
            user.CanAccessBeautySection = Input.CanAccessBeauty;

            // 3. ✅ تحديث حالة القفل + الإيقاف اللحظي (مدمج)
            if (Input.IsActive)
            {
                // تفعيل: إزالة تاريخ القفل
                user.LockoutEnd = null;
            }
            else
            {
                // إيقاف:
                user.LockoutEnabled = true; // تفعيل ميزة القفل لهذا المستخدم
                user.LockoutEnd = DateTimeOffset.MaxValue; // قفل للأبد

                // 🔥 السر هنا: تغيير البصمة يدوياً ليتم حفظها مع UpdateAsync
                user.SecurityStamp = Guid.NewGuid().ToString();
            }

            // 4. حفظ كل التغييرات (شاملة البصمة) في خطوة واحدة
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