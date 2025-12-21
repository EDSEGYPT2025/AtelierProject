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
            // ✅ تم إضافة .ThenInclude(pi => pi.ProductDefinition) للوصول للقسم
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (!await IsUserAllowed(booking.BranchId)) return Forbid();

            booking.Status = newStatus;

            // عند تحويل الحالة إلى "تم الاستلام"
            if (newStatus == BookingStatus.PickedUp)
            {
                // 1. تحديث حالة المخزون
                foreach (var item in booking.BookingItems)
                {
                    if (item.ProductItem != null) item.ProductItem.Status = ItemStatus.Rented;
                }

                // 2. تسجيل الحركات المالية في الخزنة
                var currentUser = await _userManager.GetUserAsync(User);
                // تحديد القسم بناءً على أول قطعة (افتراضي حريمي لو لم يوجد)
                var dept = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition?.Department ?? DepartmentType.Women;

                // أ) تحصيل باقي مبلغ الإيجار (إن وجد)
                decimal remaining = booking.TotalAmount - booking.PaidAmount;
                if (remaining > 0)
                {
                    // نعتبر أنه دفع الباقي عند الاستلام
                    booking.PaidAmount += remaining;

                    _context.SafeTransactions.Add(new SafeTransaction
                    {
                        Amount = remaining,
                        Type = TransactionType.Income, // إيراد
                        Department = dept,
                        BranchId = booking.BranchId ?? 1,
                        TransactionDate = DateTime.Now,
                        Description = $"سداد باقي حجز رقم {booking.Id} عند الاستلام",
                        ReferenceId = booking.Id.ToString(),
                        CreatedByUserId = currentUser?.Id
                    });
                }

                // ب) استلام مبلغ التأمين (أمانات)
                if (booking.InsuranceAmount > 0)
                {
                    _context.SafeTransactions.Add(new SafeTransaction
                    {
                        Amount = booking.InsuranceAmount,
                        Type = TransactionType.InsuranceIn, // دخول تأمين للخزنة
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
            // ✅ تغيير البحث ليجلب تفاصيل القطعة لتحديد القسم
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

                // تسجيل الحركة في الخزنة
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
            // ✅ إضافة الـ Includes
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
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

            // تسجيل رد التأمين (خروج مال من الخزنة)
            // المبلغ المسترد = التأمين الأصلي - الخصم
            decimal refund = booking.InsuranceAmount - deductionAmount;

            if (refund > 0) // لو فيه فلوس هترجع للعميل
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var dept = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition?.Department ?? DepartmentType.Women;

                _context.SafeTransactions.Add(new SafeTransaction
                {
                    Amount = refund,
                    Type = TransactionType.InsuranceOut, // خروج تأمين
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