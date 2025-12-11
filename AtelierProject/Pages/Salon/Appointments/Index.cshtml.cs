using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // هام
using Microsoft.AspNetCore.Authorization; // هام
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Appointments
{
    [Authorize] // حماية الصفحة
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 1. تعريف المانجر

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<SalonAppointment> Appointments { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        public async Task OnGetAsync()
        {
            // 2. جلب المستخدم الحالي
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            // 3. الاستعلام الأساسي
            var query = _context.SalonAppointments
                .Include(a => a.Client)
                .AsQueryable();

            // 🛑 4. الفلترة حسب الفرع (الخطوة الأهم)
            if (user.BranchId.HasValue)
            {
                query = query.Where(a => a.BranchId == user.BranchId);
            }

            // فلتر البحث
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(a => a.Client.Name.Contains(SearchString) || a.Client.Phone.Contains(SearchString));
            }

            // فلتر التاريخ
            if (FilterDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == FilterDate.Value.Date);
            }

            // الترتيب
            query = query.OrderByDescending(a => a.AppointmentDate);

            Appointments = await query.ToListAsync();
        }
    }
}