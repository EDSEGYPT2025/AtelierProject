using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Bookings
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

        public Booking Booking { get; set; } = default!;
        // حساب المتبقي (الإجمالي - المدفوع)
        public decimal RemainingRental => Booking.TotalAmount - Booking.PaidAmount;

        // دالة التحقق من الصلاحية
        private async Task<bool> IsUserAllowed(int? bookingBranchId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            if (user.BranchId == null) return true; // المدير العام
            if (bookingBranchId == null || bookingBranchId != user.BranchId) return false;
            return true;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // ✅ الاستعلام المحدث: جلب العناصر المتعددة (BookingItems) وتفاصيل المنتجات
            Booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Branch)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null) return NotFound();

            if (!await IsUserAllowed(Booking.BranchId)) return Forbid();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, BookingStatus newStatus)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = newStatus;

            // تحديث حالة كل القطع المرتبطة بالحجز
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
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

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
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = BookingStatus.Returned;
            booking.InsuranceDeduction = deductionAmount;

            // إعادة جميع القطع للحالة "متاح"
            foreach (var item in booking.BookingItems)
            {
                if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Available;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}