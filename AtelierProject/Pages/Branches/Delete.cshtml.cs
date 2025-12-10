using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Branches
{
    public class DeleteModel : PageModel
    {
        private readonly AtelierProject.Data.ApplicationDbContext _context;

        public DeleteModel(AtelierProject.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Branch Branch { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches.FirstOrDefaultAsync(m => m.Id == id);

            if (branch is not null)
            {
                Branch = branch;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                Branch = branch;
                _context.Branches.Remove(Branch);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
