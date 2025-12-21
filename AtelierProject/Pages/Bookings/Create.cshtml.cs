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

        // ✅ التعديل 1: تحويل المتغير لقائمة لاستقبال أكثر من قطعة
        [BindProperty]
        public List<int> SelectedItemIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync()
        {
            Booking = new Booking
            {
                PickupDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(3),
                PaidAmount = 0,
                TotalAmount = 0
            };

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToPage("/Account/Login");

            // التحقق من صحة التواريخ
            if (Booking.ReturnDate <= Booking.PickupDate)
            {
                ModelState.AddModelError("Booking.ReturnDate", "تاريخ الإرجاع يجب أن يكون بعد تاريخ الاستلام.");
                await LoadSelectListsAsync();
                return Page();
            }

            // التحقق من اختيار قطعة واحدة على الأقل
            if (SelectedItemIds == null || !SelectedItemIds.Any())
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار قطعة واحدة على الأقل لإتمام الحجز.");
                await LoadSelectListsAsync();
                return Page();
            }

            // التحقق من توفر جميع القطع المختارة
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
                    var itemName = _context.ProductItems.Include(p => p.ProductDefinition)
                                    .Where(p => p.Id == itemId)
                                    .Select(p => p.ProductDefinition.Name + " (" + p.Barcode + ")")
                                    .FirstOrDefault();

                    ModelState.AddModelError(string.Empty, $"عذراً، القطعة '{itemName}' محجوزة بالفعل في هذه الفترة.");
                    await LoadSelectListsAsync();
                    return Page();
                }
            }

            // إعداد بيانات الحجز الأساسية
            Booking.BranchId = currentUser.BranchId;
            Booking.Status = BookingStatus.New;

            if (Booking.BookingItems == null)
            {
                Booking.BookingItems = new List<BookingItem>();
            }

            // إضافة القطع للحجز
            foreach (var itemId in SelectedItemIds)
            {
                var bookingItem = new BookingItem
                {
                    ProductItemId = itemId,
                    RentalPrice = 0 // السعر الإجمالي مسجل في Booking.TotalAmount
                };
                Booking.BookingItems.Add(bookingItem);
            }

            // 1. حفظ الحجز أولاً للحصول على رقم الحجز (BookingId)
            _context.Bookings.Add(Booking);
            await _context.SaveChangesAsync();

            // ============================================================
            // ✅ إضافة جديدة: تسجيل العربون في خزنة القسم المختص
            // ============================================================
            if (Booking.PaidAmount > 0)
            {
                // محاولة معرفة القسم (رجالي/حريمي) من أول قطعة تم اختيارها
                var department = DepartmentType.Women; // الافتراضي
                var firstItemId = SelectedItemIds.FirstOrDefault();
                if (firstItemId != 0)
                {
                    var item = await _context.ProductItems
                        .Include(p => p.ProductDefinition)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == firstItemId);

                    if (item?.ProductDefinition != null)
                    {
                        department = item.ProductDefinition.Department;
                    }
                }

                var transaction = new SafeTransaction
                {
                    Amount = Booking.PaidAmount,
                    Type = TransactionType.Income, // إيراد
                    Department = department,       // الخزنة المحددة (رجالي/حريمي)
                    BranchId = Booking.BranchId ?? 1,
                    TransactionDate = DateTime.Now,
                    Description = $"عربون حجز رقم {Booking.Id}",
                    ReferenceId = Booking.Id.ToString(),
                    CreatedByUserId = currentUser.Id
                };

                _context.SafeTransactions.Add(transaction);
                await _context.SaveChangesAsync();
            }
            // ============================================================

            return RedirectToPage("./Index");
        }
        //public async Task<IActionResult> OnPostAsync()
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (currentUser == null) return RedirectToPage("/Account/Login");

        //    if (Booking.ReturnDate <= Booking.PickupDate)
        //    {
        //        ModelState.AddModelError("Booking.ReturnDate", "تاريخ الإرجاع يجب أن يكون بعد تاريخ الاستلام.");
        //        await LoadSelectListsAsync();
        //        return Page();
        //    }

        //    // ✅ التعديل 2: التحقق من اختيار قطعة واحدة على الأقل
        //    if (SelectedItemIds == null || !SelectedItemIds.Any())
        //    {
        //        ModelState.AddModelError(string.Empty, "يجب اختيار قطعة واحدة على الأقل لإتمام الحجز.");
        //        await LoadSelectListsAsync();
        //        return Page();
        //    }

        //    // ✅ التعديل 3: التحقق من توفر جميع القطع المختارة
        //    foreach (var itemId in SelectedItemIds)
        //    {
        //        bool isBooked = await _context.BookingItems
        //            .Include(bi => bi.Booking)
        //            .AnyAsync(bi =>
        //                bi.ProductItemId == itemId &&
        //                bi.Booking.Status != BookingStatus.Cancelled &&
        //                bi.Booking.Status != BookingStatus.Returned &&
        //                (
        //                    (Booking.PickupDate >= bi.Booking.PickupDate && Booking.PickupDate < bi.Booking.ReturnDate) ||
        //                    (Booking.ReturnDate > bi.Booking.PickupDate && Booking.ReturnDate <= bi.Booking.ReturnDate) ||
        //                    (Booking.PickupDate <= bi.Booking.PickupDate && Booking.ReturnDate >= bi.Booking.ReturnDate)
        //                )
        //            );

        //        if (isBooked)
        //        {
        //            // جلب اسم القطعة المحجوزة لعرضه في الخطأ
        //            var itemName = _context.ProductItems.Include(p => p.ProductDefinition)
        //                            .Where(p => p.Id == itemId)
        //                            .Select(p => p.ProductDefinition.Name + " (" + p.Barcode + ")")
        //                            .FirstOrDefault();

        //            ModelState.AddModelError(string.Empty, $"عذراً، القطعة '{itemName}' محجوزة بالفعل في هذه الفترة.");
        //            await LoadSelectListsAsync();
        //            return Page();
        //        }
        //    }

        //    Booking.BranchId = currentUser.BranchId;
        //    Booking.Status = BookingStatus.New;

        //    if (Booking.BookingItems == null)
        //    {
        //        Booking.BookingItems = new List<BookingItem>();
        //    }

        //    // ✅ التعديل 4: إضافة جميع القطع المختارة للحجز
        //    foreach (var itemId in SelectedItemIds)
        //    {
        //        var bookingItem = new BookingItem
        //        {
        //            ProductItemId = itemId,
        //            // بما أن السعر الإجمالي مسجل في Booking.TotalAmount
        //            // يمكننا وضع 0 هنا أو توزيع السعر لاحقاً إذا أردت
        //            RentalPrice = 0
        //        };
        //        Booking.BookingItems.Add(bookingItem);
        //    }

        //    _context.Bookings.Add(Booking);
        //    await _context.SaveChangesAsync();



        //    return RedirectToPage("./Index");
        //}

        private async Task LoadSelectListsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            int? branchId = user?.BranchId;

            var clientsQuery = _context.Clients.AsQueryable();
            ViewData["ClientId"] = new SelectList(await clientsQuery.ToListAsync(), "Id", "Name");

            var itemsQuery = _context.ProductItems
                .AsNoTracking()
                .Include(p => p.ProductDefinition)
                .AsQueryable();

            if (branchId.HasValue)
            {
                itemsQuery = itemsQuery.Where(p => p.BranchId == branchId);
            }

            var allItems = await itemsQuery.ToListAsync();

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
                DisplayText = $"[{(string.IsNullOrEmpty(p.Barcode) ? "بدون كود" : p.Barcode)}] " +
                              $"{(string.IsNullOrEmpty(p.Name) ? p.ProductDefinition.Name : p.ProductDefinition.Name + " (" + p.Name + ")")} " +
                              $"- {p.Size}"
            })
            .OrderBy(x => x.DisplayText)
            .ToList();

            ViewData["ProductItemId"] = new SelectList(filteredItems, "Id", "DisplayText");
        }
    }
}