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

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานแดชบอร์ดแม่บ้าน";
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (role == "Staff")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงแดชบอร์ดแม่บ้าน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น เพื่อป้องกันการปฏิบัติงานข้ามหน้าที่";
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
                return;
            }

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

        [HttpGet]
        public IActionResult Dashboard(string? branch = null, string? statusFilter = null, string? floorFilter = null)
        {
            string currentBranch = GetCurrentBranch(branch);
            string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

            var branchRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            HousekeepingService.SyncWithRooms(branchRooms, currentBranch, staffName);

            var allTasks = HousekeepingService.GetAllTasks(currentBranch);

            int pendingCount = allTasks.Count(t => t.Status == "Pending");
            int inProgressCount = allTasks.Count(t => t.Status == "InProgress");

            var today = DateTime.Today;
            int completedTodayCount = allTasks.Count(t => t.Status == "Completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today);

            int availableRoomsCount = branchRooms.Count(r => r.Status == "Available");
            int totalRoomsCount = branchRooms.Count;

            ViewBag.PendingCount = pendingCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.CompletedTodayCount = completedTodayCount;
            ViewBag.AvailableRoomsCount = availableRoomsCount;
            ViewBag.TotalRoomsCount = totalRoomsCount;

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

            HousekeepingService.CompleteCleaning(taskId, housekeeperName, completionNotes, checklistList);

            var room = _context.Rooms.FirstOrDefault(r => (r.RoomId == task.RoomId) || (r.RoomNo == task.RoomNo && r.Branch == task.Branch));
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = $"ส่งงานทำความสะอาดห้อง {task.RoomNo} สำเร็จเรียบร้อยแล้ว! สถานะห้องพักเปลี่ยนเป็น 'ว่าง' (Available) พร้อมให้นำไปเปิดขายต่อทันที";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectReleaseRoom(int roomId)
        {
            var room = _context.Rooms.Find(roomId);
            if (room != null)
            {
                room.Status = "Available";
                _context.SaveChanges();

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
