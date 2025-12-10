using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Clients
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Client> Clients { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } // متغير البحث

        public async Task OnGetAsync()
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(c => c.Name.Contains(SearchTerm) || c.Phone.Contains(SearchTerm));
            }

            // ترتيب حسب الأحدث
            Clients = await query.OrderByDescending(c => c.RegistrationDate).ToListAsync();
        }
    }
}