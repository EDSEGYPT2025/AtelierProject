using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // هام
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Definitions
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 1. إضافة المانجر

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ProductDefinition ProductDefinition { get; set; } = default!;
        public List<ProductItem> ProductItems { get; set; } = new List<ProductItem>();

        [BindProperty]
        public ProductItem NewItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // 2. معرفة فرع الموظف الحالي
            var user = await _userManager.GetUserAsync(User);
            int? branchId = user?.BranchId;

            // جلب الموديل
            var product = await _context.ProductDefinitions
                .Include(p => p.ProductItems) // جلب كل النسخ مبدئياً
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            ProductDefinition = product;

            // 3. فلترة النسخ: عرض النسخ الموجودة في فرع الموظف فقط
            // إذا كان أدمن (بدون فرع) يرى الكل، وإلا يرى فرعه فقط
            var itemsQuery = product.ProductItems.AsQueryable();

            if (branchId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i => i.BranchId == branchId);
            }

            ProductItems = itemsQuery.OrderBy(i => i.Size).ToList();

            return Page();
        }

        // دالة لإضافة نسخة جديدة
        public async Task<IActionResult> OnPostAddItemAsync(int definitionId)
        {
            // 4. جلب الموظف لتحديد الفرع
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            NewItem.ProductDefinitionId = definitionId;
            NewItem.Status = ItemStatus.Available;

            // 🔥 تسجيل الفرع تلقائياً 🔥
            NewItem.BranchId = user.BranchId;

            if (string.IsNullOrWhiteSpace(NewItem.Barcode))
            {
                NewItem.Barcode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }

            _context.ProductItems.Add(NewItem);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = definitionId });
        }

        public async Task<IActionResult> OnPostDeleteItemAsync(int itemId, int definitionId)
        {
            var item = await _context.ProductItems.FindAsync(itemId);
            // حماية إضافية: التأكد أن الموظف يحذف قطعة من فرعه هو فقط
            var user = await _userManager.GetUserAsync(User);

            if (item != null)
            {
                // إذا كان للموظف فرع، والقطعة في فرع آخر، نمنع الحذف
                if (user.BranchId.HasValue && item.BranchId != user.BranchId)
                {
                    return Forbid(); // غير مسموح
                }

                _context.ProductItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = definitionId });
        }
    }
}