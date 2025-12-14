using AtelierProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessage = "كلمة المرور مطلوبة")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "تذكرني؟")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");

            // مسح الكوكيز لضمان تسجيل دخول نظيف
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // ✅ التعديل هنا: lockoutOnFailure: true لتفعيل التحقق من القفل
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }

                // ✅ التعديل هنا: التعامل مع الحساب المقفول (غير النشط)
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    // بدلاً من التوجيه لصفحة أخرى، نعرض الرسالة هنا
                    ModelState.AddModelError(string.Empty, "هذا الحساب غير نشط (مغلق)، يرجى مراجعة الإدارة.");
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
                    return Page();
                }
            }

            // إذا وصلنا هنا فهذا يعني أن البيانات غير مكتملة
            return Page();
        }
    }
}