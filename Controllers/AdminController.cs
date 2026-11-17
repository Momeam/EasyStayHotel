using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using EasyStayHotel.Services;
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


        // GET: Admin/UserManagement
        // หน้ารายการแยกบัญชีผู้ใช้งานทั่วไป (ลูกค้า) และ พนักงานทุกบทบาท (Staff & Personnel)
        public async Task<IActionResult> UserManagement(string? tab = "staff", string? roleFilter = null, string? searchString = null)
        {
            tab = string.IsNullOrEmpty(tab) ? "staff" : tab.ToLower();
            roleFilter = string.IsNullOrEmpty(roleFilter) ? "ALL" : roleFilter;
            string cleanSearch = searchString?.Trim() ?? "";

            var staffRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SuperAdmin", "Admin", "Staff", "Accounting", "Housekeeper", "Maid"
            };

            var query = _context.Users
                .Include(u => u.Bookings)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cleanSearch))
            {
                query = query.Where(u => u.FullName.Contains(cleanSearch)
                                      || u.Email.Contains(cleanSearch)
                                      || (u.Phone != null && u.Phone.Contains(cleanSearch))
                                      || (u.Role != null && u.Role.Contains(cleanSearch))
                                      || (u.Branch != null && u.Branch.Contains(cleanSearch)));
            }

            var allUsers = await query.ToListAsync();

            var staffList = allUsers.Where(u => staffRoles.Contains(u.Role ?? "")).ToList();
            var customerList = allUsers.Where(u => !staffRoles.Contains(u.Role ?? "")).ToList();

            // กรองเพิ่มเติมตาม Role ฝั่งพนักงาน
            var filteredStaff = staffList;
            if (roleFilter != "ALL")
            {
                if (roleFilter == "Housekeeper")
                {
                    filteredStaff = staffList.Where(u => u.Role == "Housekeeper" || u.Role == "Maid").ToList();
                }
                else if (roleFilter == "Admin")
                {
                    filteredStaff = staffList.Where(u => u.Role == "Admin" || u.Role == "SuperAdmin").ToList();
                }
                else
                {
                    filteredStaff = staffList.Where(u => string.Equals(u.Role, roleFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            ViewBag.ActiveTab = tab;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.SearchString = cleanSearch;

            ViewBag.TotalStaff = staffList.Count;
            ViewBag.TotalCustomers = customerList.Count;

            ViewBag.AdminCount = staffList.Count(u => u.Role == "Admin" || u.Role == "SuperAdmin");
            ViewBag.ReceptionCount = staffList.Count(u => u.Role == "Staff");
            ViewBag.HousekeeperCount = staffList.Count(u => u.Role == "Housekeeper" || u.Role == "Maid");
            ViewBag.AccountingCount = staffList.Count(u => u.Role == "Accounting");

            ViewBag.StaffList = filteredStaff.OrderBy(u => u.Role).ThenBy(u => u.UserId).ToList();
            ViewBag.CustomerList = customerList.OrderByDescending(u => u.UserId).ToList();

            return View();
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

            // 4. พนักงานแม่บ้าน (Housekeeper) พยายามเข้าหน้า Admin -> ส่งกลับหน้าแดชบอร์ดแม่บ้าน
            if (role == "Housekeeper" || role == "Maid")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
                return;
            }

            // 5. ผู้ใช้งานทั่วไป (Customer) หรือบทบาทอื่นที่ไม่ใช่ Admin หรือ SuperAdmin -> ส่งกลับหน้าหลัก
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
        .Where(r => r.Status != "Available" && r.RoomType != null) // นับเฉพาะห้องที่ไม่ว่าง
        .GroupBy(r => r.RoomType!.TypeName)
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

    // 6. ดึงรายงานสรุปการเงินที่ฝ่ายบัญชีส่งมาให้ผู้จัดการ (Admin)
    var financialReports = FinancialReportService.GetAll();
    ViewBag.FinancialReports = financialReports;
    ViewBag.PendingFinancialReportsCount = financialReports.Count(r => r.Status == "Pending");

    return View(recentBookings);
}

// POST: /Admin/AcknowledgeFinancialReport
// ผู้จัดการลงนามรับทราบหรืออนุมัติรายงานการเงินจากฝ่ายบัญชี
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult AcknowledgeFinancialReport(string reportId, string? managerComment, bool approve = true)
{
    string adminName = HttpContext.Session.GetString("FullName") ?? "ผู้จัดการทั่วไป (Admin)";
    bool success = FinancialReportService.Acknowledge(reportId, adminName, managerComment, approve);
    if (success)
    {
        TempData["SuccessMessage"] = $"บันทึกการ{(approve ? "อนุมัติ" : "รับทราบ")}รายงานการเงินรหัส {reportId} เรียบร้อยแล้ว";
    }
    else
    {
        TempData["ErrorMessage"] = "ไม่พบรายงานการเงินที่ระบุ";
    }
    return RedirectToAction(nameof(Dashboard));
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
        public IActionResult RoomManagement(string? status = null, string? branch = null)
        {
            var query = _context.Rooms.Include(r => r.RoomType).AsQueryable();

            if (!string.IsNullOrEmpty(branch) && branch != "ALL")
            {
                query = query.Where(r => r.Branch == branch);
            }

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                query = query.Where(r => r.Status == status);
            }

            var rooms = query.OrderBy(r => r.Branch).ThenBy(r => r.RoomTypeId).ThenBy(r => r.RoomNo).ToList();
            ViewBag.SelectedStatus = status ?? "ALL";
            ViewBag.SelectedBranch = branch ?? "ALL";
            ViewBag.TotalRooms = _context.Rooms.Count();
            ViewBag.AvailableCount = _context.Rooms.Count(r => r.Status == "Available");
            ViewBag.OccupiedCount = _context.Rooms.Count(r => r.Status == "Occupied");
            ViewBag.CleaningCount = _context.Rooms.Count(r => r.Status == "Cleaning");
            ViewBag.MaintenanceCount = _context.Rooms.Count(r => r.Status == "Maintenance");

            return View(rooms);
        }

        public IActionResult CreateRoom()
        {
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
            return View();
        }

        // 2. รับข้อมูลห้องพักมาบันทึก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRoom(Room newRoom)
        {
            if (string.IsNullOrEmpty(newRoom.Branch))
            {
                newRoom.Branch = "Bangkok";
            }

            if (ModelState.IsValid)
            {
                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"เพิ่มห้องพักใหม่ {newRoom.RoomNo} (สาขา {RoomPreviewHelper.GetBranchNameTh(newRoom.Branch)}) เรียบร้อยแล้ว";

                return RedirectToAction("RoomManagement");
            }
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
            return View(newRoom);
        }

        // GET: Admin/EditRoom/5
        // ดึงข้อมูลห้องพักเดิมมาแสดงในฟอร์ม
        public IActionResult EditRoom(int id)
        {
            var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
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
                        if (!string.IsNullOrEmpty(updatedRoom.Branch))
                        {
                            roomInDb.Branch = updatedRoom.Branch;
                        }

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
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
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