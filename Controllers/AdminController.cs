using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;
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


        // เพิ่มพารามิเตอร์สำหรับรับค่าจากช่องค้นหาและเลขหน้า
        public async Task<IActionResult> UserManagement(string searchString, int? pageNumber)
        {
            int pageSize = 10;
            int currentPage = pageNumber ?? 1;

            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // ค้นหาจาก ชื่อ, อีเมล หรือ บทบาท
                users = users.Where(u => u.FullName.Contains(searchString)
                                      || u.Email.Contains(searchString)
                                      || u.Role.Contains(searchString));
            }

            int totalRecords = await users.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // เรียงลำดับด้วย UserId
            var pagedUsers = await users
                .OrderBy(u => u.UserId)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchString = searchString;

            return View(pagedUsers);
        }
        // หน้าจัดการผู้ใช้งาน (User Management)

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
      
       // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // 1. นับจำนวนผู้ใช้งานทั้งหมด
            int totalUsers = await _context.Users.CountAsync();
            
            // 2. นับจำนวนห้องพักทั้งหมด
            int totalRooms = await _context.Rooms.CountAsync();
            
            // 3. นับจำนวนห้องพักที่ว่างอยู่ (Status == "Available")
            int availableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available");

            // 4. นับจำนวนการจองทั้งหมดในระบบ
            int totalBookings = await _context.Bookings.CountAsync();

            // ส่งข้อมูลไปยัง View ผ่าน ViewBag
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.TotalBookings = totalBookings;

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
            if (ModelState.IsValid)
            {
                bool isEmailExist = _context.Users.Any(u => u.Email == newUser.Email);
                if (isEmailExist)
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
        // ==========================================
        // ระบบ: ลบผู้ใช้งาน (Delete User)
        // ==========================================
        public IActionResult DeleteUser(int id)
        {
            var userToDelete = _context.Users.Find(id);
            if (userToDelete != null)
            {
                // ดึง Role ของคนที่กำลังกดลบ จาก Session
                var currentUserRole = HttpContext.Session.GetString("Role");

                // กฎข้อ 1: ห้ามลบ SuperAdmin เด็ดขาด
                if (userToDelete.Role == "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "ไม่อนุญาตให้ลบบัญชี Super Admin ได้";
                    return RedirectToAction("UserManagement");
                }

                // กฎข้อ 2: Admin ลบ Admin ด้วยกันเองไม่ได้ (แต่ SuperAdmin ลบ Admin ได้)
                if (currentUserRole == "Admin" && userToDelete.Role == "Admin")
                {
                    TempData["ErrorMessage"] = "แอดมินไม่สามารถลบบัญชีแอดมินด้วยกันเองได้";
                    return RedirectToAction("UserManagement");
                }

                // ถ้าผ่านเงื่อนไข ให้ลบได้
                _context.Users.Remove(userToDelete);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ลบผู้ใช้งานออกจากระบบเรียบร้อยแล้ว";
            }
            return RedirectToAction("UserManagement");
        }

        // GET: Admin/EditUser/5
        // ดึงข้อมูลผู้ใช้งานเดิมมาแสดงบนฟอร์ม
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Admin/EditUser/5
        // บันทึกข้อมูลที่ถูกแก้ไขลงฐานข้อมูล
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, User updatedUser)
        {
            if (id != updatedUser.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Users.Update(updatedUser);
                    _context.SaveChanges();
                    
                    TempData["SuccessMessage"] = $"อัปเดตข้อมูลของคุณ {updatedUser.FullName} เรียบร้อยแล้ว";
                    return RedirectToAction(nameof(UserManagement));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "เกิดข้อผิดพลาดในการอัปเดตข้อมูล: " + ex.Message);
                }
            }
            return View(updatedUser);
        }

        // ==========================================
        // ระบบ: จัดการห้องพัก (Room Management)
        // ==========================================

        // หน้ารายการห้องพักทั้งหมด
        public IActionResult RoomManagement()
        {
            // ดึงข้อมูลห้องพักทั้งหมดจาก Database และรวมข้อมูล RoomType มาด้วย
            var rooms = _context.Rooms.Include(r => r.RoomType).ToList();
            return View(rooms);
        }


        public IActionResult CreateRoom()
        {
            return View();
        }

        // 2. รับข้อมูลห้องพักมาบันทึก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRoom(Room newRoom)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "เพิ่มห้องพักใหม่เรียบร้อยแล้ว";

                
                return RedirectToAction("RoomManagement");
            }
            return View(newRoom);
        }

        // GET: Admin/EditRoom/5
        // ดึงข้อมูลห้องพักเดิมมาแสดงในฟอร์ม
        public IActionResult EditRoom(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        // POST: Admin/EditRoom/5
        // บันทึกการแก้ไขห้องพัก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRoom(int id, Room updatedRoom)
        {
            if (id != updatedRoom.RoomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var roomInDb = _context.Rooms.Find(id);
                    if (roomInDb != null)
                    {
                        // อัปเดตข้อมูล
                        roomInDb.RoomNo = updatedRoom.RoomNo;
                        roomInDb.RoomTypeId = updatedRoom.RoomTypeId;
                        roomInDb.Status = updatedRoom.Status;
                        
                        _context.SaveChanges();
                        TempData["SuccessMessage"] = $"อัปเดตข้อมูลห้องพัก {updatedRoom.RoomNo} เรียบร้อยแล้ว";
                    }
                    return RedirectToAction(nameof(RoomManagement));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "เกิดข้อผิดพลาดในการบันทึก: " + ex.Message);
                }
            }
            return View(updatedRoom);
        }

        // GET: Admin/DeleteRoom/5
        // ระบบลบห้องพัก
        public IActionResult DeleteRoom(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ลบห้องพักออกจากระบบเรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(RoomManagement));
        }
    }
}