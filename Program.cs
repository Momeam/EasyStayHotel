using EasyStayHotel.Models;
using EasyStayHotel.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. เพิ่มบริการ MVC
builder.Services.AddControllersWithViews();

// 2. เพิ่มบริการ Session
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60); // ล็อกอินอยู่ได้ 60 นาที
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. ตั้งค่าการเชื่อมต่อฐานข้อมูล MySQL
builder.Services.AddDbContext<EasystaydbContext>(options =>
    options.UseMySql("server=localhost;port=3306;database=easystaydb;user=root;password=1234;",
    ServerVersion.AutoDetect("server=localhost;port=3306;database=easystaydb;user=root;password=1234;")));

// 4. เพิ่มการเรียกใช้ HttpContextAccessor เพื่อให้เข้าถึง Session จากหน้า View ได้
builder.Services.AddHttpContextAccessor();

// 5. เพิ่มบริการส่งอีเมลและยืนยัน OTP
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// ตรวจสอบและเตรียมห้องพักสถานะว่างสำหรับสาขา และเตรียมผู้ใช้แม่บ้าน
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EasystaydbContext>();
    DatabaseInitializer.EnsureBranchRooms(context);
    DatabaseInitializer.EnsureHousekeeperUser(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// UseSession ต้องอยู่ก่อน UseAuthorization เสมอ
app.UseSession(); 
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();