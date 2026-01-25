using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Clients
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        // 1. جلب بيانات العميل عند فتح الصفحة
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            Client = client;
            return Page();
        }

        // 2. حفظ التعديلات
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // --- التحقق من التكرار (Logic) ---

            // أ) التحقق من رقم الهاتف (مع استثناء العميل الحالي من الفحص)
            // الشرط: (الرقم موجود) AND (ID العميل الذي يملك الرقم != ID العميل الحالي)
            bool phoneExists = await _context.Clients.AnyAsync(c => c.Phone == Client.Phone && c.Id != Client.Id);
            if (phoneExists)
            {
                ModelState.AddModelError("Client.Phone", "رقم الهاتف هذا مسجل باسم عميل آخر.");
                return Page();
            }

            // ب) التحقق من الرقم القومي (إن وجد)
            if (!string.IsNullOrEmpty(Client.NationalId))
            {
                bool nidExists = await _context.Clients.AnyAsync(c => c.NationalId == Client.NationalId && c.Id != Client.Id);
                if (nidExists)
                {
                    ModelState.AddModelError("Client.NationalId", "الرقم القومي هذا مسجل باسم عميل آخر.");
                    return Page();
                }
            }

            // --- عملية التحديث الآمنة ---
            // نستخدم هذا الأسلوب للحفاظ على تاريخ التسجيل الأصلي وعدم تغييره
            var clientToUpdate = await _context.Clients.FindAsync(Client.Id);

            if (clientToUpdate == null)
            {
                return NotFound();
            }

            // تحديث الحقول المسموح بتغييرها فقط
            clientToUpdate.Name = Client.Name;
            clientToUpdate.Phone = Client.Phone;
            clientToUpdate.Address = Client.Address;
            clientToUpdate.NationalId = Client.NationalId;
            clientToUpdate.Notes = Client.Notes;
            clientToUpdate.IsBlacklisted = Client.IsBlacklisted;

            // ملاحظة: لم نقم بتحديث RegistrationDate لنحافظ على تاريخ الانضمام الأصلي

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(Client.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}