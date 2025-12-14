using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AtelierProject.Pages.Reports
{
    [Authorize]
    public class GeneralReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GeneralReportModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // مدخلات الفلتر
        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public int? BranchId { get; set; }

        // النتائج
        public ReportSummary Summary { get; set; } = new ReportSummary();
        public List<BranchPerformance> BranchPerformances { get; set; } = new List<BranchPerformance>();

        // قوائم التفاصيل (للعرض في الجداول)
        public List<Booking> DetailedBookings { get; set; }
        public List<SalonAppointment> DetailedAppointments { get; set; }
        public List<Expense> DetailedExpenses { get; set; }

        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // 1. تطبيق الصلاحيات (الذكاء الأمني)
            if (user.BranchId != null)
            {
                // موظف عادي: مجبر على فرعه فقط
                BranchId = user.BranchId;
                IsAdmin = false;
            }
            else
            {
                // مدير عام: نملأ القائمة له ليختار
                IsAdmin = true;
                ViewData["BranchList"] = new SelectList(_context.Branches, "Id", "Name");
            }

            // ضبط التوقيت لنهاية اليوم في البحث
            DateTime searchEnd = EndDate.Date.AddDays(1).AddTicks(-1);
            DateTime searchStart = StartDate.Date;

            // 2. الاستعلامات الأساسية (تم إضافة Include للعميل)
            var bookingsQuery = _context.Bookings
                .Include(b => b.Branch)
                .Include(b => b.Client) // ✅ ضروري لإظهار اسم العميل
                .Where(b => b.PickupDate >= searchStart && b.PickupDate <= searchEnd);

            var salonQuery = _context.SalonAppointments
                .Include(s => s.Branch)
                .Include(s => s.Client) // ✅ ضروري لإظهار اسم العميلة
                .Where(s => s.AppointmentDate >= searchStart && s.AppointmentDate <= searchEnd);

            var expensesQuery = _context.Expenses
                .Include(e => e.Branch)
                .Include(e => e.ExpenseCategory) // ✅ أضفت هذا أيضاً ليظهر اسم بند المصروف
                .Where(e => e.ExpenseDate >= searchStart && e.ExpenseDate <= searchEnd);

            // 3. فلتر الفرع (إذا تم اختياره أو إجباره)
            if (BranchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BranchId == BranchId);
                salonQuery = salonQuery.Where(s => s.BranchId == BranchId);
                expensesQuery = expensesQuery.Where(e => e.BranchId == BranchId);
            }

            // 4. جلب البيانات التفصيلية (للجداول)
            DetailedBookings = await bookingsQuery.OrderByDescending(b => b.Id).ToListAsync();
            DetailedAppointments = await salonQuery.OrderByDescending(s => s.AppointmentDate).ToListAsync();
            DetailedExpenses = await expensesQuery.OrderByDescending(e => e.ExpenseDate).ToListAsync();

            // 5. حساب المجاميع العامة (Dashboard Numbers)
            Summary.TotalAtelier = DetailedBookings.Sum(b => b.PaidAmount); // المبلغ المقبوض فعلياً
            Summary.TotalSalon = DetailedAppointments.Sum(s => s.PaidAmount);
            Summary.TotalExpenses = DetailedExpenses.Sum(e => e.Amount);
            Summary.NetIncome = (Summary.TotalAtelier + Summary.TotalSalon) - Summary.TotalExpenses;

            // 6. تجميع الأداء حسب كل فرع (Group By) - للتقارير المجمعة
            // نجمع كل الفروع التي لها حركة في هذه الفترة
            var branchIds = DetailedBookings.Select(b => b.BranchId).Concat(DetailedAppointments.Select(s => s.BranchId)).Concat(DetailedExpenses.Select(e => e.BranchId)).Distinct();

            foreach (var bId in branchIds)
            {
                if (bId == null) continue;

                string bName = _context.Branches.Find(bId)?.Name ?? "غير محدد";

                BranchPerformances.Add(new BranchPerformance
                {
                    BranchName = bName,
                    AtelierRevenue = DetailedBookings.Where(b => b.BranchId == bId).Sum(b => b.PaidAmount),
                    SalonRevenue = DetailedAppointments.Where(s => s.BranchId == bId).Sum(s => s.PaidAmount),
                    Expenses = DetailedExpenses.Where(e => e.BranchId == bId).Sum(e => e.Amount)
                });
            }

            return Page();
        }

        // كلاسات مساعدة لتنظيم البيانات
        public class ReportSummary
        {
            public decimal TotalAtelier { get; set; }
            public decimal TotalSalon { get; set; }
            public decimal TotalExpenses { get; set; }
            public decimal NetIncome { get; set; }
        }

        public class BranchPerformance
        {
            public string BranchName { get; set; }
            public decimal AtelierRevenue { get; set; }
            public decimal SalonRevenue { get; set; }
            public decimal Expenses { get; set; }
            public decimal Net => (AtelierRevenue + SalonRevenue) - Expenses;
        }
    }
}