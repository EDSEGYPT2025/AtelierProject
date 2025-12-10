using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity; // هام
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AtelierProject.Pages.Definitions
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<ProductDefinition> ProductDefinitions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            var query = _context.ProductDefinitions.AsQueryable();

            // تطبيق فلتر الأقسام بناءً على صلاحيات المستخدم
            if (user != null)
            {
                // إذا كان يملك صلاحية رجالي وحريمي (أدمن مثلاً)، يرى الكل.
                // أما لو كان محدد الصلاحية، نفلتر:

                if (user.CanAccessMenSection && !user.CanAccessWomenSection)
                {
                    // يرى الرجالي فقط
                    query = query.Where(p => p.Department == DepartmentType.Men);
                }
                else if (user.CanAccessWomenSection && !user.CanAccessMenSection)
                {
                    // يرى الحريمي فقط
                    query = query.Where(p => p.Department == DepartmentType.Women);
                }

                // ملاحظة: البيوتي سنتر ليس له "ملابس"، لذا قد لا يرى هذه الصفحة أو يراها فارغة
            }

            ProductDefinitions = await query.ToListAsync();
        }

        // دالة الحذف (يفضل إضافة حماية هنا أيضاً)
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var product = await _context.ProductDefinitions.FindAsync(id);
            if (product != null)
            {
                _context.ProductDefinitions.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}