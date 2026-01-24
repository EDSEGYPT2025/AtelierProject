using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
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
        public SalonAppointment Appointment { get; set; } = default!;

        public List<SalonService> AvailableServices { get; set; } = new List<SalonService>();

        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        // ✅ 1. خاصية جديدة لاستقبال الكميات (مفتاح: رقم الخدمة، قيمة: العدد)
        [BindProperty]
        public Dictionary<int, int> ServiceQuantities { get; set; } = new Dictionary<int, int>();

        public async Task<IActionResult> OnGetAsync()
        {
            var clients = await _context.Clients
                .Select(c => new {
                    c.Id,
                    DisplayText = c.Name + " - " + (c.Phone ?? "")
                })
                .ToListAsync();

            ViewData["ClientId"] = new SelectList(clients, "Id", "DisplayText");

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
            if (SelectedServiceIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار خدمة واحدة على الأقل.");
            }

            // استبعاد الحقول المحسوبة من التحقق
            ModelState.Remove("Appointment.Branch");
            ModelState.Remove("Appointment.BranchId");
            ModelState.Remove("Appointment.Client");
            ModelState.Remove("Appointment.Items");
            ModelState.Remove("Appointment.TotalAmount");

            if (!ModelState.IsValid)
            {
                var clients = await _context.Clients
                    .Select(c => new {
                        c.Id,
                        DisplayText = c.Name + " - " + (c.Phone ?? "")
                    })
                    .ToListAsync();

                ViewData["ClientId"] = new SelectList(clients, "Id", "DisplayText");
                AvailableServices = await _context.SalonServices.ToListAsync();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Appointment.BranchId = currentUser.BranchId;
            }

            // جلب بيانات الخدمات المختارة من قاعدة البيانات
            var selectedServicesInfo = await _context.SalonServices
                .Where(s => SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            decimal totalAmount = 0;

            Appointment.Status = SalonAppointmentStatus.Confirmed;
            // سيتم حساب TotalAmount لاحقاً بعد معرفة الكميات

            _context.SalonAppointments.Add(Appointment);

            foreach (var service in selectedServicesInfo)
            {
                // ✅ 2. تحديد الكمية (الافتراضي 1 إذا لم تُرسل)
                int qty = 1;
                if (ServiceQuantities != null && ServiceQuantities.ContainsKey(service.Id))
                {
                    qty = ServiceQuantities[service.Id];
                    if (qty < 1) qty = 1; // حماية من القيم السالبة أو الصفر
                }

                var item = new SalonAppointmentItem
                {
                    SalonAppointment = Appointment,
                    SalonServiceId = service.Id,
                    Price = service.Price,
                    Quantity = qty // ✅ 3. حفظ الكمية
                };

                _context.SalonAppointmentItems.Add(item);

                // ✅ 4. جمع الإجمالي (السعر × الكمية)
                totalAmount += (service.Price * qty);
            }

            Appointment.TotalAmount = totalAmount; // تعيين الإجمالي النهائي

            // حفظ الحجز أولاً للحصول على الـ ID
            await _context.SaveChangesAsync();

            // ============================================================
            // ✅ تسجيل العربون في خزنة الكوافير
            // ============================================================
            if (Appointment.PaidAmount > 0)
            {
                var transaction = new SafeTransaction
                {
                    Amount = Appointment.PaidAmount,
                    Type = TransactionType.Income,            // نوع الحركة: إيراد
                    Department = DepartmentType.BeautySalon, // القسم: كوافير
                    BranchId = Appointment.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"عربون حجز صالون رقم {Appointment.Id}",
                    ReferenceId = Appointment.Id.ToString(),
                    CreatedByUserId = currentUser?.Id
                };

                _context.SafeTransactions.Add(transaction);
                await _context.SaveChangesAsync();
            }
            // ============================================================

            return RedirectToPage("./Index");
        }
    }
}