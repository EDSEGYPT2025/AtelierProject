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

        // --- خصائص الفلاتر (Binding) ---
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; }

        // =========================================================
        // 1. دالة العرض (GET)
        // =========================================================
        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            // 1. الاستعلام الأساسي (مع جلب البيانات المرتبطة)
            IQueryable<Booking> query = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                    .ThenInclude(pi => pi.ProductDefinition)
                .OrderByDescending(b => b.Id); // الأحدث أولاً

            // 2. 🛑 فلتر الأمان (الفرع):
            // إذا كان المستخدم له فرع محدد، نعرض له حجوزات فرعه فقط.
            // إذا كان (Admin) والفرع null، يتجاوز هذا الشرط ويرى الكل.
            if (currentUser.BranchId != null)
            {
                query = query.Where(b => b.BranchId == currentUser.BranchId);
            }

            // 3. فلتر البحث (رقم الحجز، اسم العميل، الهاتف)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // إذا كان البحث برقم (نبحث عن رقم الحجز)
                if (int.TryParse(SearchTerm, out int id))
                {
                    query = query.Where(b => b.Id == id || b.Client.Phone.Contains(SearchTerm));
                }
                // وإلا نبحث بالاسم أو الهاتف
                else
                {
                    query = query.Where(b => b.Client.Name.Contains(SearchTerm) || b.Client.Phone.Contains(SearchTerm));
                }
            }

            // 4. فلتر التاريخ والنوع
            if (DateFilter.HasValue)
            {
                if (TypeFilter == "Pickup") // مواعيد التسليم
                {
                    query = query.Where(b => b.PickupDate.Date == DateFilter.Value.Date);
                }
                else if (TypeFilter == "Return") // مواعيد الإرجاع
                {
                    query = query.Where(b => b.ReturnDate.Date == DateFilter.Value.Date);
                }
                else // تاريخ الإنشاء (الافتراضي)
                {
                    query = query.Where(b => b.CreatedDate.Date == DateFilter.Value.Date);
                }
            }

            // 5. فلتر الحالة (اختياري لو أردت استخدامه مستقبلاً)
            if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<BookingStatus>(StatusFilter, out var status))
            {
                query = query.Where(b => b.Status == status);
            }

            // تنفيذ الاستعلام
            Bookings = await query.ToListAsync();
        }

        // =========================================================
        // 2. دالة إلغاء الحجز (POST)
        // =========================================================
        public async Task<IActionResult> OnPostCancelBookingAsync(int bookingId, decimal refundAmount)
        {
            // أ. جلب الحجز
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .ThenInclude(bi => bi.ProductItem)
                .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            // ب. التحقق من الصلاحية (الأمان)
            var currentUser = await _userManager.GetUserAsync(User);
            // لو المستخدم له فرع، والحجز لفرع آخر -> ممنوع
            if (currentUser.BranchId != null && booking.BranchId != currentUser.BranchId)
            {
                return Forbid();
            }

            // ج. تحديث الحالة
            booking.Status = BookingStatus.Cancelled;
            booking.Notes += $" | تم الإلغاء بواسطة {currentUser.FullName} بتاريخ {DateTime.Now:yyyy-MM-dd}";

            // د. معالجة الخزينة (إذا كان هناك استرداد)
            if (refundAmount > 0)
            {
                if (refundAmount > booking.PaidAmount) refundAmount = booking.PaidAmount;

                // تحديد القسم (رجال/سيدات) بناءً على أول قطعة
                var firstItemDef = booking.BookingItems.FirstOrDefault()?.ProductItem?.ProductDefinition;
                DepartmentType targetDept = DepartmentType.Women; // الافتراضي

                if (firstItemDef != null && firstItemDef.Department == DepartmentType.Men)
                {
                    targetDept = DepartmentType.Men;
                }

                var transaction = new SafeTransaction
                {
                    Amount = refundAmount,
                    Type = TransactionType.Expense, // مصروف
                    Department = targetDept,
                    BranchId = booking.BranchId ?? currentUser.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"استرداد الغاء حجز رقم #{booking.Id}",
                    ReferenceId = booking.Id.ToString(),
                    CreatedByUserId = currentUser.Id
                };

                _context.SafeTransactions.Add(transaction);
            }

            // هـ. حفظ التغييرات
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}