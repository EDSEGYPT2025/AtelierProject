using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // 1. هام جداً
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 2. تعريف مدير المستخدمين

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Booking Booking { get; set; } = default!;

        [BindProperty]
        public int SelectedItemId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // إعداد التواريخ الافتراضية
            Booking = new Booking
            {
                PickupDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(3),
                PaidAmount = 0,
                TotalAmount = 0
            };

            // تحميل القوائم بناءً على الفرع والصلاحيات
            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. جلب المستخدم الحالي
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToPage("/Account/Login");

            // 2. التحقق من صحة التواريخ
            if (Booking.ReturnDate <= Booking.PickupDate)
            {
                ModelState.AddModelError("Booking.ReturnDate", "تاريخ الإرجاع يجب أن يكون بعد تاريخ الاستلام.");
                await LoadSelectListsAsync();
                return Page();
            }

            // 3. التحقق من توافر القطعة (Availability Check)
            bool isBooked = await _context.BookingItems
                .Include(bi => bi.Booking)
                .AnyAsync(bi =>
                    bi.ProductItemId == SelectedItemId &&
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
                ModelState.AddModelError(string.Empty, "عذراً، القطعة المختارة محجوزة بالفعل في هذه الفترة لعميل آخر.");
                await LoadSelectListsAsync();
                return Page();
            }

            // 4. إعداد الحجز وحفظ الفرع
            Booking.BranchId = currentUser.BranchId; // ✅ تسجيل الفرع تلقائياً
            Booking.Status = BookingStatus.New;

            // 5. إضافة القطعة للحجز
            // نحتاج لجلب السعر الحالي للقطعة (اختياري ولكن مفضل للدقة)
            // var productItem = await _context.ProductItems.FindAsync(SelectedItemId); 
            // if(productItem.BranchId != currentUser.BranchId) { ... error ... } // حماية إضافية

            if (Booking.BookingItems == null)
            {
                Booking.BookingItems = new List<BookingItem>();
            }

            var bookingItem = new BookingItem
            {
                ProductItemId = SelectedItemId,
                // هنا نأخذ المبلغ الذي أدخله المستخدم كإجمالي، أو يمكن جلبه من سعر القطعة
                RentalPrice = Booking.TotalAmount
            };
            Booking.BookingItems.Add(bookingItem);

            _context.Bookings.Add(Booking);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // دالة تحميل القوائم (تم تعديلها لتكون Async وتدعم الفلترة)
        private async Task LoadSelectListsAsync()
        {
            // معرفة المستخدم الحالي لتطبيق الفلاتر
            var user = await _userManager.GetUserAsync(User);
            int? branchId = user?.BranchId;

            // 1. فلترة العملاء (اختياري: هل العميل مشترك بين الفروع أم خاص بالفرع؟)
            // هنا افترضنا أنك تريد رؤية عملاء فرعك فقط، لو تريد الكل احذف شرط Where
            var clientsQuery = _context.Clients.AsQueryable();
            // if (branchId.HasValue) clientsQuery = clientsQuery.Where(c => c.BranchId == branchId); 

            ViewData["ClientId"] = new SelectList(await clientsQuery.ToListAsync(), "Id", "Name");

            // 2. فلترة المنتجات (أهم جزء)
            var itemsQuery = _context.ProductItems
                .AsNoTracking()
                .Include(p => p.ProductDefinition)
                .AsQueryable();

            // أ) فلتر الفرع: يجب أن يرى بضاعة فرعه فقط
            if (branchId.HasValue)
            {
                itemsQuery = itemsQuery.Where(p => p.BranchId == branchId);
            }

            // ب) فلتر الأقسام (صلاحيات المستخدم)
            // سنقوم بسحب البيانات ثم الفلترة في الذاكرة للسهولة مع الـ Enum، 
            // أو يمكن بناء Expression معقد، لكن هذا أسهل للقراءة الآن:
            var allItems = await itemsQuery.ToListAsync();

            var filteredItems = allItems.Where(p =>
            {
                // إذا لم يكن هناك تعريف للمنتج، نعرضه احتياطياً أو نخفيه
                if (p.ProductDefinition == null) return false;

                var dept = p.ProductDefinition.Department;

                // إذا كان المنتج رجالي، هل الموظف لديه صلاحية رجالي؟
                if (dept == DepartmentType.Men && user.CanAccessMenSection) return true;

                // إذا كان المنتج حريمي، هل الموظف لديه صلاحية حريمي؟
                if (dept == DepartmentType.Women && user.CanAccessWomenSection) return true;

                return false;
            })
            .Select(p => new
            {
                Id = p.Id,
                DisplayText = $"[{p.UniqueCode}] {(p.ProductDefinition != null ? p.ProductDefinition.Name : "غير معروف")} - {p.Size}"
            })
            .ToList();

            ViewData["ProductItemId"] = new SelectList(filteredItems, "Id", "DisplayText");
        }
    }
}