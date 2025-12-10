using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public SalonAppointment Appointment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Appointment = await _context.SalonAppointments
                .Include(a => a.Client)
                .Include(a => a.Items)
                .ThenInclude(s => s.SalonService)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Appointment == null) return NotFound();

            return Page();
        }

        // دالة سداد المبلغ المتبقي
        public async Task<IActionResult> OnPostPayRemainingAsync(int id, decimal amount)
        {
            var appt = await _context.SalonAppointments.FindAsync(id);
            if (appt == null || amount <= 0) return RedirectToPage(new { id = id });

            // حماية: عدم دفع أكثر من المتبقي
            if (amount > appt.RemainingAmount)
            {
                amount = appt.RemainingAmount;
            }

            appt.PaidAmount += amount;

            // تحديث الحالة تلقائياً إذا تم سداد كامل المبلغ
            // افتراضاً أن لديك حالة باسم Completed
            if (appt.RemainingAmount <= 0)
            {
                appt.Status = SalonAppointmentStatus.Completed;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }

        // دالة إلغاء الموعد
        public async Task<IActionResult> OnPostCancelAsync(int id, decimal refundAmount)
        {
            var appt = await _context.SalonAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            // 1. تغيير الحالة
            appt.Status = SalonAppointmentStatus.Cancelled;

            // 2. معالجة استرداد العربون
            if (refundAmount > 0)
            {
                // حماية: لا يمكن رد أكثر مما دفع
                if (refundAmount > appt.PaidAmount)
                {
                    appt.PaidAmount = 0; // رد كامل المبلغ
                }
                else
                {
                    appt.PaidAmount -= refundAmount; // خصم المبلغ المسترد من المدفوعات المسجلة
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}