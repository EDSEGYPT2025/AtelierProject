using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using AtelierProject.Data;
using AtelierProject.Models;

namespace AtelierProject.Pages.Branches
{
    public class CreateModel : PageModel
    {
        private readonly AtelierProject.Data.ApplicationDbContext _context;

        public CreateModel(AtelierProject.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Branch Branch { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Branches.Add(Branch);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
