using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Clients
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. التحقق من تكرار رقم الهاتف (Rule هام جداً)
            if (_context.Clients.Any(c => c.Phone == Client.Phone))
            {
                ModelState.AddModelError("Client.Phone", "رقم الهاتف هذا مسجل لعميل آخر بالفعل.");
                return Page();
            }

            // 2. التحقق من تكرار الرقم القومي (إذا تم إدخاله)
            if (!string.IsNullOrEmpty(Client.NationalId) && _context.Clients.Any(c => c.NationalId == Client.NationalId))
            {
                ModelState.AddModelError("Client.NationalId", "الرقم القومي هذا مسجل لعميل آخر.");
                return Page();
            }

            // 3. ضبط تاريخ التسجيل
            Client.RegistrationDate = DateTime.Now;

            // 4. الحفظ
            _context.Clients.Add(Client);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}