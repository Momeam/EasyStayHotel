// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Cookie
using Microsoft.AspNetCore.Http;
// นำเข้า Microsoft.AspNetCore.Mvc.Filters สำหรับ ActionExecutingContext
using Microsoft.AspNetCore.Mvc.Filters;
// นำเข้า Microsoft.AspNetCore.Mvc สำหรับโครงสร้าง MVC Controller และ Action Results
using Microsoft.AspNetCore.Mvc;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;
// นำเข้าเซอร์วิสต่างๆ เช่น FinancialReportService, RoomPreviewHelper
using EasyStayHotel.Services;
// นำเข้า Microsoft.EntityFrameworkCore สำหรับคำสั่งคิวรี Asynchronous เช่น CountAsync, SumAsync, ToListAsync
using Microsoft.EntityFrameworkCore;
// นำเข้า System สำหรับชนิดข้อมูลพื้นฐาน เช่น DateTime, DateOnly, Math
using System;
// นำเข้า System.Collections.Generic สำหรับ List, HashSet
using System.Collections.Generic;
// นำเข้า System.Linq สำหรับฟังก์ชัน LINQ เช่น Where, GroupBy, Select, OrderBy
using System.Linq;
// นำเข้า System.Threading.Tasks สำหรับการประมวลผลแบบ Asynchronous (Task, async/await)
using System.Threading.Tasks;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers
{
    // ตัวควบคุมระบบผู้บริหารและแอดมินสูงสุด (Executive Admin Console)
    public class AdminController : Controller
    {
        // ตัวแปรจัดเก็บ DbContext สำหรับเชื่อมโยงฐานข้อมูล MySQL
        private readonly EasystaydbContext _context;

        // คอนสตรัคเตอร์รับ DbContext ผ่านระบบ Dependency Injection
        public AdminController(EasystaydbContext context)
        {
            // กำหนดค่า DbContext
            _context = context;
        }

        // ตรวจสอบสิทธิ์การเข้าใช้งาน สงวนสิทธิ์เฉพาะผู้ดูแลระบบ (Admin/SuperAdmin) เท่านั้น
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // ดึง UserId ของผู้ใช้จาก Session
            var userId = HttpContext.Session.GetInt32("UserId");
            // ดึง Role ของผู้ใช้จาก Session
            var role = HttpContext.Session.GetString("Role");

            // ตรวจสอบว่าผู้ใช้ยังไม่ได้เข้าสู่ระบบหรือไม่
            if (userId == null || string.IsNullOrEmpty(role))
            {
                // แจ้งเตือนให้เข้าสู่ระบบก่อน
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบผู้ดูแลระบบ (Admin)";
                // จดจำ URL เดิมที่พยายามเข้าถึง
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                // สั่งนำทางไปยังหน้าเข้าสู่ระบบ Login
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                // จบการทำงานทันที
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานต้อนรับ ห้ามเข้าถึงหน้าระบบแอดมิน
            if (role == "Staff")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานฝ่ายบัญชี
            if (role == "Accounting")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("RevenueReport", "Accounting", null);
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานแม่บ้าน
            if (role == "Housekeeper" || role == "Maid")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) สงวนสิทธิ์เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
                return;
            }

            // ตรวจสอบว่าบทบาทไม่ใช่ Admin และไม่ใช่ SuperAdmin หรือไม่
            if (role != "Admin" && role != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบผู้ดูแลระบบ (Admin) เฉพาะแอดมินเท่านั้น";
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            // เรียกการทำงานของคลาสแม่ตามปกติ
            base.OnActionExecuting(context);
        }

        // 1. แดชบอร์ดสถิติภาพรวมโรงแรม อัตราการเข้าพัก Occupancy Rate และรายรับ 3 สาขา
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // ดึงวันที่ปัจจุบัน
            var today = DateOnly.FromDateTime(DateTime.Now);
            // คำนวณวันที่ 6 วันก่อนหน้าเพื่อสรุปแนวโน้มย้อนหลัง 7 วัน
            var sevenDaysAgo = today.AddDays(-6);

            // คำนวณรายรับรวมวันนี้จากการจองที่เช็คอินวันนี้และไม่ถูก Pending
            var todayRevenue = await _context.Bookings
                .Where(b => b.CheckInDate == today && b.BookingStatus != "Pending")
                .SumAsync(b => b.TotalPrice);

            // นับจำนวนห้องพักทั้งหมดในระบบ 3 สาขา
            int totalRooms = await _context.Rooms.CountAsync();
            // นับจำนวนห้องพักที่ว่างพร้อมขายในปัจจุบัน
            int availableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available");
            // คำนวณจำนวนห้องพักที่มีผู้เข้าพัก
            int occupiedRooms = totalRooms - availableRooms;
            // คำนวณอัตราการเข้าพักเฉลี่ย (Occupancy Rate %)
            double occupancyRate = totalRooms > 0 ? Math.Round(((double)occupiedRooms / totalRooms) * 100, 1) : 0;

            // นับจำนวนรายการจองที่แขกกำลังพักอยู่ในโรงแรมขณะนี้ (CheckIn / CheckedIn)
            int activeBookings = await _context.Bookings
                .Where(b => b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn")
                .CountAsync();

            // ส่งข้อมูล KPI ต่างๆ ไปยัง View ผ่าน ViewBag
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.OccupancyRate = occupancyRate;
            ViewBag.ActiveBookings = activeBookings;

            // ดึงข้อมูลการจองย้อนหลัง 7 วันล่าสุด
            var last7DaysBookings = await _context.Bookings
                .Where(b => b.CheckInDate >= sevenDaysAgo && b.CheckInDate <= today)
                .ToListAsync();

            // กำหนด List เก็บป้ายกำกับแกน X ของกราฟรายได้
            var revenueLabels = new List<string>();
            // กำหนด List เก็บตัวเลขยอดเงินแกน Y ของกราฟรายได้
            var revenueData = new List<decimal>();

            // วนลูปคำนวณยอดรายได้แต่ละวันย้อนหลัง 7 วัน
            for (int i = 6; i >= 0; i--)
            {
                // กำหนดวันเป้าหมาย
                var targetDate = today.AddDays(-i);
                // เพิ่มป้ายกำกับวันที่ (เช่น 19 พ.ย.)
                revenueLabels.Add(targetDate.ToString("dd MMM"));

                // รวมยอดเงินของการจองในวันนั้น
                var dailyRev = last7DaysBookings
                    .Where(b => b.CheckInDate == targetDate && b.BookingStatus != "Pending")
                    .Sum(b => b.TotalPrice);

                // เพิ่มยอดเงินลงใน List
                revenueData.Add(dailyRev);
            }
            // ส่งข้อมูลกราฟแท่งไปยัง View
            ViewBag.RevenueLabels = revenueLabels;
            ViewBag.RevenueData = revenueData;

            // คำนวณสัดส่วนการเข้าพักแยกตามประเภทห้องพักสำหรับกราฟโดนัท
            var roomStats = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status != "Available" && r.RoomType != null)
                .GroupBy(r => r.RoomType!.TypeName)
                .Select(g => new { TypeName = g.Key, Count = g.Count() })
                .ToListAsync();

            // ส่งป้ายชื่อประเภทห้องและจำนวนห้องไปยัง View
            ViewBag.RoomTypeLabels = roomStats.Select(r => r.TypeName ?? "ไม่ระบุ").ToList();
            ViewBag.RoomTypeData = roomStats.Select(r => r.Count).ToList();

            // ดึงรายการจองล่าสุด 5 รายการ
            var recentBookings = await _context.Bookings
                .Include(b => b.Room)
                .OrderByDescending(b => b.BookingId)
                .Take(5)
                .ToListAsync();

            // ดึงรายการรายงานสรุปการเงินที่ฝ่ายบัญชีส่งมาให้ผู้จัดการ
            var financialReports = FinancialReportService.GetAll();
            ViewBag.FinancialReports = financialReports;
            // นับจำนวนรายงานที่รอผู้จัดการลงนามอนุมัติ
            ViewBag.PendingFinancialReportsCount = financialReports.Count(r => r.Status == "Pending");

            // แสดงหน้าจอ Dashboard
            return View(recentBookings);
        }

        // 4. ผู้จัดการตรวจสอบ ลงความเห็น และลงนามอนุมัติรายงานการเงินที่ฝ่ายบัญชีส่งมา
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcknowledgeFinancialReport(string reportId, string? managerComment, bool approve = true)
        {
            // ดึงชื่อผู้จัดการจาก Session
            string adminName = HttpContext.Session.GetString("FullName") ?? "ผู้จัดการทั่วไป (Admin)";
            // บันทึกการลงนามรับทราบและข้อคิดเห็นผ่าน FinancialReportService
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

        // 2. ระบบจัดการผู้ใช้ แยกแท็บระหว่างพนักงานทุกแผนกกับลูกค้าทั่วไป และป้องกันการลบบัญชี SuperAdmin
        [HttpGet]
        public async Task<IActionResult> UserManagement(string? tab = "staff", string? roleFilter = null, string? searchString = null)
        {
            // กำหนดแท็บเริ่มต้นเป็น staff (รายชื่อพนักงาน)
            tab = string.IsNullOrEmpty(tab) ? "staff" : tab.ToLower();
            // กำหนดตัวกรองตำแหน่งเริ่มต้นเป็น ALL
            roleFilter = string.IsNullOrEmpty(roleFilter) ? "ALL" : roleFilter;
            // ตัดช่องว่างคำค้นหา
            string cleanSearch = searchString?.Trim() ?? "";

            // เซ็ตตำแหน่งทั้งหมดที่เป็นพนักงาน
            var staffRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SuperAdmin", "Admin", "Staff", "Accounting", "Housekeeper", "Maid"
            };

            // คิวรีดึงข้อมูลผู้ใช้พร้อมประวัติการจอง
            var query = _context.Users
                .Include(u => u.Bookings)
                .AsQueryable();

            // ค้นหาตามคำค้น (ชื่อ อีเมล เบอร์โทร ตำแหน่ง สาขา)
            if (!string.IsNullOrEmpty(cleanSearch))
            {
                query = query.Where(u => u.FullName.Contains(cleanSearch)
                                      || u.Email.Contains(cleanSearch)
                                      || (u.Phone != null && u.Phone.Contains(cleanSearch))
                                      || (u.Role != null && u.Role.Contains(cleanSearch))
                                      || (u.Branch != null && u.Branch.Contains(cleanSearch)));
            }

            // ดึงรายชื่อผู้ใช้ทั้งหมด
            var allUsers = await query.ToListAsync();

            // แยกกลุ่มพนักงานทุกแผนก
            var staffList = allUsers.Where(u => staffRoles.Contains(u.Role ?? "")).ToList();
            // แยกกลุ่มลูกค้าทั่วไป
            var customerList = allUsers.Where(u => !staffRoles.Contains(u.Role ?? "")).ToList();

            // กรองพนักงานตามตำแหน่งที่เลือกในปุ่ม Filter
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

            // ส่งข้อมูลประกอบไปยัง View
            ViewBag.ActiveTab = tab;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.SearchString = cleanSearch;

            // สรุปยอดรวมจำนวนพนักงานและลูกค้า
            ViewBag.TotalStaff = staffList.Count;
            ViewBag.TotalCustomers = customerList.Count;

            // นับจำนวนพนักงานในแต่ละแผนก
            ViewBag.AdminCount = staffList.Count(u => u.Role == "Admin" || u.Role == "SuperAdmin");
            ViewBag.ReceptionCount = staffList.Count(u => u.Role == "Staff");
            ViewBag.HousekeeperCount = staffList.Count(u => u.Role == "Housekeeper" || u.Role == "Maid");
            ViewBag.AccountingCount = staffList.Count(u => u.Role == "Accounting");

            // ส่งรายชื่อพนักงานและลูกค้าที่เรียงลำดับแล้วไปยัง View
            ViewBag.StaffList = filteredStaff.OrderBy(u => u.Role).ThenBy(u => u.UserId).ToList();
            ViewBag.CustomerList = customerList.OrderByDescending(u => u.UserId).ToList();

            // แสดงหน้าจอ UserManagement
            return View();
        }

        // หน้าจอเพิ่มผู้ใช้งานใหม่
        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        // บันทึกผู้ใช้งานใหม่ลงฐานข้อมูล
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User newUser)
        {
            if (ModelState.IsValid)
            {
                // ตรวจสอบว่าอีเมลซ้ำหรือไม่
                bool isEmailExist = _context.Users.Any(u => u.Email == newUser.Email);
                if (isEmailExist)
                {
                    ViewBag.ErrorMessage = "อีเมลนี้ถูกใช้งานไปแล้ว กรุณาใช้เมลอื่น";
                    return View(newUser);
                }
                _context.Users.Add(newUser);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"เพิ่มผู้ใช้งาน {newUser.FullName} เรียบร้อยแล้ว";
                return RedirectToAction("UserManagement");
            }
            return View(newUser);
        }

        // ลบผู้ใช้งานออกจากระบบ พร้อมระบบความปลอดภัยป้องกันการลบ SuperAdmin
        public IActionResult DeleteUser(int id)
        {
            // ค้นหาผู้ใช้ที่ต้องการลบ
            var userToDelete = _context.Users.Find(id);
            if (userToDelete != null)
            {
                var currentUserRole = HttpContext.Session.GetString("Role");

                // ป้องกันการลบบัญชี SuperAdmin สูงสุดเด็ดขาด
                if (userToDelete.Role == "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "ไม่อนุญาตให้ลบบัญชี Super Admin ได้";
                    return RedirectToAction("UserManagement");
                }

                // ป้องกันไม่ให้แอดมินลบบัญชีแอดมินด้วยกันเอง
                if (currentUserRole == "Admin" && userToDelete.Role == "Admin")
                {
                    TempData["ErrorMessage"] = "แอดมินไม่สามารถลบบัญชีแอดมินด้วยกันเองได้";
                    return RedirectToAction("UserManagement");
                }

                // ลบผู้ใช้ออกจากฐานข้อมูล
                _context.Users.Remove(userToDelete);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ลบผู้ใช้งานออกจากระบบเรียบร้อยแล้ว";
            }
            return RedirectToAction("UserManagement");
        }

        // หน้าจอแก้ไขข้อมูลผู้ใช้งาน
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // บันทึกการแก้ไขข้อมูลผู้ใช้งาน
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

        // 3. ระบบจัดการห้องพัก 3 สาขา โดยตัวเลขสรุปสถานะห้องจะคำนวณตามสาขาที่เลือกแบบไดนามิก
        [HttpGet]
        public IActionResult RoomManagement(string? status = null, string? branch = null)
        {
            // กรองห้องพักตามสาขาที่เลือก เพื่อให้ตัวเลขในปุ่มสถานะเปลี่ยนตามสาขาเสมอ
            var branchQuery = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(branch) && branch != "ALL")
            {
                branchQuery = branchQuery.Where(r => r.Branch == branch);
            }

            // คำนวณสรุปจำนวนห้องตามสถานะสำหรับสาขาที่เลือก
            ViewBag.TotalRooms = branchQuery.Count();
            ViewBag.AvailableCount = branchQuery.Count(r => r.Status == "Available");
            ViewBag.OccupiedCount = branchQuery.Count(r => r.Status == "Occupied");
            ViewBag.CleaningCount = branchQuery.Count(r => r.Status == "Cleaning");
            ViewBag.MaintenanceCount = branchQuery.Count(r => r.Status == "Maintenance");

            // คิวรีดึงรายการห้องพักพร้อมประเภทห้อง
            var query = _context.Rooms.Include(r => r.RoomType).AsQueryable();

            if (!string.IsNullOrEmpty(branch) && branch != "ALL")
            {
                query = query.Where(r => r.Branch == branch);
            }

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                query = query.Where(r => r.Status == status);
            }

            // เรียงลำดับตามสาขา ประเภทห้อง และหมายเลขห้อง
            var rooms = query.OrderBy(r => r.Branch).ThenBy(r => r.RoomTypeId).ThenBy(r => r.RoomNo).ToList();
            ViewBag.SelectedStatus = status ?? "ALL";
            ViewBag.SelectedBranch = branch ?? "ALL";

            // แสดงหน้าจอ RoomManagement
            return View(rooms);
        }

        // หน้าจอเพิ่มห้องพักใหม่ (Admin)
        [HttpGet]
        public IActionResult CreateRoom()
        {
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
            return View();
        }

        // บันทึกห้องพักใหม่
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

        // หน้าจอแก้ไขห้องพัก
        [HttpGet]
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

        // ลบห้องพักออกจากระบบ
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
