using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Booking Booking { get; set; } = default!;

        // لاستقبال قائمة IDs للقطع المختارة
        [BindProperty]
        public List<int> SelectedItemIds { get; set; } = new List<int>();

        // =========================================================
        // 1. عند فتح الصفحة (GET)
        // =========================================================
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // ⛔ منع المدير العام (بدون فرع) من إنشاء حجوزات، يجب أن يكون موظف فرع
            if (user.BranchId == null)
            {
                TempData["Error"] = "غير مسموح للمدير العام بإنشاء حجوزات. يرجى الدخول بحساب موظف الفرع.";
                return RedirectToPage("./Index");
            }

            // إعداد القيم الافتراضية
            Booking = new Booking
            {
                PickupDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(3),
                PaidAmount = 0,
                TotalAmount = 0,
                Discount = 0,      // ✅ قيمة افتراضية للخصم
                InsuranceAmount = 0
            };

            await LoadSelectListsAsync();
            return Page();
        }

        // =========================================================
        // 2. عند الحفظ (POST)
        // =========================================================
        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToPage("/Account/Login");
            if (currentUser.BranchId == null) return RedirectToPage("./Index");

            // 1. التحقق من منطقية التواريخ
            if (Booking.ReturnDate <= Booking.PickupDate)
            {
                ModelState.AddModelError("Booking.ReturnDate", "تاريخ الإرجاع يجب أن يكون بعد تاريخ الاستلام.");
                await LoadSelectListsAsync();
                return Page();
            }

            // 2. التحقق من اختيار قطع
            if (SelectedItemIds == null || !SelectedItemIds.Any())
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار قطعة واحدة على الأقل.");
                await LoadSelectListsAsync();
                return Page();
            }

            // 3. التحقق من توفر القطع (منع التداخل في الحجوزات)
            // الشرح: نتأكد أن القطعة غير محجوزة في أي حجز آخر (غير ملغي أو مرتجع) يتقاطع زمنياً مع الحجز الحالي
            foreach (var itemId in SelectedItemIds)
            {
                bool isBooked = await _context.BookingItems
                    .Include(bi => bi.Booking)
                    .AnyAsync(bi =>
                        bi.ProductItemId == itemId &&
                        bi.Booking.Status != BookingStatus.Cancelled &&
                        bi.Booking.Status != BookingStatus.Returned &&
                        (
                            (Booking.PickupDate >= bi.Booking.PickupDate && Booking.PickupDate < bi.Booking.ReturnDate) ||
                            (Booking.ReturnDate > bi.Booking.PickupDate && Booking.ReturnDate <= bi.Booking.ReturnDate) ||
                            (Booking.PickupDate <= bi.Booking.PickupDate && Booking.ReturnDate >= bi.Booking.ReturnDate)
                        )
                    );

                if (isBooked)
                {
                    var itemInfo = await _context.ProductItems
                        .Include(p => p.ProductDefinition)
                        .Where(p => p.Id == itemId)
                        .Select(p => p.ProductDefinition.Name + " (" + p.Barcode + ")")
                        .FirstOrDefaultAsync();

                    ModelState.AddModelError(string.Empty, $"عذراً، القطعة '{itemInfo}' محجوزة بالفعل في هذه الفترة.");
                    await LoadSelectListsAsync();
                    return Page();
                }
            }

            // 4. إعداد وحفظ بيانات الحجز
            Booking.BranchId = currentUser.BranchId;
            Booking.CreatedDate = DateTime.Now;
            Booking.Status = BookingStatus.New;

            // ✅ ملاحظة: Booking.TotalAmount و Booking.Discount يأتيان تلقائياً من الـ Form

            Booking.BookingItems = new List<BookingItem>();

            // جلب تفاصيل القطع لربطها وحفظ سعرها الأصلي للمرجع
            var selectedItemsDetails = await _context.ProductItems
                .Include(p => p.ProductDefinition)
                .Where(p => SelectedItemIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in selectedItemsDetails)
            {
                Booking.BookingItems.Add(new BookingItem
                {
                    ProductItemId = item.Id,
                    RentalPrice = item.ProductDefinition.RentalPrice // تسجيل السعر الأصلي للقطعة
                });
            }

            _context.Bookings.Add(Booking);
            await _context.SaveChangesAsync();

            // 5. تسجيل حركة مالية في الخزينة (إذا تم دفع عربون)
            if (Booking.PaidAmount > 0)
            {
                // تحديد القسم (أولوية للرجال إذا وجد، وإلا نساء) - منطق اختياري
                var department = DepartmentType.Women;
                if (selectedItemsDetails.Any(x => x.ProductDefinition.Department == DepartmentType.Men))
                {
                    department = DepartmentType.Men;
                }

                var transaction = new SafeTransaction
                {
                    Amount = Booking.PaidAmount,
                    Type = TransactionType.Income, // إيراد
                    Department = department,
                    BranchId = Booking.BranchId.Value,
                    TransactionDate = DateTime.Now,
                    Description = $"عربون حجز رقم #{Booking.Id} - العميل: {Booking.ClientId}", // يفضل جلب اسم العميل
                    ReferenceId = Booking.Id.ToString(),
                    CreatedByUserId = currentUser.Id
                };

                _context.SafeTransactions.Add(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        // دالة مساعدة لتحميل القوائم
        private async Task LoadSelectListsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            int? branchId = user?.BranchId;

            // قائمة العملاء
            var clientsQuery = _context.Clients.OrderBy(c => c.Name).AsQueryable();
            ViewData["ClientId"] = new SelectList(await clientsQuery.ToListAsync(), "Id", "Name");

            // قائمة المنتجات (حسب الفرع وصلاحيات المستخدم)
            var itemsQuery = _context.ProductItems
                .AsNoTracking()
                .Include(p => p.ProductDefinition)
                .AsQueryable();

            if (branchId.HasValue)
            {
                itemsQuery = itemsQuery.Where(p => p.BranchId == branchId);
            }

            var allItems = await itemsQuery.ToListAsync();

            // فلترة حسب الصلاحية (رجالي/حريمي)
            var filteredItems = allItems.Where(p =>
            {
                if (p.ProductDefinition == null) return false;
                var dept = p.ProductDefinition.Department;

                if (dept == DepartmentType.Men && user.CanAccessMenSection) return true;
                if (dept == DepartmentType.Women && user.CanAccessWomenSection) return true;

                return false;
            })
            .Select(p => new
            {
                Id = p.Id,
                // نص العرض في القائمة: [الكود] الاسم (النسخة) - المقاس - السعر
                DisplayText = $"[{(string.IsNullOrEmpty(p.Barcode) ? "بدون كود" : p.Barcode)}] " +
                              $"{(string.IsNullOrEmpty(p.Name) ? p.ProductDefinition.Name : p.ProductDefinition.Name + " (" + p.Name + ")")} " +
                              $"- {p.Size} - {p.ProductDefinition.RentalPrice:N0} ج.م"
            })
            .OrderBy(x => x.DisplayText)
            .ToList();

            ViewData["ProductItemId"] = new SelectList(filteredItems, "Id", "DisplayText");
        }
    }
}