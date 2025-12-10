using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        // --- خزينة الرجالي ---
        public decimal MenIncome { get; set; }
        public int MenCount { get; set; }

        // --- خزينة الحريمي والبيوتي (مشتركة) ---
        public decimal WomenIncome { get; set; }
        public int WomenCount { get; set; }

        public decimal BeautyIncome { get; set; }
        public int BeautyCount { get; set; }

        public decimal TotalWomenAndBeautyTreasury => WomenIncome + BeautyIncome;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 1. جلب حجوزات الأتيليه لهذا اليوم (تسليمات)
            var bookingsQuery = _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.PickupDate.Date == ReportDate.Date)
                .Where(b => b.Status != BookingStatus.Cancelled); // استبعاد الملغي

            // فلتر الفرع
            if (user.BranchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BranchId == user.BranchId);
            }

            var bookings = await bookingsQuery.ToListAsync();

            // 2. فصل الإيرادات (رجالي / حريمي)
            foreach (var booking in bookings)
            {
                var dept = GetBookingDepartment(booking);

                if (dept == DepartmentType.Men)
                {
                    MenIncome += booking.PaidAmount;
                    MenCount++;
                }
                else if (dept == DepartmentType.Women)
                {
                    WomenIncome += booking.PaidAmount;
                    WomenCount++;
                }
            }

            // 3. جلب إيرادات الصالون
            var salonQuery = _context.SalonAppointments
                .Where(s => s.AppointmentDate.Date == ReportDate.Date)
                .Where(s => s.Status != SalonAppointmentStatus.Cancelled);

            if (user.BranchId.HasValue)
            {
                salonQuery = salonQuery.Where(s => s.BranchId == user.BranchId);
            }

            var salonAppointments = await salonQuery.ToListAsync();

            BeautyIncome = salonAppointments.Sum(s => s.PaidAmount);
            BeautyCount = salonAppointments.Count;
        }

        // دالة مساعدة لمعرفة قسم الحجز
        private DepartmentType GetBookingDepartment(Booking booking)
        {
            var item = booking.BookingItems.FirstOrDefault()?.ProductItem;
            if (item?.ProductDefinition != null)
            {
                return item.ProductDefinition.Department;
            }
            return DepartmentType.Women; // افتراضي
        }
    }
}