using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SalonAppointment> Appointments { get; set; } = default!;

        // خصائص البحث والفلترة
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        public async Task OnGetAsync()
        {
            // استعلام أساسي مع تضمين بيانات العميل
            var query = _context.SalonAppointments
                .Include(a => a.Client)
                .AsQueryable();

            // 1. فلتر البحث (اسم العميل أو الهاتف)
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(a => a.Client.Name.Contains(SearchString)
                                      || a.Client.Phone.Contains(SearchString));
            }

            // 2. فلتر التاريخ (إذا تم تحديده)
            if (FilterDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == FilterDate.Value.Date);
            }

            // 3. الترتيب: الأحدث تاريخاً في الأعلى (أو يمكن عكسها للأقرب فالأبعد)
            query = query.OrderByDescending(a => a.AppointmentDate);

            Appointments = await query.ToListAsync();
        }
    }
}