using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Clients
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Client Client { get; set; } = default!;

        // قوائم السجلات
        public List<Booking> ClientBookings { get; set; } = new List<Booking>();
        public List<SalonAppointment> ClientAppointments { get; set; } = new List<SalonAppointment>();

        // ملخص مالي
        public decimal TotalDebt { get; set; }
        public decimal TotalPaid { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // 1. جلب بيانات العميل
            Client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);
            if (Client == null) return NotFound();

            // 2. جلب حجوزات الأتيليه (مع التفاصيل لحساب الأسعار)
            ClientBookings = await _context.Bookings
                .Include(b => b.BookingItems)
                    .ThenInclude(bi => bi.ProductItem)
                        .ThenInclude(pi => pi.ProductDefinition)
                .Where(b => b.ClientId == id)
                .OrderByDescending(b => b.PickupDate) // الأحدث أولاً
                .ToListAsync();

            // 3. جلب مواعيد الصالون (إذا كان الموديول مفعل)
            ClientAppointments = await _context.SalonAppointments
                .Include(s => s.Items)
                    .ThenInclude(i => i.SalonService)
                .Where(s => s.ClientId == id)
                .OrderByDescending(s => s.AppointmentDate)
                .ToListAsync();

            // 4. حساب الملخص المالي (أتيليه + صالون)

            // ديون الأتيليه
            decimal bookingsDebt = ClientBookings.Sum(b => b.RemainingRentalAmount);
            // ديون الصالون
            decimal salonDebt = ClientAppointments.Sum(a => a.RemainingAmount);

            TotalDebt = bookingsDebt + salonDebt;

            // المدفوعات
            TotalPaid = ClientBookings.Sum(b => b.PaidAmount) +
                        ClientAppointments.Sum(a => a.PaidAmount);

            return Page();
        }
    }
}