using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // إضافة هامة
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 1. تعريف مدير المستخدمين

        // 2. حقن الخدمة في البناء
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
            if (SelectedServiceIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار خدمة واحدة على الأقل.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
                AvailableServices = await _context.SalonServices.ToListAsync();
                return Page();
            }

            // ---------------------------------------------------------
            // 3. جلب المستخدم الحالي وتعيين الفرع تلقائياً
            // ---------------------------------------------------------
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // حفظ الفرع الذي يتبع له الموظف في الحجز
                Appointment.BranchId = currentUser.BranchId;
            }
            // ---------------------------------------------------------

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