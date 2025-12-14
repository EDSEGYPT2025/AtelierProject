using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 1. تعديل خدمة Identity لدعم الأدوار (Roles) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✅✅✅ هذا هو الجزء الجديد والمهم جداً للإيقاف اللحظي ✅✅✅
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // جعل الفاصل الزمني صفر يجبر النظام على التحقق من "بصمة الأمان" مع كل طلب
    // بمجرد تغيير البصمة في صفحة التعديل، سيتم طرد المستخدم فوراً
    options.ValidationInterval = TimeSpan.Zero;
});
// -------------------------------------------------------------

// --- 2. إعدادات الكوكيز ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------- بداية كود العملة ----------------
var defaultCulture = new CultureInfo("ar-EG");
defaultCulture.NumberFormat.CurrencySymbol = "ج.م";

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);
// ---------------- نهاية كود العملة ----------------

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// ---------------- بداية كود إنشاء الأدمن التلقائي ----------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        string email = "admin@admin.com";
        string password = "Oe@123456";

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "مدير النظام"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($">>> Error creating user: {error.Description}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error seeding database: {ex.Message}");
    }
}
// ---------------- نهاية كود إنشاء الأدمن التلقائي ----------------

app.Run();