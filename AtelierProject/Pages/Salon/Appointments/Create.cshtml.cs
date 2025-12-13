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

        public async Task<IActionResult> OnGetAsync()
        {
            // 👇 1. تعديل استعلام العملاء ليشمل الاسم ورقم الهاتف
            // سيظهر في القائمة: "منى أحمد - 01012345678"
            var clients = await _context.Clients
                .Select(c => new {
                    c.Id,
                    DisplayText = c.Name + " - " + (c.Phone ?? "")
                })
                .ToListAsync();

            ViewData["ClientId"] = new SelectList(clients, "Id", "DisplayText");
            // -------------------------------------------------------------

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
                // 👇 2. نفس التعديل هنا عند إعادة التحميل (في حالة حدوث خطأ)
                var clients = await _context.Clients
                    .Select(c => new {
                        c.Id,
                        DisplayText = c.Name + " - " + (c.Phone ?? "")
                    })
                    .ToListAsync();

                ViewData["ClientId"] = new SelectList(clients, "Id", "DisplayText");
                // -------------------------------------------------------------

                AvailableServices = await _context.SalonServices.ToListAsync();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Appointment.BranchId = currentUser.BranchId;
            }

            var selectedServicesInfo = await _context.SalonServices
                .Where(s => SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            decimal totalAmount = selectedServicesInfo.Sum(s => s.Price);

            Appointment.Status = SalonAppointmentStatus.Confirmed;
            Appointment.TotalAmount = totalAmount;

            _context.SalonAppointments.Add(Appointment);

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