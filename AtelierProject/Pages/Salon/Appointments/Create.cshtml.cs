using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization; // 1. هام: أضفنا مكتبة التصريح
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    [Authorize] // 2. هام: منع الدخول لغير الموظفين
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
        public SalonAppointment Appointment { get; set; } = default!;

        public List<SalonService> AvailableServices { get; set; } = new List<SalonService>();

        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync()
        {
            // تحميل قائمة العملاء والخدمات
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
            AvailableServices = await _context.SalonServices.ToListAsync();

            var now = DateTime.Now;
            Appointment = new SalonAppointment
            {
                AppointmentDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // التحقق من اختيار خدمة واحدة على الأقل
            if (SelectedServiceIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار خدمة واحدة على الأقل.");
            }

            // =================================================================================
            // 🔥 الحل الجذري لمشكلة التحقق 🔥
            // نقوم بإزالة الحقول التي سنملؤها برمجياً من عملية التحقق
            // =================================================================================
            ModelState.Remove("Appointment.Branch");       // لا يأتي من الصفحة
            ModelState.Remove("Appointment.BranchId");     // نحدده من الموظف
            ModelState.Remove("Appointment.Client");       // نرسل ClientId فقط
            ModelState.Remove("Appointment.Items");        // نملؤها لاحقاً
            ModelState.Remove("Appointment.TotalAmount");  // نحسبها لاحقاً
            // =================================================================================

            if (!ModelState.IsValid)
            {
                // إعادة تحميل القوائم في حالة وجود خطأ
                ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
                AvailableServices = await _context.SalonServices.ToListAsync();
                return Page();
            }

            // 3. جلب المستخدم الحالي وتعيين الفرع
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Appointment.BranchId = currentUser.BranchId;
            }

            // 4. حساب الخدمات والأسعار
            var selectedServicesInfo = await _context.SalonServices
                .Where(s => SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            decimal totalAmount = selectedServicesInfo.Sum(s => s.Price);

            Appointment.Status = SalonAppointmentStatus.Confirmed;
            Appointment.TotalAmount = totalAmount; // يمكن السماح بتعديله يدوياً لاحقاً إذا أردت

            _context.SalonAppointments.Add(Appointment);

            // 5. إضافة التفاصيل
            foreach (var service in selectedServicesInfo)
            {
                var item = new SalonAppointmentItem
                {
                    SalonAppointment = Appointment,
                    SalonServiceId = service.Id,
                    Price = service.Price
                };
                _context.SalonAppointmentItems.Add(item);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}