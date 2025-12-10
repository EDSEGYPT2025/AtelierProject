using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Bookings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Booking> Bookings { get; set; } = default!;

        // فلاتر البحث
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } // New, PickedUp, Returned...

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; } // Pickup, Return

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 1. الاستعلام الأساسي (مع Include للبيانات المرتبطة)
            var query = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .AsQueryable();

            // 2. فلتر الفرع (إجباري)
            if (user.BranchId.HasValue)
            {
                query = query.Where(b => b.BranchId == user.BranchId);
            }

            // 3. فلتر البحث (اسم العميل، الهاتف، أو رقم الحجز)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // إذا كان البحث رقمي، نبحث في رقم الحجز أو الهاتف
                if (int.TryParse(SearchTerm, out int id))
                {
                    query = query.Where(b => b.Id == id || b.Client.Phone.Contains(SearchTerm));
                }
                else
                {
                    query = query.Where(b => b.Client.Name.Contains(SearchTerm) || b.Client.Phone.Contains(SearchTerm));
                }
            }

            // 4. فلتر الحالة
            if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<BookingStatus>(StatusFilter, out var status))
            {
                query = query.Where(b => b.Status == status);
            }

            // 5. فلتر التاريخ والنوع (تسليمات اليوم / مرتجعات اليوم)
            if (DateFilter.HasValue)
            {
                if (TypeFilter == "Pickup") // أريد تسليمات هذا اليوم
                {
                    query = query.Where(b => b.PickupDate.Date == DateFilter.Value.Date);
                }
                else if (TypeFilter == "Return") // أريد مرتجعات هذا اليوم
                {
                    query = query.Where(b => b.ReturnDate.Date == DateFilter.Value.Date);
                }
                else
                {
                    // بحث عام بالتاريخ (تاريخ الإنشاء)
                    query = query.Where(b => b.CreatedDate.Date == DateFilter.Value.Date);
                }
            }

            // 6. فلتر الأقسام (صلاحيات الموظف) - اختياري ولكن مفضل
            // نقوم بسحب البيانات ثم الفلترة في الذاكرة إذا كان الاستعلام معقداً، 
            // لكن هنا سنعرض الكل ونعتمد على أن الموظف يرى حجوزات فرعه.
            // (يمكنك إضافة منطق لإخفاء حجوزات الرجال عن موظف النساء هنا)

            // الترتيب: الأحدث أولاً
            Bookings = await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
        }
    }
}