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

// --- إعدادات الهوية ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- إعدادات الأمان (الإيقاف اللحظي) ---
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// --- إعدادات الكوكيز ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------- إعدادات العملة واللغة ----------------
var defaultCulture = new CultureInfo("ar-EG");
defaultCulture.NumberFormat.CurrencySymbol = "ج.م";

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);
// -------------------------------------------------------

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
app.UseStaticFiles(); // ✅ (تعديل بسيط: UseStaticFiles أفضل من MapStaticAssets في بعض نسخ السيرفرات، كلاهما يعمل)

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// ==============================================================================
// ✅✅✅ منطقة التشغيل التلقائي (Migration + Admin Seeding) ✅✅✅
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. جلب الخدمات اللازمة
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 2. 🔥 أهم سطر للنشر: تطبيق التحديثات وإنشاء الجداول تلقائياً 🔥
        // هذا السطر سينشئ قاعدة البيانات والجداول إذا لم تكن موجودة على السيرفر
        context.Database.Migrate();

        // 3. إنشاء مستخدم الأدمن الافتراضي
        string email = "admin@admin.com";
        string password = "Oe@123456"; // ⚠️ يفضل تغيير الباسورد فور الدخول

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "مدير النظام",
                BranchId = null // مدير عام
            };

            var result = await userManager.CreateAsync(user, password);

            // (اختياري) طباعة أخطاء إنشاء المستخدم في الـ Console للمراجعة
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($">>> Error creating User: {error.Description}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        // تسجيل الأخطاء في حال فشل الاتصال بقاعدة البيانات
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, ">>> حدث خطأ أثناء تهيئة قاعدة البيانات (Migrations/Seeding).");
    }
}
// ==============================================================================

app.Run();