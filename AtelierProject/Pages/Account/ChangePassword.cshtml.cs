using System.ComponentModel.DataAnnotations;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AtelierProject.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور الحالية")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
            [StringLength(100, ErrorMessage = "{0} يجب أن تكون على الأقل {2} حروف.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور الجديدة")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "تأكيد كلمة المرور الجديدة")]
            [Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    // ترجمة رسالة الخطأ الشائعة
                    if (error.Code == "PasswordMismatch")
                        ModelState.AddModelError(string.Empty, "كلمة المرور الحالية غير صحيحة.");
                    else
                        ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // إعادة تسجيل الدخول لتحديث الكوكيز (حتى لا يخرج النظام المستخدم)
            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "تم تغيير كلمة المرور بنجاح.";
            return RedirectToPage();
        }
    }
}