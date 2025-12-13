using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- الإحصائيات (Cards) ---
        public int PickupsTodayCount { get; set; }
        public int ReturnsTodayCount { get; set; }
        public int ActiveRentalsCount { get; set; } // عدد القطع الموجودة حالياً عند العملاء
        public int OverdueCount { get; set; } // المتأخرات

        // --- القوائم (Tables) ---
        public List<Booking> OverdueBookings { get; set; } = new List<Booking>(); // قائمة المتأخرين
        public List<Booking> TodaysPickups { get; set; } = new List<Booking>(); // تسليمات اليوم

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            var today = DateTime.Today;

            // استعلام أساسي
            var bookingsQuery = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingItems).ThenInclude(bi => bi.ProductItem).ThenInclude(pi => pi.ProductDefinition)
                .AsQueryable();

            // 🛑 فلتر الفرع (هام جداً)
            if (user.BranchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.BranchId == user.BranchId);
            }

            // 1. حسابات العدادات
            // تسليمات اليوم (المفروض يستلموها النهاردة)
            PickupsTodayCount = await bookingsQuery.CountAsync(b => b.PickupDate.Date == today && b.Status == BookingStatus.New);

            // مرتجعات اليوم (المفروض يرجعوها النهاردة)
            ReturnsTodayCount = await bookingsQuery.CountAsync(b => b.ReturnDate.Date == today && b.Status == BookingStatus.PickedUp);

            // قطع خارج الأتيليه حالياً (مؤجرة)
            ActiveRentalsCount = await bookingsQuery.CountAsync(b => b.Status == BookingStatus.PickedUp);

            // المتأخرات (تاريخ الإرجاع فات، ولسه الحالة "مؤجر")
            OverdueCount = await bookingsQuery.CountAsync(b => b.ReturnDate.Date < today && b.Status == BookingStatus.PickedUp);

            // 2. جلب القوائم للتفاصيل
            // قائمة المتأخرين (الأخطر)
            OverdueBookings = await bookingsQuery
                .Where(b => b.ReturnDate.Date < today && b.Status == BookingStatus.PickedUp)
                .OrderBy(b => b.ReturnDate) // الأقدم تأخيراً يظهر أولاً
                .Take(5) // عرض آخر 5 فقط في الرئيسية
                .ToListAsync();

            // قائمة تسليمات اليوم (عشان نجهزها)
            TodaysPickups = await bookingsQuery
                .Where(b => b.PickupDate.Date == today && b.Status == BookingStatus.New)
                .OrderBy(b => b.CreatedDate)
                .Take(5)
                .ToListAsync();
        }
    }
}