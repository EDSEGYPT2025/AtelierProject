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

        [BindProperty]
        public int SelectedItemId { get; set; }

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

            if (Booking.ReturnDate <= Booking.PickupDate)
            {
                ModelState.AddModelError("Booking.ReturnDate", "تاريخ الإرجاع يجب أن يكون بعد تاريخ الاستلام.");
                await LoadSelectListsAsync();
                return Page();
            }

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

            Booking.BranchId = currentUser.BranchId;
            Booking.Status = BookingStatus.New;

            if (Booking.BookingItems == null)
            {
                Booking.BookingItems = new List<BookingItem>();
            }

            var bookingItem = new BookingItem
            {
                ProductItemId = SelectedItemId,
                RentalPrice = Booking.TotalAmount
            };
            Booking.BookingItems.Add(bookingItem);

            _context.Bookings.Add(Booking);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

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
                // --- التعديل هنا: تحسين تنسيق النص للبحث ---
                DisplayText = $"[{(string.IsNullOrEmpty(p.Barcode) ? "بدون كود" : p.Barcode)}] " +
                              $"{(string.IsNullOrEmpty(p.Name) ? p.ProductDefinition.Name : p.ProductDefinition.Name + " (" + p.Name + ")")} " +
                              $"- {p.Size}"
            })
            .OrderBy(x => x.DisplayText) // ترتيب أبجدي ليسهل العثور عليها
            .ToList();

            ViewData["ProductItemId"] = new SelectList(filteredItems, "Id", "DisplayText");
        }
    }
}