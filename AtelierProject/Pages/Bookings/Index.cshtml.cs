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
        public string StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 1. الاستعلام الأساسي
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

            // 3. فلتر البحث
            if (!string.IsNullOrEmpty(SearchTerm))
            {
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

            // 5. فلتر التاريخ
            if (DateFilter.HasValue)
            {
                if (TypeFilter == "Pickup")
                {
                    query = query.Where(b => b.PickupDate.Date == DateFilter.Value.Date);
                }
                else if (TypeFilter == "Return")
                {
                    query = query.Where(b => b.ReturnDate.Date == DateFilter.Value.Date);
                }
                else
                {
                    query = query.Where(b => b.CreatedDate.Date == DateFilter.Value.Date);
                }
            }

            // ============================================================
            // 6. ✅ فلتر الأقسام (حسب صلاحيات الموظف) ✅
            // ============================================================

            // الحالة أ: إذا كان المستخدم يملك الصلاحيتين (أو مدير عام)، لا نفلتر شيئاً (يرى الكل)
            if (user.CanAccessMenSection && user.CanAccessWomenSection)
            {
                // لا تغيير - يرى جميع الحجوزات
            }
            // الحالة ب: مصرح له بالرجالي فقط
            else if (user.CanAccessMenSection && !user.CanAccessWomenSection)
            {
                // نعرض الحجوزات التي تحتوي على قطعة واحدة على الأقل من قسم الرجال
                query = query.Where(b => b.BookingItems.Any(bi => bi.ProductItem.ProductDefinition.Department == DepartmentType.Men));
            }
            // الحالة ج: مصرح له بالحريمي فقط
            else if (!user.CanAccessMenSection && user.CanAccessWomenSection)
            {
                // نعرض الحجوزات التي تحتوي على قطعة واحدة على الأقل من قسم النساء
                query = query.Where(b => b.BookingItems.Any(bi => bi.ProductItem.ProductDefinition.Department == DepartmentType.Women));
            }
            // الحالة د: ليس لديه أي صلاحية (حالة نادرة)
            else
            {
                // نعرض قائمة فارغة
                query = query.Where(b => false);
            }
            // ============================================================

            // الترتيب: الأحدث أولاً
            Bookings = await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
        }
    }
}