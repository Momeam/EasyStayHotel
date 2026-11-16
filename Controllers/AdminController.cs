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

        // ฟังก์ชันนี้จะทำงาน "ก่อน" ที่ทุกหน้าใน Admin จะถูกเปิด เพื่อตรวจสอบสิทธิ์การเข้าถึง
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            // 1. ยังไม่ได้เข้าสู่ระบบ -> ส่งไปหน้า Login
            if (userId == null || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบผู้ดูแลระบบ (Admin)";
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // 2. พนักงานทั่วไป (Staff) พยายามเข้าหน้า Admin -> ส่งกลับหน้าพนักงาน
            if (role == "Staff")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
                return;
            }

            // 3. ฝ่ายบัญชี (Accounting) พยายามเข้าหน้า Admin -> ส่งกลับหน้ารายงานบัญชี
            if (role == "Accounting")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("RevenueReport", "Accounting", null);
                return;
            }

            // 4. ผู้ใช้งานทั่วไป (Customer) หรือบทบาทอื่นที่ไม่ใช่ Admin หรือ SuperAdmin -> ส่งกลับหน้าหลัก
            if (role != "Admin" && role != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
      
       // GET: Admin/Dashboard
       
       public async Task<IActionResult> Dashboard()
{
    // 1. ตั้งค่าวันที่ปัจจุบันและย้อนหลัง 7 วัน
    var today = DateOnly.FromDateTime(DateTime.Now);
    var sevenDaysAgo = today.AddDays(-6);

    // 2. คำนวณสถิติ 4 การ์ดด้านบน
    var todayRevenue = await _context.Bookings
        .Where(b => b.CheckInDate == today && b.BookingStatus != "Pending")
        .SumAsync(b => b.TotalPrice);

    int totalRooms = await _context.Rooms.CountAsync();
    int availableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available");
    int occupiedRooms = totalRooms - availableRooms;
    double occupancyRate = totalRooms > 0 ? Math.Round(((double)occupiedRooms / totalRooms) * 100, 1) : 0;

    int activeBookings = await _context.Bookings
        .Where(b => b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn")
        .CountAsync();

    ViewBag.TodayRevenue = todayRevenue;
    ViewBag.AvailableRooms = availableRooms;
    ViewBag.TotalRooms = totalRooms;
    ViewBag.OccupancyRate = occupancyRate;
    ViewBag.ActiveBookings = activeBookings;

    // 3. เตรียมข้อมูลกราฟแท่ง (รายรับย้อนหลัง 7 วัน)
    var last7DaysBookings = await _context.Bookings
        .Where(b => b.CheckInDate >= sevenDaysAgo && b.CheckInDate <= today)
        .ToListAsync();

    var revenueLabels = new List<string>();
    var revenueData = new List<decimal>();

    for (int i = 6; i >= 0; i--)
    {
        var targetDate = today.AddDays(-i);
        revenueLabels.Add(targetDate.ToString("dd MMM")); // เช่น 15 พ.ย.
        
        var dailyRev = last7DaysBookings
            .Where(b => b.CheckInDate == targetDate && b.BookingStatus != "Pending")
            .Sum(b => b.TotalPrice);
            
        revenueData.Add(dailyRev);
    }
    ViewBag.RevenueLabels = revenueLabels;
    ViewBag.RevenueData = revenueData;

    // 4. เตรียมข้อมูลกราฟโดนัท (สัดส่วนห้องพักที่ถูกจองแยกตามประเภท)
    var roomStats = await _context.Rooms
        .Include(r => r.RoomType)
        .Where(r => r.Status != "Available") // นับเฉพาะห้องที่ไม่ว่าง
        .GroupBy(r => r.RoomType.TypeName)
        .Select(g => new { TypeName = g.Key, Count = g.Count() })
        .ToListAsync();

    ViewBag.RoomTypeLabels = roomStats.Select(r => r.TypeName ?? "ไม่ระบุ").ToList();
    ViewBag.RoomTypeData = roomStats.Select(r => r.Count).ToList();

    // 5. ตารางการจองล่าสุด
    var recentBookings = await _context.Bookings
        .Include(b => b.Room)
        .OrderByDescending(b => b.BookingId)
        .Take(5)
        .ToListAsync();

    return View(recentBookings);
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