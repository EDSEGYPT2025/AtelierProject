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

        // ✅ تعديل: حساب المتبقي ليشمل الخصم
        public decimal RemainingRental
        {
            get
            {
                if (Booking == null) return 0;
                var netTotal = Booking.TotalAmount - Booking.Discount;
                var remaining = netTotal - Booking.PaidAmount;
                return remaining < 0 ? 0 : remaining;
            }
        }

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

            Booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Branch)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null) return NotFound();

            if (!await IsUserAllowed(Booking.BranchId)) return Forbid();

            // إصلاح مشكلة ظهور الإجمالي صفر (للبيانات القديمة)
            if (Booking.TotalAmount == 0 && Booking.BookingItems.Any())
            {
                Booking.TotalAmount = Booking.BookingItems.Sum(item =>
                    item.RentalPrice > 0
                    ? item.RentalPrice
                    : (item.ProductItem?.ProductDefinition?.RentalPrice ?? 0)
                );
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, BookingStatus newStatus)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = newStatus;

            // عند التسليم (PickedUp)
            if (newStatus == BookingStatus.PickedUp)
            {
                // 1. تحديث المخزون
                foreach (var item in booking.BookingItems)
                {
                    if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Rented;
                }

                // 2. تسجيل المالية
                var currentUser = await _userManager.GetUserAsync(User);
                var dept = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition?.Department ?? DepartmentType.Women;

                // إصلاح التوتال لو كان صفر
                if (booking.TotalAmount == 0)
                {
                    decimal calculatedTotal = booking.BookingItems.Sum(item => item.RentalPrice > 0 ? item.RentalPrice : (item.ProductItem?.ProductDefinition?.RentalPrice ?? 0));
                    booking.TotalAmount = calculatedTotal;
                }

                // ✅ تعديل: حساب المتبقي بعد الخصم
                decimal netTotal = booking.TotalAmount - booking.Discount;
                decimal remaining = netTotal - booking.PaidAmount;

                if (remaining > 0)
                {
                    booking.PaidAmount += remaining;
                    _context.SafeTransactions.Add(new SafeTransaction
                    {
                        Amount = remaining,
                        Type = TransactionType.Income,
                        Department = dept,
                        BranchId = booking.BranchId ?? 1,
                        TransactionDate = DateTime.Now,
                        Description = $"سداد باقي حجز رقم {booking.Id} عند الاستلام",
                        ReferenceId = booking.Id.ToString(),
                        CreatedByUserId = currentUser?.Id
                    });
                }

                // تأمين
                if (booking.InsuranceAmount > 0)
                {
                    _context.SafeTransactions.Add(new SafeTransaction
                    {
                        Amount = booking.InsuranceAmount,
                        Type = TransactionType.InsuranceIn,
                        Department = dept,
                        BranchId = booking.BranchId ?? 1,
                        TransactionDate = DateTime.Now,
                        Description = $"استلام تأمين حجز رقم {booking.Id}",
                        ReferenceId = booking.Id.ToString(),
                        CreatedByUserId = currentUser?.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostAddPaymentAsync(int id, decimal paymentAmount)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            if (paymentAmount > 0)
            {
                booking.PaidAmount += paymentAmount;

                var currentUser = await _userManager.GetUserAsync(User);
                var dept = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition?.Department ?? DepartmentType.Women;

                _context.SafeTransactions.Add(new SafeTransaction
                {
                    Amount = paymentAmount,
                    Type = TransactionType.Income,
                    Department = dept,
                    BranchId = booking.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"دفعة إضافية لحجز رقم {booking.Id}",
                    ReferenceId = booking.Id.ToString(),
                    CreatedByUserId = currentUser?.Id
                });

                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostReturnAsync(int id, decimal deductionAmount)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = BookingStatus.Returned;
            booking.InsuranceDeduction = deductionAmount;

            foreach (var item in booking.BookingItems)
            {
                if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Available;
            }

            decimal refund = booking.InsuranceAmount - deductionAmount;

            if (refund > 0)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var dept = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition?.Department ?? DepartmentType.Women;

                _context.SafeTransactions.Add(new SafeTransaction
                {
                    Amount = refund,
                    Type = TransactionType.InsuranceOut,
                    Department = dept,
                    BranchId = booking.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"رد تأمين حجز رقم {booking.Id} (بعد خصم {deductionAmount})",
                    ReferenceId = booking.Id.ToString(),
                    CreatedByUserId = currentUser?.Id
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id });
        }
    }
}