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
    public class HousekeeperController : Controller
    {
        private readonly EasystaydbContext _context;

        public HousekeeperController(EasystaydbContext context)
        {
            _context = context;
        }

        // ตรวจสอบสิทธิ์การเข้าถึง (Housekeeper, Maid, Admin, SuperAdmin)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            // 1. ยังไม่ได้เข้าสู่ระบบ
            if (userId == null || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานแดชบอร์ดแม่บ้าน";
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // 2. ถ้าเป็นพนักงานต้อนรับ (Staff) พยายามเข้าหน้าแม่บ้าน -> ดีดกลับหน้ารายการจองของพนักงาน
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

        // ==========================================
        // 1. แดชบอร์ดแม่บ้าน (Housekeeper Dashboard)
        // ==========================================
        [HttpGet]
        public IActionResult Dashboard(string? branch = null, string? statusFilter = null, string? floorFilter = null)
        {
            string currentBranch = GetCurrentBranch(branch);
            string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

            // ดึงข้อมูลห้องพักทั้งหมดของสาขานี้จากฐานข้อมูล
            var branchRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            // ซิงค์ห้องพักในฐานข้อมูลที่มีสถานะ Cleaning เข้ากับ HousekeepingService
            HousekeepingService.SyncWithRooms(branchRooms, currentBranch, staffName);

            // ดึงรายการงานทำความสะอาดทั้งหมดของสาขานี้
            var allTasks = HousekeepingService.GetAllTasks(currentBranch);

            // สถิติต่างๆ สำหรับหัวแดชบอร์ด
            int pendingCount = allTasks.Count(t => t.Status == "Pending");
            int inProgressCount = allTasks.Count(t => t.Status == "InProgress");
            
            // งานที่เสร็จสิ้นในวันนี้
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

            // ตัวกรอง
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

            // ส่งข้อมูลห้องพักทั้งหมดให้ View เพื่อใช้ในการตรวจสอบสถานะแบบเรียลไทม์
            ViewBag.BranchRooms = branchRooms;

            return View(filteredTasks.ToList());
        }

        // ==========================================
        // 2. รับงาน / เริ่มทำความสะอาด (Start Cleaning)
        // ==========================================
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

        // ==========================================
        // 3. ส่งงานทำความสะอาด -> นำห้องไปขายต่อทันที (Complete Cleaning & Ready for Sale)
        // ==========================================
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

            // 1. บันทึกผลการส่งงานลงใน HousekeepingService
            HousekeepingService.CompleteCleaning(taskId, housekeeperName, completionNotes, checklistList);

            // 2. สำคัญที่สุด: อัปเดตสถานะห้องในฐานข้อมูล MySQL เป็น "Available" เพื่อนำไปเปิดขายต่อทันที!
            var room = _context.Rooms.FirstOrDefault(r => (r.RoomId == task.RoomId) || (r.RoomNo == task.RoomNo && r.Branch == task.Branch));
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = $"✨ ส่งงานทำความสะอาดห้อง {task.RoomNo} สำเร็จเรียบร้อยแล้ว! สถานะห้องพักเปลี่ยนเป็น 'ว่าง' (Available) พร้อมให้นำไปเปิดขายต่อทันที";
            return RedirectToAction(nameof(Dashboard));
        }

        // ==========================================
        // 4. ปลดล็อกห้องพักให้พร้อมขายด่วน (Direct Release Room to Available)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectReleaseRoom(int roomId)
        {
            var room = _context.Rooms.Find(roomId);
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();

                // อัปเดต Task ถ้ามีอยู่
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
