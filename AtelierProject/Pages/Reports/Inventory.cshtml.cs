using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AtelierProject.Pages.Reports
{
    [Authorize]
    public class InventoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // فلاتر البحث
        [BindProperty(SupportsGet = true)]
        public int? BranchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DepartmentType? Department { get; set; }

        public bool IsAdmin { get; set; }

        // البيانات التي سنعرضها: قائمة الموديلات وبداخلها القطع
        public IList<ProductDefinition> InventoryGroups { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // 1. منطق الصلاحيات (نفس التقرير المالي)
            if (user.BranchId != null)
            {
                BranchId = user.BranchId;
                IsAdmin = false;
            }
            else
            {
                IsAdmin = true;
                ViewData["BranchList"] = new SelectList(_context.Branches, "Id", "Name");
            }

            // 2. بناء الاستعلام
            var query = _context.ProductDefinitions
                .Include(d => d.ProductItems) // جلب القطع
                .ThenInclude(i => i.Branch)   // جلب اسم الفرع لكل قطعة
                .AsQueryable();

            // فلترة بالقسم (رجالي / حريمي)
            if (Department.HasValue)
            {
                query = query.Where(d => d.Department == Department);
            }

            // تنفيذ الاستعلام
            var allDefinitions = await query.OrderBy(d => d.Name).ToListAsync();

            // 3. فلترة القطع داخل كل موديل حسب الفرع المختار
            // (EF Core loads related data, we filter in memory for complex nested logic or use Filtered Include in .NET 5+)

            foreach (var def in allDefinitions)
            {
                if (BranchId.HasValue)
                {
                    // لو تم اختيار فرع، نحتفظ فقط بالقطع الموجودة في هذا الفرع
                    def.ProductItems = def.ProductItems.Where(i => i.BranchId == BranchId).ToList();
                }
            }

            // حذف الموديلات التي أصبحت فارغة بعد الفلترة (لا داعي لطباعتها)
            InventoryGroups = allDefinitions.Where(d => d.ProductItems.Any()).ToList();

            return Page();
        }
    }
}