using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using EasyStayHotel.Models;
using EasyStayHotel.Services;

namespace EasyStayHotel.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์ HousekeeperController: จัดการแดชบอร์ดและการปฏิบัติงานของแผนกแม่บ้าน (Housekeeping Management)
    /// 
    /// วงจรการทำงานหลัก (Core Workflow):
    /// 1. ตรวจสอบสิทธิ์ (RBAC): อนุญาตเฉพาะผู้ใช้ Role: Housekeeper, Maid, Admin, SuperAdmin เท่านั้น
    /// 2. แสดงรายการงานทำความสะอาด (Dashboard): ซิงค์ห้องสถานะ "Cleaning" จาก MySQL เข้าสู่คิวงวดงานของแม่บ้าน
    /// 3. รับงาน (StartTask): บันทึกเวลาเริ่มงาน เปลี่ยนสถานะเป็น "InProgress"
    /// 4. ส่งมอบงาน (CompleteTask): บันทึก Checklist ผลการทำความสะอาด และอัปเดตสถานะห้องในตาราง MySQL เป็น "Available" 
    ///    เพื่อให้ห้องนั้นกลับไปแสดงผลบนหน้าเว็บและพร้อมเปิดให้ลูกค้าจองต่อได้ทันที
    /// </summary>
    public class HousekeeperController : Controller
    {
        private readonly EasystaydbContext _context;

        /// <summary>
        /// Constructor รับ EasystaydbContext ผ่าน Dependency Injection
        /// </summary>
        public HousekeeperController(EasystaydbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// กรองและตรวจสอบสิทธิ์ก่อนที่ทุก Action ในคอนโทรลเลอร์นี้จะทำงาน (Action Filter)
        /// คำถามที่อาจารย์มักถาม:
        /// - ทำไมต้องใช้ OnActionExecuting?
        ///   -> เพื่อทำหน้าที่เป็น Centralized Access Control ตรวจสอบ Session และ Role ป้องกันไม่ให้ผู้ใช้พิมพ์ URL เข้ามาตรงๆ โดยไม่ได้รับอนุญาต
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            // 1. ตรวจสอบว่าผู้ใช้ล็อกอินหรือยัง
            if (userId == null || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานแดชบอร์ดแม่บ้าน";
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // 2. ป้องกันไม่ให้พนักงานต้อนรับ (Staff) เข้ามาทำงานข้ามหน้าที่
            if (role == "Staff")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงแดชบอร์ดแม่บ้าน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น เพื่อป้องกันการปฏิบัติงานข้ามหน้าที่";
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
                return;
            }

            // 3. ปฏิเสธสิทธิ์ Customer และ Accounting
            if (role != "Housekeeper" && role != "Maid" && role != "Admin" && role != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงระบบแม่บ้าน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น";
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// ดึงและจัดเก็บสาขาปัจจุบันของแม่บ้านลงใน Session และ ViewBag
        /// </summary>
        private string GetCurrentBranch(string? branch = null)
        {
            string currentBranch = HttpContext.Session.GetString("Branch") ?? "Bangkok";
            if (!string.IsNullOrEmpty(branch))
            {
                currentBranch = branch;
                HttpContext.Session.SetString("Branch", branch);
            }
            ViewBag.CurrentBranch = currentBranch;
            ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(currentBranch);
            return currentBranch;
        }

        // =========================================================================
        // 1. แดชบอร์ดแม่บ้าน (Housekeeper Dashboard)
        // =========================================================================
        /// <summary>
        /// แสดงหน้าแดชบอร์ดแม่บ้าน รายการงานทำความสะอาด สถิติห้องพัก และตัวกรองชั้น/สถานะ
        /// </summary>
        [HttpGet]
        public IActionResult Dashboard(string? branch = null, string? statusFilter = null, string? floorFilter = null)
        {
            string currentBranch = GetCurrentBranch(branch);
            string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

            // ดึงข้อมูลห้องพักทั้งหมดของสาขาที่สังกัดจากฐานข้อมูล MySQL
            var branchRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            // ตรวจสอบและซิงค์ห้องพักในฐานข้อมูลที่มีสถานะ Cleaning ให้เข้ามาอยู่ในระบบงานแม่บ้าน
            HousekeepingService.SyncWithRooms(branchRooms, currentBranch, staffName);

            // ดึงรายการงานทำความสะอาดทั้งหมดของสาขานี้
            var allTasks = HousekeepingService.GetAllTasks(currentBranch);

            // คำนวณสถิติสรุปสำหรับแสดงผลบนการ์ดด้านบน (Cards Metrics)
            int pendingCount = allTasks.Count(t => t.Status == "Pending");
            int inProgressCount = allTasks.Count(t => t.Status == "InProgress");
            
            // จำนวนงานที่ทำความสะอาดเสร็จสิ้นในวันนี้
            var today = DateTime.Today;
            int completedTodayCount = allTasks.Count(t => t.Status == "Completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today);
            
            // จำนวนห้องว่างพร้อมขายในระบบ MySQL ปัจจุบัน
            int availableRoomsCount = branchRooms.Count(r => r.Status == "Available");
            int totalRoomsCount = branchRooms.Count;

            ViewBag.PendingCount = pendingCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.CompletedTodayCount = completedTodayCount;
            ViewBag.AvailableRoomsCount = availableRoomsCount;
            ViewBag.TotalRoomsCount = totalRoomsCount;

            // ตัวกรองสถานะและชั้น
            ViewBag.StatusFilter = statusFilter ?? "ALL";
            ViewBag.FloorFilter = floorFilter ?? "ALL";

            var filteredTasks = allTasks.AsEnumerable();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "ALL")
            {
                filteredTasks = filteredTasks.Where(t => t.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(floorFilter) && floorFilter != "ALL")
            {
                if (int.TryParse(floorFilter, out int floorNum))
                {
                    filteredTasks = filteredTasks.Where(t => t.Floor == floorNum);
                }
            }

            ViewBag.BranchRooms = branchRooms;
            return View(filteredTasks.ToList());
        }

        // =========================================================================
        // 2. รับงาน / เริ่มทำความสะอาด (Start Cleaning Task)
        // =========================================================================
        /// <summary>
        /// แม่บ้านกดรับงานและเริ่มทำความสะอาดห้องพัก (เปลี่ยนสถานะเป็น InProgress)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสงานทำความสะอาด";
                return RedirectToAction(nameof(Dashboard));
            }

            string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
            bool success = HousekeepingService.StartCleaning(taskId, housekeeperName);

            if (success)
            {
                var task = HousekeepingService.GetTaskById(taskId);
                TempData["SuccessMessage"] = $"เริ่มทำความสะอาดห้อง {task?.RoomNo ?? ""} เรียบร้อยแล้ว (กำลังบันทึกเวลาปฏิบัติงาน)";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่สามารถเริ่มงานได้ กรุณาลองใหม่อีกครั้ง";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // =========================================================================
        // 3. ส่งงานทำความสะอาด -> นำห้องไปขายต่อทันที (Complete Cleaning & Ready for Sale)
        // =========================================================================
        /// <summary>
        /// แม่บ้านกดยืนยันทำความสะอาดเสร็จสมบูรณ์:
        /// 1. บันทึกผล Checklist และเวลาเสร็จ
        /// 2. อัปเดตสถานะห้องในตาราง rooms ของ MySQL เป็น "Available" เพื่อให้ห้องขึ้นหน้าเว็บพร้อมขายต่อทันที
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(string taskId, string? completionNotes, string[]? checklist)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสงานทำความสะอาด";
                return RedirectToAction(nameof(Dashboard));
            }

            var task = HousekeepingService.GetTaskById(taskId);
            if (task == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลงานทำความสะอาดนี้ในระบบ";
                return RedirectToAction(nameof(Dashboard));
            }

            string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
            var checklistList = checklist != null ? checklist.ToList() : new List<string>();

            // ขั้นตอนที่ 1: บันทึกสถานะงานเป็น Completed ในระบบบริการแม่บ้าน
            HousekeepingService.CompleteCleaning(taskId, housekeeperName, completionNotes, checklistList);

            // ขั้นตอนที่ 2: สำคัญมาก! อัปเดตสถานะห้องพักในฐานข้อมูล MySQL เป็น "Available"
            // เพื่อให้ระบบหน้าบ้าน (Public Booking) มองเห็นห้องนี้และให้ลูกค้าจองต่อได้ทันที
            var room = _context.Rooms.FirstOrDefault(r => (r.RoomId == task.RoomId) || (r.RoomNo == task.RoomNo && r.Branch == task.Branch));
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = $"ส่งงานทำความสะอาดห้อง {task.RoomNo} สำเร็จเรียบร้อยแล้ว! สถานะห้องพักเปลี่ยนเป็น 'ว่าง' (Available) พร้อมให้นำไปเปิดขายต่อทันที";
            return RedirectToAction(nameof(Dashboard));
        }

        // =========================================================================
        // 4. ปลดล็อกห้องพักให้พร้อมขายด่วน (Direct Release Room to Available)
        // =========================================================================
        /// <summary>
        /// คำสั่งด่วนสำหรับแม่บ้านหรือผู้ดูแลระบบในการปลดล็อกสถานะห้องเป็น "Available" ทันที
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectReleaseRoom(int roomId)
        {
            var room = _context.Rooms.Find(roomId);
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();

                // อัปเดตงานทำความสะอาดที่ค้างอยู่ (ถ้ามี) ให้เสร็จสิ้นด้วย
                var activeTask = HousekeepingService.GetAllTasks(room.Branch)
                    .FirstOrDefault(t => t.RoomId == roomId && t.Status != "Completed");
                
                if (activeTask != null)
                {
                    string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
                    HousekeepingService.CompleteCleaning(activeTask.Id, housekeeperName, "ปลดล็อกสถานะห้องเป็นว่างพร้อมขายต่อเรียบร้อยแล้ว");
                }

                TempData["SuccessMessage"] = $"ปลดล็อกห้อง {room.RoomNo} เป็น 'ว่างพร้อมขาย' (Available) เรียบร้อยแล้ว";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
