using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AtelierProject.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ApplicationUser> Users { get; set; }

        public async Task OnGetAsync()
        {
            // جلب المستخدمين مع بيانات الفرع المرتبط بهم
            Users = await _context.Users
                .Include(u => u.Branch)
                .ToListAsync();
        }
    }
}