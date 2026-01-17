using AtelierProject.Data;
using AtelierProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides; // 👈 1. إضافة مهمة
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            // تفعيل إعادة المحاولة عند فشل الاتصال المؤقت
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // زيادة وقت انتظار تنفيذ الأوامر
            sqlServerOptions.CommandTimeout(60);
        }
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- إعدادات الهوية ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // (اختياري) تخفيف قيود الباسورد قليلاً لو حابب
    // options.Password.RequireNonAlphanumeric = false; 
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

// ✅ 2. إعدادات الـ Headers للسيرفر (ضروري عشان الـ VPS وكلاود فلير)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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

// ✅ 3. تفعيل تمرير الهيدرز (يجب أن يكون في أول البايب لاين)
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

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
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // تطبيق التحديثات وإنشاء الجداول تلقائياً
        context.Database.Migrate();

        // إنشاء مستخدم الأدمن الافتراضي
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
                FullName = "مدير النظام",
                BranchId = null // مدير عام
            };

            var result = await userManager.CreateAsync(user, password);
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
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, ">>> حدث خطأ أثناء تهيئة قاعدة البيانات (Migrations/Seeding).");
    }
}
// ==============================================================================

app.Run();