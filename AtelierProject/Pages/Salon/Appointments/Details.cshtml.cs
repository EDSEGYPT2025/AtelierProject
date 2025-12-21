using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public SalonAppointment Appointment { get; set; } = default!;

        // 🛡️ الدالة الأمنية (Centralized Security Check)
        private async Task<bool> IsUserAllowed(int? branchId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;

            // المدير العام (بدون فرع) يرى كل شيء
            if (user.BranchId == null) return true;

            // الموظف يجب أن يطابق فرع الحجز
            return user.BranchId == branchId;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Appointment = await _context.SalonAppointments
                .Include(a => a.Client)
                .Include(a => a.Items)
                .ThenInclude(s => s.SalonService)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Appointment == null) return NotFound();

            // 🛡️ التحقق الأمني قبل العرض
            if (!await IsUserAllowed(Appointment.BranchId)) return Forbid();

            return Page();
        }

        // 1. تسجيل دفعة نقدية
        public async Task<IActionResult> OnPostPayRemainingAsync(int id, decimal amount)
        {
            var appt = await _context.SalonAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            // 🛡️ التحقق الأمني قبل الدفع
            if (!await IsUserAllowed(appt.BranchId)) return Forbid();

            if (amount > 0)
            {
                // تحديث المدفوع في الحجز
                appt.PaidAmount += amount;

                // ============================================================
                // ✅ الإضافة الجديدة: تسجيل الدفعة في خزنة الكوافير
                // ============================================================
                var currentUser = await _userManager.GetUserAsync(User);

                var transaction = new SafeTransaction
                {
                    Amount = amount,
                    Type = TransactionType.Income,           // نوع الحركة: إيراد
                    Department = DepartmentType.BeautySalon, // القسم: كوافير
                    BranchId = appt.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"دفعة لحجز صالون رقم {appt.Id}",
                    ReferenceId = appt.Id.ToString(),
                    CreatedByUserId = currentUser?.Id
                };

                _context.SafeTransactions.Add(transaction);
                // ============================================================

                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        // 2. إلغاء الموعد
        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var appt = await _context.SalonAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            // 🛡️ التحقق الأمني قبل الإلغاء
            if (!await IsUserAllowed(appt.BranchId)) return Forbid();

            appt.Status = SalonAppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }

        // 3. تحديث الحالة (مثلاً: تم الانتهاء Completed)
        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, SalonAppointmentStatus newStatus)
        {
            var appt = await _context.SalonAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            // 🛡️ التحقق الأمني
            if (!await IsUserAllowed(appt.BranchId)) return Forbid();

            appt.Status = newStatus;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}