// นำเข้า System สำหรับชนิดข้อมูลพื้นฐาน เช่น DateTime, DateOnly, String
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List และ Dictionary
using System.Collections.Generic;
// นำเข้า System.Linq สำหรับฟังก์ชันคิวรี LINQ เช่น Where, Count, Sum, OrderBy, FirstOrDefault
using System.Linq;
// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Request Context
using Microsoft.AspNetCore.Http;
// นำเข้า Microsoft.AspNetCore.Mvc สำหรับสร้างคอนโทรลเลอร์และ Action Results
using Microsoft.AspNetCore.Mvc;
// นำเข้า Microsoft.AspNetCore.Mvc.Filters สำหรับคลาส ActionExecutingContext ใน OnActionExecuting
using Microsoft.AspNetCore.Mvc.Filters;
// นำเข้า Microsoft.EntityFrameworkCore สำหรับคำสั่ง Include, ThenInclude, ToList
using Microsoft.EntityFrameworkCore;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;
// นำเข้า ViewModels ที่เกี่ยวข้องกับการทำงานของพนักงาน เช่น WalkinViewModel
using EasyStayHotel.Models.ViewModels;
// นำเข้าเซอร์วิสต่างๆ เช่น HousekeepingService, AccountingFeedbackService, RoomPreviewHelper, DateHelper
using EasyStayHotel.Services;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers
{
    // ตัวควบคุมระบบเคาน์เตอร์พนักงานต้อนรับ (Front Desk Staff Console)
    public class StaffController : Controller
    {
        // ตัวแปรจัดเก็บ DbContext สำหรับเชื่อมโยงฐานข้อมูล MySQL
        private readonly EasystaydbContext _context;

        // คอนสตรัคเตอร์รับ DbContext ผ่าน Dependency Injection
        public StaffController(EasystaydbContext context)
        {
            // กำหนดค่า DbContext
            _context = context;
        }

        // ตรวจสอบสิทธิ์การเข้าใช้งาน สงวนสิทธิ์เฉพาะพนักงานต้อนรับและผู้ดูแลระบบเท่านั้น
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
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบพนักงาน";
                // จดจำพาธ URL เดิมที่พยายามเข้าถึง
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                // สั่งนำทางไปยังหน้า Login
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                // จบการทำงาน
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานแม่บ้าน ห้ามเข้าหน้าระบบพนักงานต้อนรับ
            if (role == "Housekeeper" || role == "Maid")
            {
                // แจ้งเตือนเรื่องการปฏิบัติงานข้ามหน้าที่
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงานต้อนรับ สงวนสิทธิ์เฉพาะพนักงานต้อนรับและผู้ดูแลระบบเท่านั้น เพื่อป้องกันการปฏิบัติงานข้ามหน้าที่";
                // ส่งไปยังหน้า Dashboard ของแม่บ้าน
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
                // จบการทำงาน
                return;
            }

            // ตรวจสอบกรณีเป็นพนักงานฝ่ายบัญชี
            if (role == "Accounting")
            {
                // แจ้งเตือนว่าไม่มีสิทธิ์
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงานต้อนรับ";
                // ส่งไปยังหน้ารายงานรายได้ของบัญชี
                context.Result = new RedirectToActionResult("RevenueReport", "Accounting", null);
                // จบการทำงาน
                return;
            }

            // ตรวจสอบว่าบทบาทไม่ใช่ Staff, Admin หรือ SuperAdmin หรือไม่
            if (role != "Staff" && role != "Admin" && role != "SuperAdmin")
            {
                // แจ้งเตือนว่าไม่มีสิทธิ์
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงาน สงวนสิทธิ์เฉพาะพนักงานและผู้ดูแลระบบเท่านั้น";
                // ส่งกลับหน้าแรกของเว็บไซต์
                context.Result = new RedirectToActionResult("Index", "Home", null);
                // จบการทำงาน
                return;
            }

            // เรียกการทำงานของคลาสแม่ตามปกติ
            base.OnActionExecuting(context);
        }

        // ล็อกสาขาประจำการของพนักงานต้อนรับ ป้องกันการดูหรือแก้ไขข้อมูลข้ามสาขา
        private string GetCurrentBranch(string? branch = null)
        {
            // ดึงบทบาทของผู้ใช้จาก Session
            var role = HttpContext.Session.GetString("Role");
            // ดึงสาขาที่ผู้ใช้ประจำการอยู่จาก Session (หากไม่มีให้เป็น Bangkok)
            string userBranch = HttpContext.Session.GetString("Branch") ?? "Bangkok";

            // หากเป็นพนักงานต้อนรับ (Staff) จะถูกล็อกสาขาให้ทำงานเฉพาะสาขาตนเองเท่านั้น
            if (role == "Staff")
            {
                // ส่งสาขาไปยัง View
                ViewBag.CurrentBranch = userBranch;
                // ส่งชื่อภาษาไทยของสาขา
                ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(userBranch);
                // ระบุว่าสาขาถูกล็อก ไม่สามารถสลับสาขาได้
                ViewBag.IsBranchLocked = true;
                // คืนค่าสาขาประจำการ
                return userBranch;
            }

            // หากเป็น Admin หรือ SuperAdmin สามารถเลือกสลับสาขาเพื่อดูข้อมูลได้
            string currentBranch = userBranch;
            if (!string.IsNullOrEmpty(branch))
            {
                currentBranch = branch;
                // บันทึกสาขาใหม่ลง Session
                HttpContext.Session.SetString("Branch", branch);
            }
            // ส่งสาขาปัจจุบันไปยัง View
            ViewBag.CurrentBranch = currentBranch;
            // ส่งชื่อภาษาไทยของสาขาไปยัง View
            ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(currentBranch);
            // ระบุว่าไม่ได้ล็อกสาขา (สามารถเลือกดูสาขาอื่นได้)
            ViewBag.IsBranchLocked = false;
            // คืนค่าสาขาที่เลือก
            return currentBranch;
        }

        // 1. ภาพรวมการปฏิบัติงานประจำวัน แสดงตัวเลขสถิติและรายรับประจำวัน
        [HttpGet]
        public IActionResult DailyOverview(string? date = null, string? branch = null)
        {
            // ดึงสาขาที่กำลังดูข้อมูล
            string currentBranch = GetCurrentBranch(branch);

            // ตัวแปรวันที่เป้าหมายที่ต้องการดูข้อมูล
            DateOnly targetDate;
            // ลองแปลงข้อความวันที่ หากไม่สำเร็จให้ใช้วันที่ปัจจุบัน (Today)
            if (!DateOnly.TryParse(date, out targetDate))
            {
                targetDate = DateOnly.FromDateTime(DateTime.Today);
            }
            // ปรับเทียบปีให้เป็น ค.ศ. สากล
            targetDate = DateHelper.NormalizeToGregorian(targetDate);

            // ส่งข้อมูลวันที่ไปยัง View
            ViewBag.TargetDate = targetDate;
            ViewBag.TargetDateStr = DateHelper.ToIsoDate(targetDate);
            ViewBag.TargetDateTh = DateHelper.ToThaiFullDate(targetDate.ToDateTime(TimeOnly.MinValue));
            ViewBag.IsToday = targetDate == DateOnly.FromDateTime(DateTime.Today);

            // คิวรีรายการแขกที่จะเช็คอินในวันนี้ (Arrivals Today)
            var arrivalsToday = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckInDate == targetDate && b.BookingStatus != "Completed")
                .OrderBy(b => b.BookingStatus)
                .ToList();

            // คิวรีรายการแขกที่จะเช็คเอาท์ในวันนี้ (Departures Today)
            var departuresToday = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckOutDate == targetDate && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"))
                .ToList();

            // คิวรีรายการแขกที่กำลังพักอยู่ในโรงแรมขณะนี้ (In-House Bookings)
            var inHouseBookings = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"))
                .ToList();

            // ดึงรายการห้องพักทั้งหมดในสาขานี้
            var rooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            // สรุปจำนวนห้องพักตามสถานะต่างๆ
            int totalRooms = rooms.Count;
            int availableRooms = rooms.Count(r => r.Status == "Available");
            int occupiedRooms = rooms.Count(r => r.Status == "Occupied");
            int cleaningRooms = rooms.Count(r => r.Status == "Cleaning");
            int maintenanceRooms = rooms.Count(r => r.Status == "Maintenance");
            int reservedRooms = rooms.Count(r => r.Status == "Reserved");

            // กำหนดช่วงเวลาเริ่มต้นและสิ้นสุดของวันเป้าหมาย
            DateTime startOfDay = targetDate.ToDateTime(TimeOnly.MinValue);
            DateTime endOfDay = targetDate.ToDateTime(TimeOnly.MaxValue);
            // คิวรีรายการชำระเงินที่เกิดขึ้นในวันนั้น
            var paymentsToday = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Where(p => p.Booking != null && p.Booking.Room != null && p.Booking.Room.Branch == currentBranch && p.TransferDate >= startOfDay && p.TransferDate <= endOfDay)
                .ToList();

            // คำนวณรายรับรวมประจำวัน
            decimal totalRevenue = paymentsToday.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed").Sum(p => p.Amount);
            // คำนวณรายรับเงินสด
            decimal cashRevenue = paymentsToday.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            // คำนวณรายรับเงินโอนพร้อมเพย์
            decimal promptPayRevenue = paymentsToday.Where(p => p.PaymentMethod == "PromptPay").Sum(p => p.Amount);

            // ส่งข้อมูลสรุปทั้งหมดไปยัง View
            ViewBag.ArrivalsToday = arrivalsToday;
            ViewBag.DeparturesToday = departuresToday;
            ViewBag.InHouseBookings = inHouseBookings;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.OccupiedRooms = occupiedRooms;
            ViewBag.CleaningRooms = cleaningRooms;
            ViewBag.MaintenanceRooms = maintenanceRooms;
            ViewBag.ReservedRooms = reservedRooms;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.CashRevenue = cashRevenue;
            ViewBag.PromptPayRevenue = promptPayRevenue;

            // แสดงหน้าจอ DailyOverview
            return View();
        }

        // 2. หน้าเคาน์เตอร์เช็คอินและจัดสรรหมายเลขห้องพักจริงให้ลูกค้า
        [HttpGet]
        public IActionResult CheckInDesk(string? bookingRef = null, string? search = null, string? branch = null)
        {
            // ดึงสาขาที่กำลังดูข้อมูล
            string currentBranch = GetCurrentBranch(branch);
            // ดึงข้อความค้นหา ไม่ว่าจะเป็นรหัสการจอง เบอร์โทร หรือชื่อแขก
            string queryText = !string.IsNullOrWhiteSpace(bookingRef) ? bookingRef.Trim() : (search?.Trim() ?? "");

            // ค้นหาการจองเป้าหมายตามข้อความค้นหา
            Booking? targetBooking = null;
            if (!string.IsNullOrEmpty(queryText))
            {
                // ค้นหาขั้นที่ 1: ตรวจสอบจาก BookingRef หรือเบอร์โทรศัพท์ในสาขานั้น
                targetBooking = _context.Bookings
                    .Include(b => b.Room)
                        .ThenInclude(r => r!.RoomType)
                    .Include(b => b.User)
                    .Include(b => b.Payments)
                    .FirstOrDefault(b => (b.BookingRef == queryText || (b.User != null && b.User.Phone == queryText) || b.BookingRef.Contains(queryText)) && b.Room != null && b.Room.Branch == currentBranch);

                // ค้นหาขั้นที่ 2: ตรวจสอบแบบมีคำบางส่วน หรือตรงกับชื่อแขก
                if (targetBooking == null)
                {
                    targetBooking = _context.Bookings
                        .Include(b => b.Room)
                            .ThenInclude(r => r!.RoomType)
                        .Include(b => b.User)
                        .Include(b => b.Payments)
                        .FirstOrDefault(b => (b.BookingRef.Contains(queryText) || (b.GuestName != null && b.GuestName.Contains(queryText))) && b.Room != null && b.Room.Branch == currentBranch);
                }

                // หากยังไม่พบข้อมูล ให้แจ้งเตือนผู้ใช้
                if (targetBooking == null)
                {
                    ViewBag.SearchError = $"ไม่พบข้อมูลการจองที่ตรงกับ '{queryText}' ในสาขา{ViewBag.CurrentBranchTh}";
                }
            }

            // คิวรีดึงรายการห้องพักที่ว่างพร้อมเข้าพักในสาขานี้ (รวมถึงห้องที่ผูกกับการจองนี้อยู่แล้ว)
            var availableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch && (r.Status == "Available" || (targetBooking != null && r.RoomId == targetBooking.RoomId)))
                .OrderBy(r => r.RoomTypeId)
                .ThenBy(r => r.RoomNo)
                .ToList();

            // ดึงรายการแขกที่รอเช็คอินในวันนี้หรือเลยกำหนดเช็คอิน
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pendingArrivals = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && b.BookingStatus != "Completed" && (b.BookingStatus == "Pending" || b.BookingStatus == "Confirmed" || b.BookingStatus == "Reserved") && b.CheckInDate <= today)
                .OrderBy(b => b.CheckInDate)
                .Take(10)
                .ToList();

            // ส่งข้อมูลไปยัง View
            ViewBag.SearchQuery = queryText;
            ViewBag.TargetBooking = targetBooking;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.PendingArrivals = pendingArrivals;

            // หากพบรายการจอง ให้คำนวณสถิติสถานะห้องพักในประเภทที่จอง
            if (targetBooking?.Room?.RoomType != null)
            {
                int? bookedTypeId = targetBooking.Room.RoomTypeId;
                var allTypeRooms = _context.Rooms
                    .Where(r => r.Branch == currentBranch && r.RoomTypeId == bookedTypeId)
                    .ToList();
                ViewBag.BookedTypeTotal = allTypeRooms.Count;
                ViewBag.BookedTypeAvailable = allTypeRooms.Count(r => r.Status == "Available" || r.RoomId == targetBooking.RoomId);
                ViewBag.BookedTypeCleaning = allTypeRooms.Count(r => r.Status == "Cleaning");
                ViewBag.BookedTypeOccupied = allTypeRooms.Count(r => r.Status == "Occupied");
            }

            // แสดงหน้าจอ CheckInDesk
            return View();
        }

        // ยืนยันการเช็คอิน ผูกหมายเลขห้อง ปรับสถานะห้องเป็น Occupied และปรับการจองเป็น CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessCheckIn(int bookingId, int roomId, string? notes = null)
        {
            // ค้นหารายการจองในฐานข้อมูล
            var booking = _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefault(b => b.BookingId == bookingId);

            // หากไม่พบรายการจอง
            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการเช็คอิน";
                return RedirectToAction(nameof(CheckInDesk));
            }

            // ค้นหาห้องพักใหม่ที่เลือกมอบให้ลูกค้า
            var newRoom = _context.Rooms.Find(roomId);
            if (newRoom == null)
            {
                TempData["ErrorMessage"] = "ไม่พบห้องพักที่เลือก";
                return RedirectToAction(nameof(CheckInDesk), new { bookingRef = booking.BookingRef });
            }

            // ตรวจสอบสิทธิ์สาขาของพนักงานต้อนรับ
            string currentBranch = GetCurrentBranch();
            var role = HttpContext.Session.GetString("Role");
            if (role == "Staff" && (newRoom.Branch != currentBranch || (booking.Room != null && booking.Room.Branch != currentBranch)))
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์จัดการการเช็คอินของสาขาอื่น";
                return RedirectToAction(nameof(BookingList));
            }

            // ตรวจสอบว่าได้รับการยืนยันชำระเงิน (ตรวจสลิป) เรียบร้อยแล้วหรือยัง ก่อนอนุญาตให้เช็คอิน
            var payment = _context.Payments
                .OrderByDescending(p => p.PaymentId)
                .FirstOrDefault(p => p.BookingId == bookingId);

            if (payment == null || payment.PaymentStatus != "Paid")
            {
                TempData["ErrorMessage"] = $"ไม่สามารถเช็คอินรหัส {booking.BookingRef} ได้ กรุณาตรวจสอบสลิปและกดยืนยันรับชำระเงินก่อนส่งมอบห้องพัก";
                return RedirectToAction(nameof(CheckInDesk), new { bookingRef = booking.BookingRef });
            }

            // หากมีการเปลี่ยนห้องพัก ให้ปลดล็อกห้องพักเดิมกลับเป็น Available
            if (booking.RoomId != roomId && booking.Room != null)
            {
                booking.Room.Status = "Available";
            }

            // ผูกหมายเลขห้องใหม่เข้ากับการจอง
            booking.RoomId = roomId;
            // ปรับสถานะการจองเป็น CheckIn (เข้าพักแล้ว)
            booking.BookingStatus = "CheckIn";
            // ปรับสถานะห้องพักใหม่เป็น Occupied (มีแขกพักอยู่)
            newRoom.Status = "Occupied";

            // บันทึกการเปลี่ยนแปลงทั้งหมดลง MySQL
            _context.SaveChanges();

            // แจ้งข้อความสำเร็จและนำไปยังหน้าพิมพ์ใบเสร็จ BookingReceipt
            TempData["SuccessMessage"] = $"เช็คอินสำเร็จ! แขก: {booking.GuestName} เข้าพักห้อง {newRoom.RoomNo} เรียบร้อยแล้ว";
            return RedirectToAction(nameof(BookingReceipt), new { id = booking.BookingId });
        }

        // 3. เช็คเอาท์ห้องพัก ปรับสถานะการจองเป็น Completed และเปลี่ยนสถานะห้องเป็น Cleaning ส่งต่อให้แม่บ้าน
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOutBooking(int id)
        {
            // ค้นหารายการจองพร้อมข้อมูลห้องพักและผู้ใช้
            var booking = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking != null)
            {
                // ตรวจสอบสิทธิ์สาขา
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && booking.Room != null && booking.Room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เช็คเอาท์ห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(BookingList));
                }

                // ปรับสถานะการจองเป็น Completed (เสร็จสิ้น)
                booking.BookingStatus = "Completed";
                if (booking.Room != null)
                {
                    // เปลี่ยนสถานะห้องพักเป็น Cleaning (รอทำความสะอาด)
                    booking.Room.Status = "Cleaning";
                    _context.SaveChanges();

                    // ดึงชื่อพนักงานและชื่อแขก
                    string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงานต้อนรับ";
                    string guestName = booking.User?.FullName ?? "ลูกค้าทั่วไป";
                    // สร้างงานทำความสะอาดส่งต่อให้แม่บ้านในระบบ HousekeepingService โดยอัตโนมัติ
                    HousekeepingService.CreateTask(
                        booking.Room.RoomId,
                        booking.Room.RoomNo,
                        booking.Room.RoomType?.TypeName ?? "Standard",
                        booking.Room.Branch,
                        staffName,
                        priority: "Normal",
                        notes: $"ลูกค้า {guestName} เช็คเอาท์เรียบร้อยแล้ว รบกวนทำความสะอาดและจัดเตรียมห้องสำหรับผู้เข้าพักถัดไป",
                        guestName: guestName,
                        source: "CheckOut"
                    );
                }
                else
                {
                    _context.SaveChanges();
                }

                // แจ้งข้อความสำเร็จ
                TempData["SuccessMessage"] = $"เช็คเอาท์แขกห้อง {(booking.Room != null ? booking.Room.RoomNo : "")} เรียบร้อยแล้ว (ส่งรายการทำความสะอาดไปยังฝั่งแม่บ้านแล้ว)";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการเช็คเอาท์";
            }

            // นำทางกลับไปยังหน้าที่เรียกมา (Referer) หากมี
            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            // นำส่งกลับไปยังหน้ารายชื่อการจอง BookingList
            return RedirectToAction(nameof(BookingList));
        }

        // เมธอดเช็คอินด่วนจากหน้ารายชื่อการจอง
        public IActionResult CheckInBooking(int id)
        {
            // ค้นหารายการจอง
            var booking = _context.Bookings.Include(b => b.Room).FirstOrDefault(b => b.BookingId == id);
            if (booking != null)
            {
                // ปรับสถานะการจองเป็น CheckIn
                booking.BookingStatus = "CheckIn";
                if (booking.Room != null)
                {
                    // ปรับสถานะห้องเป็น Occupied
                    booking.Room.Status = "Occupied";
                }
                // บันทึกการเปลี่ยนแปลง
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"รับเช็คอินคุณ {booking.GuestName} เข้าห้อง {(booking.Room != null ? booking.Room.RoomNo : "")} เรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(BookingList));
        }

        // หน้ากระดานควบคุมสถานะห้องพักแบบการ์ดเรียลไทม์ (Room Control Board)
        public ActionResult Index(string? branch = null, string? filterStatus = null)
        {
            // ดึงสาขาที่กำลังดูข้อมูล
            string currentBranch = GetCurrentBranch(branch);

            // คิวรีดึงข้อมูลห้องพักในสาขานี้พร้อมประเภทห้อง
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch);

            // กรองสถานะห้องพักหากมีการระบุ
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                query = query.Where(r => r.Status == filterStatus);
            }

            // เรียงลำดับตามประเภทห้องและหมายเลขห้อง
            var rooms = query.OrderBy(r => r.RoomTypeId).ThenBy(r => r.RoomNo).ToList();

            // ค้นหารายการจองที่มีแขกพักอยู่จริงในห้องพักแต่ละห้องขณะนี้
            var activeBookings = _context.Bookings
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn") && b.RoomId.HasValue)
                .GroupBy(b => b.RoomId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // ส่งข้อมูลการจองปัจจุบันไปยัง View
            ViewBag.ActiveBookings = activeBookings;
            ViewBag.FilterStatus = filterStatus ?? "ALL";

            // สรุปจำนวนห้องพักในแต่ละสถานะสำหรับแสดงผลการ์ดสรุปด้านบน
            var allBranchRooms = _context.Rooms.Where(r => r.Branch == currentBranch).ToList();
            ViewBag.CountAvailable = allBranchRooms.Count(r => r.Status == "Available");
            ViewBag.CountOccupied = allBranchRooms.Count(r => r.Status == "Occupied");
            ViewBag.CountCleaning = allBranchRooms.Count(r => r.Status == "Cleaning");
            ViewBag.CountMaintenance = allBranchRooms.Count(r => r.Status == "Maintenance");
            ViewBag.CountTotal = allBranchRooms.Count;

            // แสดงหน้าจอ Index
            return View(rooms);
        }

        // เมธอดสำหรับเปลี่ยนสถานะห้องพักด่วนจากหน้าการ์ดห้องพัก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickUpdateRoomStatus(int roomId, string newStatus)
        {
            // ค้นหาห้องพักในฐานข้อมูล
            var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
            {
                // ตรวจสอบสิทธิ์สาขา
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์แก้ไขสถานะห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(Index));
                }

                // ปรับสถานะห้องพักใหม่
                room.Status = newStatus;
                _context.SaveChanges();

                string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

                // หากปรับสถานะเป็น Cleaning ให้สร้างงานทำความสะอาดส่งให้แม่บ้านทันที
                if (newStatus == "Cleaning")
                {
                    HousekeepingService.CreateTask(
                        room.RoomId,
                        room.RoomNo,
                        room.RoomType?.TypeName ?? "Standard",
                        room.Branch,
                        staffName,
                        "Normal",
                        "พนักงานต้อนรับตั้งสถานะเป็น 'รอทำความสะอาด'",
                        null,
                        "FrontDesk"
                    );
                }
                // หากปรับสถานะเป็น Available ให้ปิดงานแม่บ้านที่ค้างอยู่ของห้องนี้
                else if (newStatus == "Available")
                {
                    var activeTask = HousekeepingService.GetAllTasks(room.Branch)
                        .FirstOrDefault(t => t.RoomId == roomId && t.Status != "Completed");
                    if (activeTask != null)
                    {
                        HousekeepingService.CompleteCleaning(activeTask.Id, staffName, "พนักงานต้อนรับปลดล็อกสถานะห้องเป็นว่างพร้อมขายต่อ");
                    }
                }

                TempData["SuccessMessage"] = $"อัปเดตสถานะห้อง {room.RoomNo} เป็น '{newStatus}' เรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(Index));
        }

        // เมธอดส่งงานแจ้งแม่บ้านทำความสะอาดห้องพักด่วน
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestCleaning(int roomId, string priority = "Normal", string? notes = null)
        {
            // ค้นหาห้องพัก
            var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
            {
                // ตรวจสอบสิทธิ์สาขา
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ส่งงานห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(Index));
                }

                // ปรับสถานะห้องเป็น Cleaning
                room.Status = "Cleaning";
                _context.SaveChanges();

                string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงานต้อนรับ";
                // สร้างงานทำความสะอาดระบุระดับความด่วน (Normal, Urgent, VIP) และหมายเหตุ
                HousekeepingService.CreateTask(
                    room.RoomId,
                    room.RoomNo,
                    room.RoomType?.TypeName ?? "Standard",
                    room.Branch,
                    staffName,
                    priority: string.IsNullOrEmpty(priority) ? "Normal" : priority,
                    notes: string.IsNullOrEmpty(notes) ? "พนักงานต้อนรับแจ้งทำความสะอาดห้องพัก" : notes,
                    guestName: null,
                    source: "FrontDesk"
                );

                TempData["SuccessMessage"] = $"ส่งงานแจ้งทำความสะอาดห้อง {room.RoomNo} ไปยังฝั่งแม่บ้านเรียบร้อยแล้ว";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่พบห้องพักที่ระบุ";
            }

            // นำทางกลับหน้าเดิม
            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(Index));
        }

        // หน้าจอเพิ่มห้องพักใหม่
        public IActionResult CreateRoom()
        {
            string currentBranch = GetCurrentBranch();
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
            var room = new Room
            {
                Branch = currentBranch,
                Status = "Available"
            };
            return View(room);
        }

        // บันทึกห้องพักใหม่ลงในฐานข้อมูล MySQL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRoom(Room newRoom)
        {
            if (string.IsNullOrEmpty(newRoom.Branch))
            {
                newRoom.Branch = GetCurrentBranch();
            }

            if (ModelState.IsValid)
            {
                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"เพิ่มห้อง {newRoom.RoomNo} เรียบร้อยแล้ว";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.RoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();
            return View(newRoom);
        }

        // หน้าจอแก้ไขข้อมูลห้องพัก
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

        // บันทึกการแก้ไขข้อมูลห้องพัก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRoom(Room updatedRoom)
        {
            var roomInDb = _context.Rooms.Find(updatedRoom.RoomId);
            if (roomInDb != null)
            {
                roomInDb.Status = updatedRoom.Status;
                if (!string.IsNullOrEmpty(updatedRoom.RoomNo)) roomInDb.RoomNo = updatedRoom.RoomNo;
                if (updatedRoom.RoomTypeId.HasValue) roomInDb.RoomTypeId = updatedRoom.RoomTypeId;
                if (!string.IsNullOrEmpty(updatedRoom.Branch)) roomInDb.Branch = updatedRoom.Branch;
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"แก้ไขข้อมูลห้อง {roomInDb.RoomNo} สำเร็จ";
                return RedirectToAction(nameof(Index));
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
                TempData["SuccessMessage"] = "ลบห้องพักออกจากระบบแล้ว";
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. ระบบเปิดห้องพักหน้าเคาน์เตอร์สำหรับลูกค้า Walk-in
        [HttpGet]
        public IActionResult WalkinBooking(string? branch = null, int? preselectedRoomId = null)
        {
            // ดึงสาขาปัจจุบัน
            string currentBranch = GetCurrentBranch(branch);

            // ดึงเฉพาะห้องที่ว่าง (Available) ในสาขานั้น
            var availableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available" && r.Branch == currentBranch)
                .OrderBy(r => r.RoomTypeId)
                .ThenBy(r => r.RoomNo)
                .ToList();

            // ดึงประเภทห้องทั้งหมด
            var roomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

            ViewBag.AvailableRooms = availableRooms;
            ViewBag.RoomTypes = roomTypes;
            ViewBag.PreselectedRoomId = preselectedRoomId;

            // กำหนดค่าเริ่มต้นของแบบฟอร์ม WalkinViewModel
            var model = new WalkinViewModel
            {
                CheckInDate = DateOnly.FromDateTime(DateTime.Today),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                NumberOfGuests = 2,
                PaymentMethod = "Cash"
            };

            // หากมีการเลือกห้องมาจากหน้าอื่น
            if (preselectedRoomId.HasValue)
            {
                model.RoomId = preselectedRoomId.Value;
            }

            // แสดงหน้าจอ WalkinBooking
            return View(model);
        }

        // บันทึกการเปิดห้องพัก Walk-in พร้อมคิดเงินค่าห้องและบริการเสริม Add-on
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WalkinBooking(WalkinViewModel model)
        {
            string currentBranch = GetCurrentBranch();

            // ตรวจสอบความถูกต้องของข้อมูล
            if (ModelState.IsValid)
            {
                // ตรวจสอบระบบ Fallback คำนวณราคาฝั่ง Server ป้องกันยอดเป็น 0 บาท
                if (model.TotalPrice <= 0)
                {
                    var selectedRoom = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
                    decimal basePrice = selectedRoom?.RoomType?.BasePrice ?? 1200;
                    int nights = model.CheckOutDate.DayNumber - model.CheckInDate.DayNumber;
                    if (nights <= 0) nights = 1;
                    decimal calculated = basePrice * nights;
                    // คิดค่าอาหารเช้า
                    if (model.AddBreakfast) calculated += (300 * nights);
                    // คิดค่าเตียงเสริม
                    if (model.AddExtraBed) calculated += (500 * nights);
                    // คิดค่ารถรับส่ง
                    if (model.AddAirportTransfer) calculated += 800;
                    // คิดค่าเลทเช็คเอาท์
                    if (model.AddLateCheckout) calculated += 300;
                    model.TotalPrice = calculated;
                }

                // ค้นหาบัญชีลูกค้าจากเลขบัตรประชาชนหรืออีเมล
                var guest = _context.Users.FirstOrDefault(u => u.IdCardOrPassport == model.IdCardOrPassport);
                if (guest == null && !string.IsNullOrEmpty(model.Email))
                {
                    guest = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                }

                // หากยังไม่มีบัญชี ให้สร้างบัญชีลูกค้าใหม่
                if (guest == null)
                {
                    guest = new User
                    {
                        FullName = model.GuestName,
                        Phone = model.Phone,
                        IdCardOrPassport = model.IdCardOrPassport,
                        Email = string.IsNullOrEmpty(model.Email) ? $"walkin_{DateTime.Now.Ticks}@easystay.com" : model.Email,
                        Password = "WalkinUser123!",
                        Role = "Customer",
                        Branch = currentBranch
                    };
                    _context.Users.Add(guest);
                    _context.SaveChanges();
                }

                // สร้างข้อมูลการจองในตาราง Bookings ด้วยรหัส WK-...
                var newBooking = new Booking
                {
                    BookingRef = "WK-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    UserId = guest.UserId,
                    GuestName = model.GuestName,
                    RoomId = model.RoomId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    TotalPrice = model.TotalPrice,
                    BookingStatus = "CheckIn"
                };
                _context.Bookings.Add(newBooking);
                _context.SaveChanges();

                // สร้างข้อมูลการชำระเงินในตาราง Payments
                var newPayment = new Payment
                {
                    BookingId = newBooking.BookingId,
                    Amount = model.TotalPrice,
                    TransferDate = DateTime.Now,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = "Paid"
                };
                _context.Payments.Add(newPayment);

                // ปรับสถานะห้องพักเป็น Occupied (มีแขกพัก) ทันที
                var room = _context.Rooms.Find(model.RoomId);
                if (room != null)
                {
                    room.Status = "Occupied";
                }

                // บันทึกข้อมูลลงฐานข้อมูล MySQL
                _context.SaveChanges();

                // แจ้งเตือนความสำเร็จและนำไปยังหน้าใบเสร็จ
                TempData["SuccessMessage"] = $"บันทึกการเข้าพักแบบ Walk-in สำเร็จ (รหัสอ้างอิง: {newBooking.BookingRef})";
                return RedirectToAction(nameof(BookingReceipt), new { id = newBooking.BookingId });
            }

            // กรณีข้อมูลไม่ถูกต้อง โหลดห้องพักว่างกลับมาแสดงใหม่
            ViewBag.AvailableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available" && r.Branch == currentBranch)
                .ToList();
            ViewBag.RoomTypes = _context.Roomtypes.ToList();
            return View(model);
        }

        // 5. หน้ารายชื่อการจองทั้งหมดในสาขา รองรับการกรองตามสถานะและการค้นหา
        [HttpGet]
        public IActionResult BookingList(string? tab = null, string? search = null, string? branch = null)
        {
            // ดึงสาขาที่กำลังดูข้อมูล
            string currentBranch = GetCurrentBranch(branch);

            // คิวรีดึงรายการจองในสาขานี้พร้อมข้อมูลห้องพัก ผู้ใช้ และการชำระเงิน
            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch)
                .AsQueryable();

            // กรองตามคำค้นหา (รหัสจอง ชื่อแขก เบอร์โทร หรือเลขห้อง)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(b =>
                    b.BookingRef.Contains(s) ||
                    (b.GuestName != null && b.GuestName.Contains(s)) ||
                    (b.User != null && (b.User.Phone!.Contains(s) || b.User.Email.Contains(s))) ||
                    (b.Room != null && b.Room.RoomNo.Contains(s))
                );
            }

            // ดึงข้อมูลการจองทั้งหมดเรียงตาม BookingId จากใหม่ไปเก่า
            var allBranchBookings = query.OrderByDescending(b => b.BookingId).ToList();

            // นับจำนวนการจองในแต่ละแท็บ
            var today = DateOnly.FromDateTime(DateTime.Today);
            int countAll = allBranchBookings.Count;
            int countPending = allBranchBookings.Count(b => b.BookingStatus == "Pending" || b.BookingStatus == "Confirmed" || b.BookingStatus == "Reserved");
            int countInHouse = allBranchBookings.Count(b => b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn");
            int countCompleted = allBranchBookings.Count(b => b.BookingStatus == "Completed");

            // กรองข้อมูลตามแท็บที่เลือก (All, Pending, InHouse, Completed)
            string activeTab = tab ?? "All";
            List<Booking> filteredList;
            switch (activeTab)
            {
                case "Pending":
                    filteredList = allBranchBookings.Where(b => b.BookingStatus == "Pending" || b.BookingStatus == "Confirmed" || b.BookingStatus == "Reserved").ToList();
                    break;
                case "InHouse":
                    filteredList = allBranchBookings.Where(b => b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn").ToList();
                    break;
                case "Completed":
                    filteredList = allBranchBookings.Where(b => b.BookingStatus == "Completed").ToList();
                    break;
                default:
                    filteredList = allBranchBookings;
                    activeTab = "All";
                    break;
            }

            // ส่งข้อมูลสรุปไปยัง View
            ViewBag.ActiveTab = activeTab;
            ViewBag.SearchText = search;
            ViewBag.CountAll = countAll;
            ViewBag.CountPending = countPending;
            ViewBag.CountInHouse = countInHouse;
            ViewBag.CountCompleted = countCompleted;

            // ส่งรายการห้องว่างสำหรับใช้ใน Modal จัดห้องตอนเช็คอิน
            ViewBag.AvailableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch && r.Status == "Available")
                .ToList();

            // แสดงหน้ารายชื่อการจอง BookingList
            return View(filteredList);
        }

        // หน้าแสดงเอกสารยืนยันและใบเสร็จรับเงินขนาดมาตรฐาน A4 (BookingReceipt)
        public IActionResult BookingReceipt(int id)
        {
            // ค้นหาข้อมูลการจอง
            var booking = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // ตรวจสอบสิทธิ์สาขา
            string currentBranch = GetCurrentBranch();
            var role = HttpContext.Session.GetString("Role");
            if (role == "Staff" && booking.Room != null && booking.Room.Branch != currentBranch)
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ดูใบเสร็จรับเงินของสาขาอื่น";
                return RedirectToAction(nameof(BookingList));
            }

            // ดึงรายการชำระเงินล่าสุด
            var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()
                          ?? _context.Payments.FirstOrDefault(p => p.BookingId == id);
            ViewBag.Payment = payment;

            // แสดงหน้าจอใบเสร็จ
            return View(booking);
        }

        // ยืนยันการรับชำระเงินหน้าเคาน์เตอร์ (เงินสด หรือเงินโอน)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(int bookingId, string paymentMethod = "Cash")
        {
            // ค้นหารายการจอง
            var booking = _context.Bookings
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการยืนยันการชำระเงิน";
                return RedirectToAction(nameof(BookingList));
            }

            // ค้นหารายการชำระเงินเดิม
            var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()
                          ?? _context.Payments.FirstOrDefault(p => p.BookingId == bookingId);

            // หากมีรายการเดิม ให้อัปเดตสถานะเป็น Paid
            if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    payment.PaymentMethod = paymentMethod;
                }
                if (payment.TransferDate == null)
                {
                    payment.TransferDate = DateTime.Now;
                }
            }
            // หากยังไม่มี ให้สร้างรายการชำระเงินใหม่
            else
            {
                payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = booking.TotalPrice,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Paid",
                    TransferDate = DateTime.Now
                };
                _context.Payments.Add(payment);
            }

            // ปรับสถานะการจองจาก Pending เป็น Confirmed
            if (booking.BookingStatus == "Pending")
            {
                booking.BookingStatus = "Confirmed";
            }

            // บันทึกข้อมูลลงฐานข้อมูล MySQL
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"บันทึกยืนยันการรับชำระเงินสำเร็จ (รหัส {booking.BookingRef} ยอด ฿{booking.TotalPrice:N2})";

            // นำทางกลับหน้าเดิม
            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(BookingReceipt), new { id = bookingId });
        }

        // 6. ส่งรายงานปิดรอบกะและยอดเงินสดประจำวันให้ฝ่ายบัญชีตรวจสอบ
        [HttpGet]
        public IActionResult AccountingFeedback(string? branch = null)
        {
            // ดึงสาขาปัจจุบัน
            string currentBranch = GetCurrentBranch(branch);
            var today = DateOnly.FromDateTime(DateTime.Today);
            DateTime startOfDay = today.ToDateTime(TimeOnly.MinValue);
            DateTime endOfDay = today.ToDateTime(TimeOnly.MaxValue);

            // ดึงรายการชำระเงินในวันนี้ของสาขานี้
            var paymentsToday = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Where(p => p.Booking != null && p.Booking.Room != null && p.Booking.Room.Branch == currentBranch && p.TransferDate >= startOfDay && p.TransferDate <= endOfDay && (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed"))
                .ToList();

            // คำนวณยอดเงินสดรับจริงในกะ
            decimal cashTotal = paymentsToday.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            // คำนวณยอดเงินโอนดิจิทัล
            decimal transferTotal = paymentsToday.Where(p => p.PaymentMethod == "PromptPay" || p.PaymentMethod == "CreditCard").Sum(p => p.Amount);

            // นับจำนวนแขกที่เช็คอินในวันนี้
            int checkInsToday = _context.Bookings
                .Count(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckInDate == today && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"));

            // นับจำนวนแขกที่เช็คเอาท์ในวันนี้
            int checkOutsToday = _context.Bookings
                .Count(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckOutDate == today && b.BookingStatus == "Completed");

            // ส่งค่ายอดเงินและจำนวนสถิติไปยัง View
            ViewBag.SuggestedCash = cashTotal;
            ViewBag.SuggestedTransfer = transferTotal;
            ViewBag.CheckInsCount = checkInsToday;
            ViewBag.CheckOutsCount = checkOutsToday;
            ViewBag.RecentFeedbacks = AccountingFeedbackService.GetAll(currentBranch);

            // สร้างโมเดล AccountingFeedback พร้อมค่าแนะนำ
            var model = new AccountingFeedback
            {
                Branch = currentBranch,
                StaffName = HttpContext.Session.GetString("FullName") ?? "พนักงานต้อนรับ",
                CashAmount = cashTotal,
                TransferAmount = transferTotal,
                TotalCheckIns = checkInsToday,
                TotalCheckOuts = checkOutsToday,
                Shift = DateTime.Now.Hour >= 18 || DateTime.Now.Hour < 6 ? "รอบค่ำ/ปิดกะ (Night Shift)" : (DateTime.Now.Hour >= 12 ? "รอบบ่าย (Afternoon Shift)" : "รอบเช้า (Morning Shift)")
            };

            // แสดงหน้าจอ AccountingFeedback
            return View(model);
        }

        // บันทึกและส่งรายงานปิดกะไปยังฝ่ายบัญชี
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AccountingFeedback(AccountingFeedback model)
        {
            if (ModelState.IsValid)
            {
                // บันทึกวันเวลาที่ส่งรายงาน
                model.CreatedAt = DateTime.Now;
                model.StaffName = HttpContext.Session.GetString("FullName") ?? model.StaffName ?? "พนักงานต้อนรับ";
                model.Branch = GetCurrentBranch(model.Branch);

                // บันทึกข้อมูลลง JSON Micro-Datastore ผ่าน AccountingFeedbackService
                AccountingFeedbackService.Add(model);

                TempData["SuccessMessage"] = "ส่งรายงานและ Feedback ถึงฝ่ายบัญชีเรียบร้อยแล้ว!";
                return RedirectToAction(nameof(AccountingFeedback));
            }

            ViewBag.RecentFeedbacks = AccountingFeedbackService.GetAll(model.Branch);
            return View(model);
        }
    }
}
