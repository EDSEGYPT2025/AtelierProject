using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SalonAppointment Appointment { get; set; } = default!;

        // لعرض الخدمات في الصفحة
        public List<SalonService> AvailableServices { get; set; } = new List<SalonService>();

        // لاستقبال الخدمات المختارة
        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // جلب الحجز مع العناصر المرتبطة به (Items) لتحديد الخدمات المختارة سابقاً
            var appointment = await _context.SalonAppointments
                .Include(a => a.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            Appointment = appointment;

            // تعبئة القوائم المنسدلة
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
            AvailableServices = await _context.SalonServices.ToListAsync();

            // تعبئة قائمة الـ IDs للخدمات المختارة حالياً لتظهر في الـ Checkboxes
            SelectedServiceIds = appointment.Items
                .Select(i => i.SalonServiceId)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // التحقق من اختيار خدمة واحدة على الأقل
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

            // جلب الحجز الأصلي من قاعدة البيانات لتعديله
            // (نستخدم Include للعناصر لأننا سنقوم بتعديلها)
            var appointmentToUpdate = await _context.SalonAppointments
                .Include(a => a.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointmentToUpdate == null)
            {
                return NotFound();
            }

            // 1. تحديث البيانات الأساسية
            appointmentToUpdate.ClientId = Appointment.ClientId;
            appointmentToUpdate.AppointmentDate = Appointment.AppointmentDate;
            appointmentToUpdate.Status = Appointment.Status;
            appointmentToUpdate.Notes = Appointment.Notes;
            appointmentToUpdate.PaidAmount = Appointment.PaidAmount; // القيمة الجديدة للمدفوع

            // 2. تحديث الخدمات (Items)
            // أ) حذف الخدمات التي أزال المستخدم العلامة عنها
            var itemsToRemove = appointmentToUpdate.Items
                .Where(i => !SelectedServiceIds.Contains(i.SalonServiceId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                _context.SalonAppointmentItems.Remove(item);
            }

            // ب) إضافة الخدمات الجديدة التي اختارها المستخدم ولم تكن موجودة
            var currentServiceIds = appointmentToUpdate.Items
                .Select(i => i.SalonServiceId)
                .ToList();

            var newServiceIds = SelectedServiceIds.Except(currentServiceIds).ToList();

            // نحتاج لجلب أسعار الخدمات الجديدة من الداتا بيز
            if (newServiceIds.Any())
            {
                var newServicesInfo = await _context.SalonServices
                    .Where(s => newServiceIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var service in newServicesInfo)
                {
                    appointmentToUpdate.Items.Add(new SalonAppointmentItem
                    {
                        SalonServiceId = service.Id,
                        Price = service.Price,
                        SalonAppointmentId = appointmentToUpdate.Id
                    });
                }
            }

            // 3. إعادة حساب الإجمالي الكلي (TotalAmount) بناءً على القائمة النهائية للخدمات
            // نقوم بجلب أسعار كل الخدمات المختارة حالياً (سواء القديمة أو الجديدة) لضمان دقة الحساب
            var allSelectedServices = await _context.SalonServices
                .Where(s => SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            appointmentToUpdate.TotalAmount = allSelectedServices.Sum(s => s.Price);

            // ملاحظة: لا نعدل RemainingAmount يدوياً، سيتم حسابه تلقائياً عند العرض بناءً على التعديلات أعلاه

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalonAppointmentExists(Appointment.Id))
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