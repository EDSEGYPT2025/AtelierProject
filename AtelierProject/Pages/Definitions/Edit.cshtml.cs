using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Definitions
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProductDefinition ProductDefinition { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var productdefinition = await _context.ProductDefinitions.FirstOrDefaultAsync(m => m.Id == id);

            if (productdefinition == null) return NotFound();

            ProductDefinition = productdefinition;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // إخبار قاعدة البيانات أن هذا العنصر تم تعديله
            _context.Attach(ProductDefinition).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductDefinitionExists(ProductDefinition.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool ProductDefinitionExists(int id)
        {
            return _context.ProductDefinitions.Any(e => e.Id == id);
        }
    }
}