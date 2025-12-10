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
    public class IndexModel : PageModel
    {
        private readonly AtelierProject.Data.ApplicationDbContext _context;

        public IndexModel(AtelierProject.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Branch> Branch { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Branch = await _context.Branches.ToListAsync();
        }
    }
}
