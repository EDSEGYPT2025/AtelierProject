using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace AtelierProject.Pages.Reports
{
    [Authorize]
    public class SafeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SafeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- الفلاتر ---
        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public DepartmentType? SelectedDepartment { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedBranchId { get; set; }

        // ✅ إضافة فلتر المستخدم
        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }

        // --- البيانات المعروضة ---
        public List<SafeTransaction> Transactions { get; set; } = new List<SafeTransaction>();

        // ✅ قاموس لتخزين أسماء المستخدمين (Key = UserId, Value = FullName)
        public Dictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();

        public decimal PeriodRevenue { get; set; }
        public decimal PeriodExpense { get; set; }
        public decimal PeriodInsuranceIn { get; set; }
        public decimal PeriodInsuranceOut { get; set; }
        public decimal PeriodNetFlow { get; set; }

        public decimal TotalCashBalance { get; set; }
        public decimal TotalInsuranceHeld { get; set; }

        public bool IsGeneralManager { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // 1. جلب قائمة المستخدمين لملء القائمة المنسدلة وعرض الأسماء
            var allUsers = await _userManager.Users.ToListAsync();

            // تعبئة القاموس لاستخدامه في الجدول لاحقاً
            UserNames = allUsers.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName);

            // إعداد قائمة الفلتر
            ViewData["UserId"] = new SelectList(allUsers.Select(u => new { Id = u.Id, Name = u.FullName ?? u.UserName }), "Id", "Name");


            // 2. تحديد الصلاحيات والفرع
            if (user.BranchId == null)
            {
                IsGeneralManager = true;
                ViewData["BranchId"] = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            }
            else
            {
                IsGeneralManager = false;
                SelectedBranchId = user.BranchId;
            }

            // 3. الاستعلام الأساسي
            var query = _context.SafeTransactions
                .Include(t => t.Branch)
                .AsQueryable();

            if (SelectedBranchId.HasValue)
            {
                query = query.Where(t => t.BranchId == SelectedBranchId.Value);
            }

            if (SelectedDepartment.HasValue)
            {
                query = query.Where(t => t.Department == SelectedDepartment.Value);
            }

            // ✅ تطبيق فلتر المستخدم
            if (!string.IsNullOrEmpty(SelectedUserId))
            {
                query = query.Where(t => t.CreatedByUserId == SelectedUserId);
            }

            // 4. الحسابات
            var allTimeTransactions = await query.ToListAsync();

            TotalCashBalance = allTimeTransactions.Where(t => t.Type == TransactionType.Income || t.Type == TransactionType.InsuranceIn).Sum(t => t.Amount)
                             - allTimeTransactions.Where(t => t.Type == TransactionType.Expense || t.Type == TransactionType.InsuranceOut).Sum(t => t.Amount);

            TotalInsuranceHeld = allTimeTransactions.Where(t => t.Type == TransactionType.InsuranceIn).Sum(t => t.Amount)
                               - allTimeTransactions.Where(t => t.Type == TransactionType.InsuranceOut).Sum(t => t.Amount);


            var from = FromDate.Date;
            var to = ToDate.Date.AddDays(1).AddTicks(-1);

            Transactions = allTimeTransactions
                .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            PeriodRevenue = Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            PeriodExpense = Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            PeriodInsuranceIn = Transactions.Where(t => t.Type == TransactionType.InsuranceIn).Sum(t => t.Amount);
            PeriodInsuranceOut = Transactions.Where(t => t.Type == TransactionType.InsuranceOut).Sum(t => t.Amount);
            PeriodNetFlow = (PeriodRevenue + PeriodInsuranceIn) - (PeriodExpense + PeriodInsuranceOut);

            return Page();
        }
    }
}