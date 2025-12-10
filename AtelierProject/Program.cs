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

// --- 1. تعديل خدمة Identity لدعم الأدوار (Roles) وإلغاء تأكيد الإيميل للتسهيل حالياً ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // جعلناها false للتجربة السريعة
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- 2. إعدادات الكوكيز (لحل مشكلة التوجيه الخاطئ) ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";        // مسار صفحة الدخول الجديدة
    options.LogoutPath = "/Account/Logout";      // مسار الخروج
    options.AccessDeniedPath = "/Account/AccessDenied"; // مسار عدم الصلاحية
});

builder.Services.AddRazorPages();

var app = builder.Build();

// ---------------- بداية كود العملة ----------------
// إجبار التطبيق على استخدام الثقافة المصرية (اللغة العربية - مصر)
var defaultCulture = new CultureInfo("ar-EG");

// اختياري: إذا أردت التأكد من شكل العملة بدقة (مثلاً "ج.م" بدلاً من EGP)
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

// الترتيب هنا مهم جداً: Authentication ثم Authorization
app.UseAuthentication(); // <-- تأكد من وجود هذا السطر (مهم جداً لعمل الـ Identity)
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

        // الإيميل وكلمة المرور للمستخدم الافتراضي
        string email = "admin@admin.com";
        string password = "Oe@123456"; // كلمة المرور يجب أن تكون قوية (حرف كبير، رقم، رمز)

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // تفعيل الإيميل مباشرة
                FullName = "مدير النظام" // تأكد أن هذا الحقل موجود في المودل الخاص بك
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                // طباعة الأخطاء في الكونسول إذا فشل الإنشاء
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