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
            // ดึงรายชื่อผู้ใช้ทั้งหมดจากตาราง Users
            var users = _context.Users.ToList();
            return View(users);
        }

        // สร้าง Action เปล่าๆ ไว้รองรับเมนู Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}