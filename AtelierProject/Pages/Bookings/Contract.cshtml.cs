using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Bookings
{
    [Authorize]
    public class ContractModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ContractModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Booking Booking { get; set; } = default!;
        public string AtelierName { get; set; }
        public string AtelierAddress { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Branch)
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Booking == null) return NotFound();

            // تجهيز بيانات الفرع للعقد
            AtelierName = !string.IsNullOrEmpty(Booking.Branch?.CommercialName)
                ? Booking.Branch.CommercialName
                : (Booking.Branch?.Name ?? "ATELIER PRO");

            AtelierAddress = Booking.Branch?.Address ?? "العنوان الرئيسي";

            return Page();
        }
    }
}