using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using System.Linq;


namespace EasyStayHotel.Controllers
{
    public class AdminController : Controller
    {
        private readonly EasystaydbContext _context;

        public AdminController(EasystaydbContext context)
        {
            _context = context;
        }

        // หน้าจัดการผู้ใช้งาน (User Management)
       public IActionResult UserManagement()
        {
            // ดึงรายชื่อผู้ใช้ทั้งหมด "ยกเว้น" ตำแหน่ง SuperAdmin
            var users = _context.Users.Where(u => u.Role != "SuperAdmin").ToList();
            return View(users);
        }

        // ฟังก์ชันนี้จะทำงาน "ก่อน" ที่ทุกหน้าใน Admin จะถูกเปิด
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("Role");
        
        // ถ้าไม่ใช่ทั้ง Admin และไม่ใช่ทั้ง SuperAdmin ให้เตะกลับไปหน้า Login
        if (string.IsNullOrEmpty(role) || (role != "Admin" && role != "SuperAdmin"))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
        
        base.OnActionExecuting(context);
    }
        // สร้าง Action เปล่าๆ ไว้รองรับเมนู Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        //AddUser(Admin,Staff,Accounting)

        public IActionResult AddUser()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User newUser)
        {
            if(ModelState.IsValid)
            {
                bool isEmailExist = _context.Users.Any(u => u.Email == newUser.Email);
                if(isEmailExist)
                {
                    ViewBag.ErrorMessage = "อีเมลนี้ถูกใช้งานไปแล้ว กรุณาใช้เมลอื่น";
                    return View(newUser);
                }
                _context.Users.Add(newUser);
                _context.SaveChanges();

                return RedirectToAction("UserManagement");
            }
            return View(newUser);
        }
    }
}