using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // จำเป็นสำหรับใช้งาน Session
using EasyStayHotel.Models;
using System.Linq;

namespace EasyStayHotel.Controllers
{
    public class AccountController : Controller
    {
        private readonly EasystaydbContext _context;

        public AccountController(EasystaydbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ระบบเข้าสู่ระบบ (Login)
        // ==========================================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password) // รับค่าเป็น email
        {
            // ค้นหาผู้ใช้ที่ Email และ Password ตรงกัน
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            
            if (user != null)
            {
                // บันทึกข้อมูลลง Session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role ?? "Customer");

                // แยกทางไปตาม Role (ตำแหน่ง)
                if (user.Role == "SuperAdmin" || user.Role == "Admin") return RedirectToAction("UserManagement", "Admin");
                if (user.Role == "Staff") return RedirectToAction("BookingList", "Staff");
                if (user.Role == "Accounting") return RedirectToAction("RevenueReport", "Accounting");
                
                // ถ้าเป็นลูกค้าระบบ (Customer) ให้ไปหน้าแรก
                return RedirectToAction("Index", "Home");
            }
            
            // ถ้าล็อกอินไม่ผ่าน
            ViewBag.Error = "อีเมลหรือรหัสผ่านไม่ถูกต้อง";
            return View();
        }

        // ==========================================
        // 2. ระบบสมัครสมาชิกสำหรับลูกค้า (Register)
        // ==========================================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                // เช็คว่ามีอีเมลนี้ในระบบหรือยัง
                if (_context.Users.Any(u => u.Email == newUser.Email))
                {
                    ViewBag.Error = "อีเมลนี้ถูกใช้งานแล้ว กรุณาใช้อีเมลอื่น";
                    return View(newUser);
                }

                // บังคับให้คนที่สมัครผ่านหน้าเว็บ เป็น Role "Customer" เท่านั้น
                newUser.Role = "Customer";
                
                _context.Users.Add(newUser);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(newUser);
        }

        // ==========================================
        // 3. ระบบออกจากระบบ (Logout)
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // ล้างข้อมูล Session ทั้งหมด
            return RedirectToAction("Login");
        }
    }
}