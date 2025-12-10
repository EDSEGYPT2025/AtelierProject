using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // لإحضار بيانات المستخدم

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- المتغيرات المالية ---
        public decimal TotalInsuranceHeld { get; set; }
        public decimal WomensInsurance { get; set; }
        public decimal MensInsurance { get; set; }

        public decimal TotalCollectedMoney { get; set; }
        public decimal IncomeWomen { get; set; }
        public decimal IncomeMen { get; set; }
        public decimal IncomeBeauty { get; set; }

        public decimal TotalPendingBalance { get; set; }

        // --- القوائم ---
        public List<Booking> PickupsToday { get; set; } = new();
        public List<Booking> ReturnsToday { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. معرفة المستخدم الحالي وفرعه وصلاحياته
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return; // حماية إضافية

            // إذا كان المستخدم Admin (BranchId == null)، سنعرض له كل الفروع (أو نضع منطقاً آخر)
            // هنا سنفترض أنه لو لم يكن له فرع، يرى كل شيء. لو له فرع، نفلتر به.
            int? userBranchId = user.BranchId;

            var today = DateTime.Today;

            // =================================================================================
            // أولاً: الأتيليه (Bookings)
            // =================================================================================

            // استعلام أساسي مفلتر حسب الفرع (إذا وجد)
            var bookingsQuery = _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .AsQueryable();

            if (userBranchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BranchId == userBranchId.Value);
            }

            // سحب البيانات للذاكرة لعمل الحسابات المعقدة
            // (نستبعد الملغي والمرتجع لحساب العهدة)
            var activeBookings = await bookingsQuery
                .Where(b => b.Status != BookingStatus.Returned && b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            // --- تطبيق فلتر الأقسام (صلاحيات المستخدم) ---
            // سنحسب فقط الأقسام المسموح له برؤيتها

            if (user.CanAccessWomenSection)
            {
                WomensInsurance = activeBookings
                    .Where(b => GetBookingDepartment(b) == DepartmentType.Women)
                    .Sum(b => b.InsuranceAmount);
            }

            if (user.CanAccessMenSection)
            {
                MensInsurance = activeBookings
                    .Where(b => GetBookingDepartment(b) == DepartmentType.Men)
                    .Sum(b => b.InsuranceAmount);
            }

            TotalInsuranceHeld = WomensInsurance + MensInsurance;

            // حساب المديونيات (الآجل) للموظف حسب صلاحياته
            // (يمكن تبسيطها لتشمل كل المديونيات في الفرع، أو تصفيتها حسب القسم كما فعلنا في التأمين)
            TotalPendingBalance = activeBookings.Sum(b => b.RemainingRentalAmount);


            // --- حساب الإيرادات (المال المقبوض) ---
            var revenueBookings = await bookingsQuery
                .Where(b => b.PaidAmount > 0 || b.InsuranceDeduction > 0)
                .ToListAsync();

            if (user.CanAccessWomenSection)
            {
                IncomeWomen = revenueBookings
                    .Where(b => GetBookingDepartment(b) == DepartmentType.Women)
                    .Sum(b => b.PaidAmount + b.InsuranceDeduction);
            }

            if (user.CanAccessMenSection)
            {
                IncomeMen = revenueBookings
                    .Where(b => GetBookingDepartment(b) == DepartmentType.Men)
                    .Sum(b => b.PaidAmount + b.InsuranceDeduction);
            }


            // =================================================================================
            // ثانياً: الكوافير (Salon)
            // =================================================================================

            if (user.CanAccessBeautySection)
            {
                var salonQuery = _context.SalonAppointments.AsQueryable();

                if (userBranchId.HasValue)
                {
                    salonQuery = salonQuery.Where(s => s.BranchId == userBranchId.Value);
                }

                IncomeBeauty = await salonQuery.SumAsync(s => s.TotalAmount);
            }
            else
            {
                IncomeBeauty = 0;
            }

            // الإجمالي الكلي لما يراه الموظف
            TotalCollectedMoney = IncomeWomen + IncomeMen + IncomeBeauty;


            // =================================================================================
            // ثالثاً: قوائم المهام (تسليمات ومرتجعات)
            // =================================================================================

            // التسليمات اليوم (New -> PickedUp)
            PickupsToday = await bookingsQuery
                .Include(b => b.Client)
                .Where(b => b.PickupDate.Date == today && b.Status == BookingStatus.New)
                .ToListAsync();

            // المرتجعات اليوم (PickedUp -> Returned)
            ReturnsToday = await bookingsQuery
                .Include(b => b.Client)
                .Where(b => b.ReturnDate.Date == today && b.Status == BookingStatus.PickedUp)
                .ToListAsync();
        }

        private DepartmentType GetBookingDepartment(Booking booking)
        {
            var firstItem = booking.BookingItems.FirstOrDefault();
            if (firstItem?.ProductItem?.ProductDefinition == null) return DepartmentType.Women;
            return firstItem.ProductItem.ProductDefinition.Department;
        }
    }
}