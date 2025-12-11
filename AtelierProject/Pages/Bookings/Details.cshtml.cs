using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // مكتبة الهوية
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Bookings
{
    [Authorize] // إجبار تسجيل الدخول
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // لإدارة المستخدمين

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Booking Booking { get; set; } = default!;
        public decimal RemainingRental => Booking.TotalAmount - Booking.PaidAmount;

        // 🛡️ دالة أمنية للتحقق من الصلاحية (Helper Method)
        private async Task<bool> IsUserAllowed(int? bookingBranchId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;

            // 1. الأدمن (الذي ليس له فرع) يرى كل شيء
            if (user.BranchId == null) return true;

            // 2. الموظف يجب أن يكون في نفس فرع الحجز
            // إذا كان الحجز ليس له فرع (بيانات قديمة) أو يختلف عن فرع الموظف -> مرفوض
            if (bookingBranchId == null || bookingBranchId != user.BranchId)
            {
                return false;
            }

            return true;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Branch)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null) return NotFound();

            // 🛡️ تفتيش أمني قبل عرض الصفحة
            if (!await IsUserAllowed(Booking.BranchId))
            {
                return Forbid(); // صفحة "غير مسموح لك"
            }

            return Page();
        }

        // دالة تغيير الحالة (تسليم)
        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, BookingStatus newStatus)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // 🛡️ تفتيش أمني قبل التعديل
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = newStatus;

            // تحديث المخزون عند التسليم
            if (newStatus == BookingStatus.PickedUp)
            {
                foreach (var item in booking.BookingItems)
                {
                    if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Rented;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }

        // دالة الدفع
        public async Task<IActionResult> OnPostAddPaymentAsync(int id, decimal paymentAmount)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            // 🛡️ تفتيش أمني قبل استلام الفلوس
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            if (paymentAmount > 0)
            {
                booking.PaidAmount += paymentAmount;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        // دالة الإرجاع
        public async Task<IActionResult> OnPostReturnAsync(int id, decimal deductionAmount)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // 🛡️ تفتيش أمني
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = BookingStatus.Returned;
            booking.InsuranceDeduction = deductionAmount;

            // إعادة المخزون متاح
            foreach (var item in booking.BookingItems)
            {
                if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Available;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}