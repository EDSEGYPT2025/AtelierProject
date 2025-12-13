using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Reports
{
    [Authorize]
    public class DailyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DailyModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime ReportDate { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public int? SelectedBranchId { get; set; }

        // --- المتغيرات المالية ---
        public decimal TotalCashIn_Men { get; set; }
        public decimal TotalCashIn_Women { get; set; }
        public decimal TotalCashIn_Beauty { get; set; }

        public decimal TotalRefunds_Men { get; set; }
        public decimal TotalRefunds_Women { get; set; }

        // ✅ متغير جديد للمصروفات
        public decimal TotalExpenses { get; set; }

        // ✅ الصافي النهائي لكل الفئات
        public decimal GrandTotalNet =>
            (TotalCashIn_Men + TotalCashIn_Women + TotalCashIn_Beauty)
            - (TotalRefunds_Men + TotalRefunds_Women)
            - TotalExpenses;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 1. إعداد قائمة الفروع (للأدمن)
            if (user.BranchId == null)
            {
                ViewData["BranchList"] = new SelectList(_context.Branches, "Id", "Name", SelectedBranchId);
            }

            // 2. تحديد فرع الفلترة
            int? filterBranchId = user.BranchId.HasValue ? user.BranchId : SelectedBranchId;

            // =========================================================
            // 1. حركة الوارد (Bookings - Pickups)
            // =========================================================
            var pickupsQuery = _context.Bookings
                .Include(b => b.BookingItems).ThenInclude(bi => bi.ProductItem).ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.PickupDate.Date == ReportDate.Date)
                .Where(b => b.Status != BookingStatus.Cancelled);

            if (filterBranchId.HasValue) pickupsQuery = pickupsQuery.Where(b => b.BranchId == filterBranchId);

            var pickups = await pickupsQuery.ToListAsync();

            foreach (var booking in pickups)
            {
                var dept = GetBookingDepartment(booking);
                if (dept == DepartmentType.Men) TotalCashIn_Men += booking.PaidAmount;
                else TotalCashIn_Women += booking.PaidAmount;
            }

            // =========================================================
            // 2. حركة المنصرف (Refunds - Returns)
            // =========================================================
            var returnsQuery = _context.Bookings
                .Include(b => b.BookingItems).ThenInclude(bi => bi.ProductItem).ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.ReturnDate.Date == ReportDate.Date)
                .Where(b => b.Status == BookingStatus.Returned);

            if (filterBranchId.HasValue) returnsQuery = returnsQuery.Where(b => b.BranchId == filterBranchId);

            var returns = await returnsQuery.ToListAsync();

            foreach (var booking in returns)
            {
                decimal refundAmount = booking.InsuranceAmount - booking.InsuranceDeduction;
                var dept = GetBookingDepartment(booking);

                if (dept == DepartmentType.Men) TotalRefunds_Men += refundAmount;
                else TotalRefunds_Women += refundAmount;
            }

            // =========================================================
            // 3. الكوافير (Salon)
            // =========================================================
            var salonQuery = _context.SalonAppointments
                .Where(s => s.AppointmentDate.Date == ReportDate.Date)
                .Where(s => s.Status != SalonAppointmentStatus.Cancelled);

            if (filterBranchId.HasValue) salonQuery = salonQuery.Where(s => s.BranchId == filterBranchId);

            TotalCashIn_Beauty = await salonQuery.SumAsync(s => s.PaidAmount);

            // =========================================================
            // 4. ✅ المصروفات (Expenses) - الجديد
            // =========================================================
            var expensesQuery = _context.Expenses
                .Where(e => e.ExpenseDate.Date == ReportDate.Date);

            if (filterBranchId.HasValue) expensesQuery = expensesQuery.Where(e => e.BranchId == filterBranchId);

            TotalExpenses = await expensesQuery.SumAsync(e => e.Amount);
        }

        private DepartmentType GetBookingDepartment(Booking booking)
        {
            var item = booking.BookingItems.FirstOrDefault()?.ProductItem;
            return item?.ProductDefinition?.Department ?? DepartmentType.Women;
        }
    }
}