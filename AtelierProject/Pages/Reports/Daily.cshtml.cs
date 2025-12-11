using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering; // هام للقوائم
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

        // المتغير الجديد: اختيار الفرع (للأدمن فقط)
        [BindProperty(SupportsGet = true)]
        public int? SelectedBranchId { get; set; }

        // --- المتغيرات المالية (كما هي) ---
        public decimal TotalCashIn_Men { get; set; }
        public decimal TotalCashIn_Women { get; set; }
        public decimal TotalCashIn_Beauty { get; set; }

        public decimal TotalRefunds_Men { get; set; }
        public decimal TotalRefunds_Women { get; set; }

        public decimal NetTreasury_Men => TotalCashIn_Men - TotalRefunds_Men;
        public decimal NetTreasury_WomenAndBeauty => (TotalCashIn_Women - TotalRefunds_Women) + TotalCashIn_Beauty;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 1. إعداد قائمة الفروع للأدمن فقط
            if (user.BranchId == null)
            {
                ViewData["BranchList"] = new SelectList(_context.Branches, "Id", "Name", SelectedBranchId);
            }

            // 2. تحديد "فرع الفلترة"
            // إذا كان موظفاً -> نستخدم فرعه الإجباري
            // إذا كان أدمن واختار فرعاً -> نستخدم الفرع المختار
            // إذا كان أدمن ولم يختر (اختار الكل) -> تظل null (لجلب الإجمالي)
            int? filterBranchId = user.BranchId.HasValue ? user.BranchId : SelectedBranchId;


            // =========================================================
            // أولاً: حركة الوارد (التسليمات)
            // =========================================================
            var pickupsQuery = _context.Bookings
                .Include(b => b.BookingItems).ThenInclude(bi => bi.ProductItem).ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.PickupDate.Date == ReportDate.Date)
                .Where(b => b.Status != BookingStatus.Cancelled);

            // تطبيق الفلتر الذكي
            if (filterBranchId.HasValue)
            {
                pickupsQuery = pickupsQuery.Where(b => b.BranchId == filterBranchId);
            }

            var pickups = await pickupsQuery.ToListAsync();

            foreach (var booking in pickups)
            {
                var dept = GetBookingDepartment(booking);
                if (dept == DepartmentType.Men) TotalCashIn_Men += booking.PaidAmount;
                else TotalCashIn_Women += booking.PaidAmount;
            }

            // =========================================================
            // ثانياً: حركة المنصرف (المرتجعات)
            // =========================================================
            var returnsQuery = _context.Bookings
                .Include(b => b.BookingItems).ThenInclude(bi => bi.ProductItem).ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.ReturnDate.Date == ReportDate.Date)
                .Where(b => b.Status == BookingStatus.Returned);

            // تطبيق الفلتر الذكي
            if (filterBranchId.HasValue)
            {
                returnsQuery = returnsQuery.Where(b => b.BranchId == filterBranchId);
            }

            var returns = await returnsQuery.ToListAsync();

            foreach (var booking in returns)
            {
                decimal refundAmount = booking.InsuranceAmount - booking.InsuranceDeduction;
                var dept = GetBookingDepartment(booking);

                if (dept == DepartmentType.Men) TotalRefunds_Men += refundAmount;
                else TotalRefunds_Women += refundAmount;
            }

            // =========================================================
            // ثالثاً: الكوافير
            // =========================================================
            var salonQuery = _context.SalonAppointments
                .Where(s => s.AppointmentDate.Date == ReportDate.Date)
                .Where(s => s.Status != SalonAppointmentStatus.Cancelled);

            // تطبيق الفلتر الذكي
            if (filterBranchId.HasValue)
            {
                salonQuery = salonQuery.Where(s => s.BranchId == filterBranchId);
            }

            TotalCashIn_Beauty = await salonQuery.SumAsync(s => s.PaidAmount);
        }

        private DepartmentType GetBookingDepartment(Booking booking)
        {
            var item = booking.BookingItems.FirstOrDefault()?.ProductItem;
            return item?.ProductDefinition?.Department ?? DepartmentType.Women;
        }
    }
}