using EasyStayHotel.Models;
using EasyStayHotel.Services;
using Microsoft.EntityFrameworkCore;

// =================================================================================================
// ไฟล์: Program.cs
// หน้าที่: จุดเริ่มต้นการทำงานของระบบ (Entry Point) และการตั้งค่า Configuration ทั้งหมดของ ASP.NET Core
// คำถามที่อาจารย์มักถาม:
// 1. Dependency Injection (DI) คืออะไร และลงทะเบียนอย่างไร?
//    -> คือการส่ง Object หรือ Service ที่คลาสต้องการผ่าน Constructor โดยลงทะเบียนผ่าน builder.Services
// 2. AddControllersWithViews(), AddSession(), AddDbContext() มีหน้าที่อะไร?
// 3. ทำไม app.UseSession() ต้องอยู่ก่อน app.UseAuthorization()?
//    -> เพื่อให้ระบบโหลดข้อมูล Session ของผู้ใช้ขึ้นมาก่อนที่จะทำการตรวจสอบสิทธิ์การเข้าถึง
// =================================================================================================

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------------------------------
// 1. การลงทะเบียนบริการ MVC (Model-View-Controller)
// -------------------------------------------------------------------------------------------------
// ลงทะเบียน Controllers พร้อมกับ Razor Views เพื่อรองรับการทำงานแบบ Web Application เต็มรูปแบบ
builder.Services.AddControllersWithViews();

// -------------------------------------------------------------------------------------------------
// 2. การลงทะเบียนและตั้งค่า Session (ระบบจัดเก็บข้อมูลเซสชันของผู้ใช้)
// -------------------------------------------------------------------------------------------------
// ใช้สำหรับจดจำสถานะการเข้าสู่ระบบของผู้ใช้ (UserId, Role, Branch) ระหว่างการใช้งาน
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60); // เซสชันหมดอายุอัตโนมัติหากไม่มีการใช้งานต่อเนื่องเกิน 60 นาที
    options.Cookie.HttpOnly = true;                 // ป้องกันการเข้าถึงคุกกี้เซสชันผ่านทาง Client-side JavaScript (ป้องกัน XSS)
    options.Cookie.IsEssential = true;             // บังคับใช้งานคุกกี้นี้แม้ผู้ใช้จะไม่ได้เปิดรับคุกกี้ทั่วไป (จำเป็นต่อการทำงานของระบบ)
});

// -------------------------------------------------------------------------------------------------
// 3. การตั้งค่าการเชื่อมต่อฐานข้อมูล MySQL ด้วย Entity Framework Core (EF Core)
// -------------------------------------------------------------------------------------------------
// ใช้ Pomelo.EntityFrameworkCore.MySql เชื่อมต่อไปยังฐานข้อมูล easystaydb ที่พอร์ต 3306
// ServerVersion.AutoDetect จะตรวจสอบเวอร์ชันของ MySQL Server อัตโนมัติ เพื่อปรับคำสั่ง SQL ให้เหมาะสม
builder.Services.AddDbContext<EasystaydbContext>(options =>
    options.UseMySql("server=localhost;port=3306;database=easystaydb;user=root;password=1234;",
    ServerVersion.AutoDetect("server=localhost;port=3306;database=easystaydb;user=root;password=1234;")));

// -------------------------------------------------------------------------------------------------
// 4. การลงทะเบียน IHttpContextAccessor
// -------------------------------------------------------------------------------------------------
// ช่วยให้สามารถเข้าถึง HttpContext (เช่น Session, Request, User) ได้จากหน้า Razor View หรือ Service อื่นๆ
builder.Services.AddHttpContextAccessor();

// -------------------------------------------------------------------------------------------------
// 5. การลงทะเบียน Dependency Injection สำหรับ Service ภายในระบบ
// -------------------------------------------------------------------------------------------------
// AddScoped: สร้าง Instance ใหม่ 1 ครั้งต่อ 1 HTTP Request (เหมาะสำหรับ Service ที่ทำงานกับข้อมูลคำขอของผู้ใช้)
builder.Services.AddScoped<IEmailService, EmailService>();

// สร้าง WebApplication Object ตามการตั้งค่าที่กำหนดไว้
var app = builder.Build();

// -------------------------------------------------------------------------------------------------
// 6. การตรวจสอบและเตรียมความพร้อมของฐานข้อมูลเบื้องต้น (Database Seeding)
// -------------------------------------------------------------------------------------------------
// CreateScope: สร้าง Scope ชั่วคราวเพื่อดึง DbContext ออกมาใช้งานก่อนที่เซิร์ฟเวอร์จะเปิดรับคำขอ
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EasystaydbContext>();
    
    // ตรวจสอบและสร้างห้องพักจำลองสถานะว่างสำหรับสาขา ชลบุรี และภูเก็ต หากยังไม่มี
    DatabaseInitializer.EnsureBranchRooms(context);
    
    // ตรวจสอบและสร้างบัญชีผู้ดูแลระบบ (Admin) และพนักงานแม่บ้าน (Housekeeper) เริ่มต้น
    DatabaseInitializer.EnsureHousekeeperUser(context);
}

// -------------------------------------------------------------------------------------------------
// 7. การกำหนด HTTP Request Pipeline (Middleware)
// -------------------------------------------------------------------------------------------------
// ตรวจสอบสภาพแวดล้อม (Environment)
if (!app.Environment.IsDevelopment())
{
    // เมื่อทำงานบน Production: นำทางไปยังหน้าแจ้งข้อผิดพลาดแบบสวยงาม และเปิดใช้ HSTS (ความปลอดภัย HTTPS)
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// บังคับเปลี่ยนเส้นทางคำขอเป็น HTTPS เพื่อความปลอดภัย
app.UseHttpsRedirection();

// เปิดใช้งานระบบ Routing เพื่อจับคู่ URL กับ Controller และ Action
app.UseRouting();

// ข้อควรระวัง: UseSession ต้องถูกเรียก "ก่อน" UseAuthorization เสมอ
// เพื่อให้ข้อมูลสถานะผู้ใช้และสิทธิ์ใน Session พร้อมใช้งานก่อนที่ระบบตรวจสอบสิทธิ์จะทำงาน
app.UseSession(); 
app.UseAuthorization();

// ให้บริการ Static Files (CSS, JavaScript, รูปภาพ) จากโฟลเดอร์ wwwroot
app.MapStaticAssets();

// -------------------------------------------------------------------------------------------------
// 8. การกำหนดเส้นทางเริ่มต้นของระบบ (Default Route)
// -------------------------------------------------------------------------------------------------
// หากผู้ใช้ไม่ได้ระบุ URL ระบบจะเรียกใช้ HomeController -> Index Action
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// เริ่มต้นรันเว็บแอปพลิเคชัน
app.Run();
