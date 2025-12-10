using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Salon.Services
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SalonService> Services { get; set; } = default!;

        [BindProperty]
        public SalonService NewService { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Services = await _context.SalonServices.ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.SalonServices.Add(NewService);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // دالة لحذف الخدمة
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.SalonServices.FindAsync(id);
            if (service != null)
            {
                _context.SalonServices.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}