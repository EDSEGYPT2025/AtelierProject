using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Salon.Appointments
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SalonAppointment SalonAppointment { get; set; } = default!;

        // لحمل الخدمات المختارة
        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        public List<SalonService> AvailableServices { get; set; } = new List<SalonService>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // جلب الحجز مع التفاصيل
            var salonappointment = await _context.SalonAppointments
                .Include(s => s.Items) // جلب الخدمات المسجلة للحجز
                .FirstOrDefaultAsync(m => m.Id == id);

            if (salonappointment == null) return NotFound();

            SalonAppointment = salonappointment;

            // ملء القوائم
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");

            // جلب الخدمات المتاحة (يفضل فلترتها حسب الفرع إذا أردت، هنا سنجلب الكل)
            AvailableServices = await _context.SalonServices.ToListAsync();

            // تحديد الخدمات التي اختارها العميل سابقاً لتظهر محددة في القائمة
            SelectedServiceIds = SalonAppointment.Items.Select(i => i.SalonServiceId).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. استبعاد الحقول التي تسبب مشاكل التحقق
            ModelState.Remove("SalonAppointment.Branch");
            ModelState.Remove("SalonAppointment.Client");
            ModelState.Remove("SalonAppointment.Items");

            if (!ModelState.IsValid)
            {
                // إعادة تحميل القوائم في حال فشل التحقق
                ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
                AvailableServices = await _context.SalonServices.ToListAsync();
                return Page();
            }

            // 2. جلب الحجز الأصلي من قاعدة البيانات للتعديل عليه
            var appointmentToUpdate = await _context.SalonAppointments
                .Include(a => a.Items) // ضروري لجلب الخدمات القديمة لحذفها
                .FirstOrDefaultAsync(m => m.Id == SalonAppointment.Id);

            if (appointmentToUpdate == null) return NotFound();

            // 3. تحديث البيانات الأساسية
            appointmentToUpdate.ClientId = SalonAppointment.ClientId;
            appointmentToUpdate.AppointmentDate = SalonAppointment.AppointmentDate;
            appointmentToUpdate.Notes = SalonAppointment.Notes;
            // يمكنك تحديث الحالة إذا كانت موجودة في الفورم، أو تركها كما هي
            // appointmentToUpdate.Status = SalonAppointment.Status; 

            // 4. تحديث الخدمات (الأصعب): نحذف القديم ونضيف الجديد
            // أ) حذف الخدمات القديمة
            _context.SalonAppointmentItems.RemoveRange(appointmentToUpdate.Items);

            // ب) إضافة الخدمات الجديدة المختارة
            var selectedServicesInfo = await _context.SalonServices
                .Where(s => SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            foreach (var service in selectedServicesInfo)
            {
                appointmentToUpdate.Items.Add(new SalonAppointmentItem
                {
                    SalonServiceId = service.Id,
                    Price = service.Price
                });
            }

            // 5. إعادة حساب الإجمالي
            appointmentToUpdate.TotalAmount = selectedServicesInfo.Sum(s => s.Price);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalonAppointmentExists(SalonAppointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool SalonAppointmentExists(int id)
        {
            return _context.SalonAppointments.Any(e => e.Id == id);
        }
    }
}