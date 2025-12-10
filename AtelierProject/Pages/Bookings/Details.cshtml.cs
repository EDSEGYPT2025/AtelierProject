using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // 1. إضافة مكتبة الهوية
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Bookings
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 2. تعريف المانجر

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Booking Booking { get; set; } = default!;
        public decimal RemainingRental => Booking.TotalAmount - Booking.PaidAmount;

        // دالة مساعدة للتحقق من الصلاحية (لتجنب تكرار الكود)
        private async Task<bool> IsUserAllowed(Booking booking)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;

            // إذا كان أدمن (ليس له فرع)، مسموح له بكل شيء
            if (user.BranchId == null) return true;

            // إذا كان موظف، يجب أن يكون فرعه مطابق لفرع الحجز
            return booking.BranchId == user.BranchId;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null) return NotFound();

            // 3. التحقق الأمني: هل الموظف مسموح له برؤية هذا الحجز؟
            if (!await IsUserAllowed(Booking))
            {
                return Forbid(); // عرض صفحة "غير مسموح" (403)
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, BookingStatus newStatus)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // 4. التحقق الأمني قبل التعديل (هام جداً)
            if (!await IsUserAllowed(booking)) return Forbid();

            booking.Status = newStatus;

            // تحديث المخزون
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

        public async Task<IActionResult> OnPostAddPaymentAsync(int id, decimal paymentAmount)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            // 5. التحقق الأمني قبل الدفع
            if (!await IsUserAllowed(booking)) return Forbid();

            if (paymentAmount > 0)
            {
                booking.PaidAmount += paymentAmount;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostReturnAsync(int id, decimal deductionAmount)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // 6. التحقق الأمني قبل الإرجاع
            if (!await IsUserAllowed(booking)) return Forbid();

            booking.Status = BookingStatus.Returned;
            booking.InsuranceDeduction = deductionAmount;

            // إعادة المخزون
            foreach (var item in booking.BookingItems)
            {
                if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Available;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}