using EasyStayHotel.Models;
using EasyStayHotel.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// กำหนด Culture ภาษาไทย (th-TH) แต่ใช้ปฏิทิน Gregorian (ค.ศ.) ป้องกันปัญหาปี 3112
var defaultCulture = new System.Globalization.CultureInfo("th-TH");
defaultCulture.DateTimeFormat.Calendar = new System.Globalization.GregorianCalendar();
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture);
    options.SupportedCultures = new List<System.Globalization.CultureInfo> { defaultCulture };
    options.SupportedUICultures = new List<System.Globalization.CultureInfo> { defaultCulture };
});

// ลงทะเบียนบริการ MVC Controllers และ Views พร้อมบังคับโหลด Compiled Razor Views เสมอ
builder.Services.AddControllersWithViews()
    .ConfigureApplicationPartManager(manager =>
    {
        var asm = typeof(Program).Assembly;
        var factory = new Microsoft.AspNetCore.Mvc.ApplicationParts.ConsolidatedAssemblyApplicationPartFactory();
        foreach (var part in factory.GetApplicationParts(asm))
        {
            if (!manager.ApplicationParts.Any(p => p.GetType() == part.GetType() && p.Name == part.Name))
            {
                manager.ApplicationParts.Add(part);
            }
        }
    });

// ตั้งค่าระบบ Session สำหรับจำกัดสิทธิ์ผู้ใช้และเก็บข้อมูลล็อกอิน (อายุ 60 นาที)
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// เชื่อมต่อฐานข้อมูล MySQL (easystaydb) ผ่าน Entity Framework Core
builder.Services.AddDbContext<EasystaydbContext>(options =>
    options.UseMySql("server=localhost;port=3306;database=easystaydb;user=root;password=1234;",
    ServerVersion.Parse("8.0.46-mysql")));

builder.Services.AddHttpContextAccessor();

// ลงทะเบียน EmailService สำหรับส่งอีเมลยืนยันการจองและรหัส OTP
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<EasystaydbContext>();

    // เตรียมข้อมูลห้องพักเริ่มต้นทั้ง 3 สาขาและผู้ใช้เริ่มต้นในระบบ
    DatabaseInitializer.EnsureBranchRooms(context);

        DatabaseInitializer.EnsureHousekeeperUser(context);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("ยังไม่สามารถเชื่อมต่อฐานข้อมูล MySQL ได้ในขณะเริ่มต้นระบบ: {Message}", ex.Message);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseRouting();

// เปิดใช้งาน Session และระบบ Routing ใน Middleware Pipeline
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
