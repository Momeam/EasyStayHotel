// นำเข้า System สำหรับชนิดข้อมูลพื้นฐานและฟังก์ชัน DateTime
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List และ Collection ต่างๆ
using System.Collections.Generic;
// นำเข้า System.Linq สำหรับฟังก์ชันคิวรี LINQ เช่น Where, Count, FirstOrDefault, AsEnumerable
using System.Linq;
// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Cookie
using Microsoft.AspNetCore.Http;
// นำเข้า Microsoft.AspNetCore.Mvc สำหรับสร้างคอนโทรลเลอร์และ Action Results
using Microsoft.AspNetCore.Mvc;
// นำเข้า Microsoft.AspNetCore.Mvc.Filters สำหรับคลาส ActionExecutingContext
using Microsoft.AspNetCore.Mvc.Filters;
// นำเข้า Microsoft.EntityFrameworkCore สำหรับคำสั่ง Include และฟังก์ชัน ORM
using Microsoft.EntityFrameworkCore;
// นำเข้าโมเดลหลักของ EasyStayHotel
using EasyStayHotel.Models;
// นำเข้าเซอร์วิส HousekeepingService และ RoomPreviewHelper
using EasyStayHotel.Services;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers
{
    // คอนโทรลเลอร์สำหรับควบคุมแดชบอร์ดและการปฏิบัติงานทำความสะอาดของพนักงานแม่บ้าน
    public class HousekeeperController : Controller
    {
        // ตัวแปรจัดเก็บ DbContext สำหรับเชื่อมโยงฐานข้อมูล MySQL
        private readonly EasystaydbContext _context;

        // คอนสตรัคเตอร์รับ DbContext ผ่าน Dependency Injection
        public HousekeeperController(EasystaydbContext context)
        {
            // กำหนดค่า DbContext
            _context = context;
        }

        // ตรวจสอบสิทธิ์การเข้าใช้งาน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // ดึง UserId ของผู้ใช้จาก Session
            var userId = HttpContext.Session.GetInt32("UserId");
            // ดึง Role ของผู้ใช้จาก Session
            var role = HttpContext.Session.GetString("Role");

            // ตรวจสอบว่ายังไม่ได้เข้าสู่ระบบหรือไม่
            if (userId == null || string.IsNullOrEmpty(role))
            {
                // แจ้งเตือนให้เข้าสู่ระบบก่อน
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานแดชบอร์ดแม่บ้าน";
                // จดจำพาธ URL เดิมที่พยายามเข้าถึง
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                // สั่งนำทางไปยังหน้า Login
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                // จบการทำงาน
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานต้อนรับ (Staff) ห้ามเข้าแดชบอร์ดแม่บ้าน
            if (role == "Staff")
            {
                // แจ้งเตือนเรื่องการแบ่งหน้าที่
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงแดชบอร์ดแม่บ้าน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น เพื่อป้องกันการปฏิบัติงานข้ามหน้าที่";
                // นำส่งไปยังหน้าของพนักงานต้อนรับ
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
                // จบการทำงาน
                return;
            }

            // ตรวจสอบว่าบทบาทไม่ใช่ Housekeeper, Maid, Admin หรือ SuperAdmin หรือไม่
            if (role != "Housekeeper" && role != "Maid" && role != "Admin" && role != "SuperAdmin")
            {
                // แจ้งเตือนว่าไม่มีสิทธิ์
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงระบบแม่บ้าน สงวนสิทธิ์เฉพาะพนักงานแม่บ้านและผู้ดูแลระบบเท่านั้น";
                // ส่งกลับหน้าแรกของเว็บไซต์
                context.Result = new RedirectToActionResult("Index", "Home", null);
                // จบการทำงาน
                return;
            }

            // เรียกการทำงานของคลาสแม่ตามปกติ
            base.OnActionExecuting(context);
        }

        // ฟังก์ชันช่วยดึงและล็อกสาขาประจำการของแม่บ้าน
        private string GetCurrentBranch(string? branch = null)
        {
            // ดึงสาขาจาก Session หากไม่มีให้เป็น Bangkok ตามค่าเริ่มต้น
            string currentBranch = HttpContext.Session.GetString("Branch") ?? "Bangkok";
            // หากมีการระบุสาขามาทางพารามิเตอร์ ให้เปลี่ยนสาขาปัจจุบันตามนั้น
            if (!string.IsNullOrEmpty(branch))
            {
                currentBranch = branch;
                // บันทึกสาขาใหม่ลง Session
                HttpContext.Session.SetString("Branch", branch);
            }
            // ส่งค่าสาขาไปยัง View ผ่าน ViewBag
            ViewBag.CurrentBranch = currentBranch;
            // ส่งชื่อภาษาไทยของสาขาไปยัง View
            ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(currentBranch);
            // คืนค่าชื่อสาขา
            return currentBranch;
        }

        // 1. หน้าจอแดชบอร์ดงานทำความสะอาด ซิงค์ห้องพักสถานะ Cleaning เข้ากับระบบงานอัตโนมัติ
        [HttpGet]
        public IActionResult Dashboard(string? branch = null, string? statusFilter = null, string? floorFilter = null)
        {
            // ดึงสาขาที่กำลังดูข้อมูล
            string currentBranch = GetCurrentBranch(branch);
            // ดึงชื่อพนักงานแม่บ้านที่กำลังล็อกอินอยู่
            string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

            // ดึงข้อมูลห้องพักทั้งหมดในสาขานี้จากฐานข้อมูล MySQL พร้อมประเภทห้อง
            var branchRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            // ซิงค์ห้องพักที่สถานะ Cleaning ใน MySQL เข้าสู่รายการงานของแม่บ้านอัตโนมัติ
            HousekeepingService.SyncWithRooms(branchRooms, currentBranch, staffName);

            // ดึงรายการงานทำความสะอาดทั้งหมดในสาขานี้จาก HousekeepingService
            var allTasks = HousekeepingService.GetAllTasks(currentBranch);

            // นับจำนวนงานที่รอดำเนินการ (Pending)
            int pendingCount = allTasks.Count(t => t.Status == "Pending");
            // นับจำนวนงานที่กำลังทำความสะอาดอยู่ (InProgress)
            int inProgressCount = allTasks.Count(t => t.Status == "InProgress");

            // ดึงวันที่ปัจจุบัน
            var today = DateTime.Today;
            // นับจำนวนงานที่ทำความสะอาดเสร็จสิ้นในวันนี้ (Completed Today)
            int completedTodayCount = allTasks.Count(t => t.Status == "Completed" && t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today);

            // นับจำนวนห้องพักที่ว่างพร้อมขาย (Available) ในสาขานี้
            int availableRoomsCount = branchRooms.Count(r => r.Status == "Available");
            // นับจำนวนห้องพักทั้งหมดในสาขานี้
            int totalRoomsCount = branchRooms.Count;

            // ส่งข้อมูลสรุป KPI ต่างๆ ไปยังหน้า View
            ViewBag.PendingCount = pendingCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.CompletedTodayCount = completedTodayCount;
            ViewBag.AvailableRoomsCount = availableRoomsCount;
            ViewBag.TotalRoomsCount = totalRoomsCount;

            // ส่งค่าตัวกรองสถานะและชั้นไปยัง View
            ViewBag.StatusFilter = statusFilter ?? "ALL";
            ViewBag.FloorFilter = floorFilter ?? "ALL";

            // แปลงรายการงานทั้งหมดเป็น Enumerable สำหรับการกรองเพิ่มเติม
            var filteredTasks = allTasks.AsEnumerable();

            // ตรวจสอบและกรองตามสถานะงานหากไม่ใช่ ALL
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "ALL")
            {
                filteredTasks = filteredTasks.Where(t => t.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // ตรวจสอบและกรองตามชั้นของห้องพักหากไม่ใช่ ALL
            if (!string.IsNullOrEmpty(floorFilter) && floorFilter != "ALL")
            {
                // แปลงข้อความเลขชั้นเป็นจำนวนเต็ม
                if (int.TryParse(floorFilter, out int floorNum))
                {
                    // กรองเฉพาะงานในชั้นที่เลือก
                    filteredTasks = filteredTasks.Where(t => t.Floor == floorNum);
                }
            }

            // ส่งข้อมูลรายการห้องพักของสาขาไปยัง View
            ViewBag.BranchRooms = branchRooms;
            // แสดงหน้าจอ Dashboard พร้อมรายการงานที่กรองแล้ว
            return View(filteredTasks.ToList());
        }

        // 2. บันทึกเวลากดเริ่มงานทำความสะอาด ปรับสถานะเป็น InProgress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartTask(string taskId)
        {
            // ตรวจสอบว่ามีรหัสงานส่งมาหรือไม่
            if (string.IsNullOrEmpty(taskId))
            {
                // แจ้งเตือนข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่พบรหัสงานทำความสะอาด";
                return RedirectToAction(nameof(Dashboard));
            }

            // ดึงชื่อแม่บ้านผู้กดเริ่มงานจาก Session
            string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
            // เรียกเซอร์วิสเพื่อบันทึกเวลาเริ่มทำความสะอาดและปรับสถานะเป็น InProgress
            bool success = HousekeepingService.StartCleaning(taskId, housekeeperName);

            // ตรวจสอบผลการบันทึก
            if (success)
            {
                // ดึงข้อมูลงานเพื่อนำเลขห้องมาแสดงในข้อความแจ้งเตือน
                var task = HousekeepingService.GetTaskById(taskId);
                TempData["SuccessMessage"] = $"เริ่มทำความสะอาดห้อง {task?.RoomNo ?? ""} เรียบร้อยแล้ว (กำลังบันทึกเวลาปฏิบัติงาน)";
            }
            else
            {
                // แจ้งเตือนกรณีเกิดข้อผิดพลาด
                TempData["ErrorMessage"] = "ไม่สามารถเริ่มงานได้ กรุณาลองใหม่อีกครั้ง";
            }

            // นำส่งกลับไปยังหน้าแดชบอร์ด
            return RedirectToAction(nameof(Dashboard));
        }

        // 3. ส่งงานทำความสะอาด ตรวจเช็คลิสต์ และปลดล็อกห้องกลับเป็น Available ทันทีเพื่อเปิดขายต่อ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTask(string taskId, string? completionNotes, string[]? checklist)
        {
            // ตรวจสอบว่ามีรหัสงานส่งมาหรือไม่
            if (string.IsNullOrEmpty(taskId))
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสงานทำความสะอาด";
                return RedirectToAction(nameof(Dashboard));
            }

            // ค้นหาข้อมูลงานจาก HousekeepingService
            var task = HousekeepingService.GetTaskById(taskId);
            // หากไม่พบงานในระบบ
            if (task == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลงานทำความสะอาดนี้ในระบบ";
                return RedirectToAction(nameof(Dashboard));
            }

            // ดึงชื่อแม่บ้านผู้ส่งงานจาก Session
            string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
            // แปลงอาเรย์เช็คลิสต์ที่แม่บ้านติ๊กเลือกเป็น List ของสตริง
            var checklistList = checklist != null ? checklist.ToList() : new List<string>();

            // บันทึกการส่งงานทำความสะอาดใน HousekeepingService (สถานะงานเป็น Completed)
            HousekeepingService.CompleteCleaning(taskId, housekeeperName, completionNotes, checklistList);

            // ค้นหาห้องพักที่ตรงกันในตาราง Rooms ของฐานข้อมูล MySQL
            var room = _context.Rooms.FirstOrDefault(r => (r.RoomId == task.RoomId) || (r.RoomNo == task.RoomNo && r.Branch == task.Branch));
            // หากพบห้องพักในฐานข้อมูล
            if (room != null)
            {
                // ปรับสถานะห้องพักในฐานข้อมูล MySQL ให้เป็น Available (ว่างพร้อมขาย) ทันที
                room.Status = "Available";
                // บันทึกการเปลี่ยนแปลงลงใน MySQL เพื่อให้พนักงานต้อนรับนำไปเปิดขายต่อได้ทันที
                _context.SaveChanges();
            }

            // แจ้งข้อความแสดงความสำเร็จ
            TempData["SuccessMessage"] = $"ส่งงานทำความสะอาดห้อง {task.RoomNo} สำเร็จเรียบร้อยแล้ว! สถานะห้องพักเปลี่ยนเป็น 'ว่าง' (Available) พร้อมให้นำไปเปิดขายต่อทันที";
            // นำทางกลับหน้าแดชบอร์ด
            return RedirectToAction(nameof(Dashboard));
        }

        // 4. ปลดล็อกห้องพักเป็นว่างพร้อมขายในคลิกเดียว สำหรับห้องที่ตรวจสภาพเรียบร้อยแล้ว (Direct Release)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectReleaseRoom(int roomId)
        {
            // ค้นหาห้องพักใน MySQL ด้วย roomId
            var room = _context.Rooms.Find(roomId);
            // หากพบห้องพัก
            if (room != null)
            {
                // เปลี่ยนสถานะห้องพักเป็น Available ทันที
                room.Status = "Available";
                // บันทึกลง MySQL
                _context.SaveChanges();

                // ค้นหางานทำความสะอาดที่ยังค้างอยู่ของห้องนี้
                var activeTask = HousekeepingService.GetAllTasks(room.Branch)
                    .FirstOrDefault(t => t.RoomId == roomId && t.Status != "Completed");

                // หากพบงานค้าง ให้ปิดงานทำความสะอาดอัตโนมัติ
                if (activeTask != null)
                {
                    string housekeeperName = HttpContext.Session.GetString("FullName") ?? "แม่บ้าน";
                    HousekeepingService.CompleteCleaning(activeTask.Id, housekeeperName, "ปลดล็อกสถานะห้องเป็นว่างพร้อมขายต่อเรียบร้อยแล้ว");
                }

                // แจ้งข้อความสำเร็จ
                TempData["SuccessMessage"] = $"ปลดล็อกห้อง {room.RoomNo} เป็น 'ว่างพร้อมขาย' (Available) เรียบร้อยแล้ว";
            }

            // นำทางกลับหน้าแดชบอร์ด
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
