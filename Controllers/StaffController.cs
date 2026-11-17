using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using EasyStayHotel.Models;
using EasyStayHotel.Models.ViewModels;
using EasyStayHotel.Services;

namespace EasyStayHotel.Controllers
{
    public class StaffController : Controller
    {
        private readonly EasystaydbContext _context;

        public StaffController(EasystaydbContext context)
        {
            _context = context;
        }

        // ตรวจสอบสิทธิ์การเข้าถึงระบบพนักงาน (Staff, Admin, SuperAdmin)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            // 1. ยังไม่ได้เข้าสู่ระบบ -> ส่งไปหน้า Login
            if (userId == null || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบพนักงาน";
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // 2. ถ้าเป็นพนักงานแม่บ้าน (Housekeeper) พยายามเข้าหน้าพนักงานต้อนรับ -> ส่งกลับหน้าแดชบอร์ดแม่บ้าน
            if (role == "Housekeeper" || role == "Maid")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงานต้อนรับ สงวนสิทธิ์เฉพาะพนักงานต้อนรับและผู้ดูแลระบบเท่านั้น เพื่อป้องกันการปฏิบัติงานข้ามหน้าที่";
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
                return;
            }

            // 3. ฝ่ายบัญชี (Accounting) พยายามเข้าหน้า Staff
            if (role == "Accounting")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงานต้อนรับ";
                context.Result = new RedirectToActionResult("RevenueReport", "Accounting", null);
                return;
            }

            // 4. ผู้ใช้งานทั่วไป (Customer) หรือบทบาทอื่นที่ไม่มีสิทธิ์เข้าถึงหน้าพนักงาน
            if (role != "Staff" && role != "Admin" && role != "SuperAdmin")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบพนักงาน สงวนสิทธิ์เฉพาะพนักงานและผู้ดูแลระบบเท่านั้น";
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }

        private string GetCurrentBranch(string? branch = null)
        {
            var role = HttpContext.Session.GetString("Role");
            string userBranch = HttpContext.Session.GetString("Branch") ?? "Bangkok";

            // กฎความปลอดภัย: พนักงานต้อนรับ (Staff) จะเห็นและจัดการได้เฉพาะสาขาตนเองเท่านั้น ห้ามสลับไปดูสาขาอื่น
            if (role == "Staff")
            {
                ViewBag.CurrentBranch = userBranch;
                ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(userBranch);
                ViewBag.IsBranchLocked = true;
                return userBranch;
            }

            // สำหรับผู้ดูแลระบบ (Admin, SuperAdmin) สามารถสลับดูสาขาอื่นได้
            string currentBranch = userBranch;
            if (!string.IsNullOrEmpty(branch))
            {
                currentBranch = branch;
                HttpContext.Session.SetString("Branch", branch);
            }
            ViewBag.CurrentBranch = currentBranch;
            ViewBag.CurrentBranchTh = RoomPreviewHelper.GetBranchNameTh(currentBranch);
            ViewBag.IsBranchLocked = false;
            return currentBranch;
        }

        // ==========================================
        // 1. ภาพรวมวันต่อวัน (Daily Overview / Dashboard)
        // ==========================================
        [HttpGet]
        public IActionResult DailyOverview(string? date = null, string? branch = null)
        {
            string currentBranch = GetCurrentBranch(branch);

            DateOnly targetDate;
            if (!DateOnly.TryParse(date, out targetDate))
            {
                targetDate = DateOnly.FromDateTime(DateTime.Today);
            }

            ViewBag.TargetDate = targetDate;
            ViewBag.TargetDateStr = targetDate.ToString("yyyy-MM-dd");
            ViewBag.TargetDateTh = targetDate.ToString("dd MMMM yyyy");
            ViewBag.IsToday = targetDate == DateOnly.FromDateTime(DateTime.Today);

            // 1. แขกจะเช็คอินในวันนี้ (Arrivals)
            var arrivalsToday = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckInDate == targetDate && b.BookingStatus != "Completed")
                .OrderBy(b => b.BookingStatus)
                .ToList();

            // 2. แขกจะเช็คเอาท์ในวันนี้ (Departures)
            var departuresToday = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckOutDate == targetDate && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"))
                .ToList();

            // 3. แขกที่พักอยู่ในโรงแรมปัจจุบัน (In-House)
            var inHouseBookings = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"))
                .ToList();

            // 4. สถิติห้องพักสาขานี้
            var rooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch)
                .ToList();

            int totalRooms = rooms.Count;
            int availableRooms = rooms.Count(r => r.Status == "Available");
            int occupiedRooms = rooms.Count(r => r.Status == "Occupied");
            int cleaningRooms = rooms.Count(r => r.Status == "Cleaning");
            int maintenanceRooms = rooms.Count(r => r.Status == "Maintenance");
            int reservedRooms = rooms.Count(r => r.Status == "Reserved");

            // 5. สรุปรายรับที่บันทึกของวันที่นี้
            DateTime startOfDay = targetDate.ToDateTime(TimeOnly.MinValue);
            DateTime endOfDay = targetDate.ToDateTime(TimeOnly.MaxValue);
            var paymentsToday = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Where(p => p.Booking != null && p.Booking.Room != null && p.Booking.Room.Branch == currentBranch && p.TransferDate >= startOfDay && p.TransferDate <= endOfDay)
                .ToList();

            decimal totalRevenue = paymentsToday.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed").Sum(p => p.Amount);
            decimal cashRevenue = paymentsToday.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            decimal promptPayRevenue = paymentsToday.Where(p => p.PaymentMethod == "PromptPay").Sum(p => p.Amount);

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

            return View();
        }

        // ==========================================
        // 2. เช็คอินหน้าเคาน์เตอร์ สำหรับการจองผ่านเว็บ (Check-in Desk & Room Assignment)
        // ==========================================
        [HttpGet]
        public IActionResult CheckInDesk(string? bookingRef = null, string? search = null, string? branch = null)
        {
            string currentBranch = GetCurrentBranch(branch);
            string queryText = !string.IsNullOrWhiteSpace(bookingRef) ? bookingRef.Trim() : (search?.Trim() ?? "");

            Booking? targetBooking = null;
            if (!string.IsNullOrEmpty(queryText))
            {
                targetBooking = _context.Bookings
                    .Include(b => b.Room)
                        .ThenInclude(r => r!.RoomType)
                    .Include(b => b.User)
                    .Include(b => b.Payments)
                    .FirstOrDefault(b => (b.BookingRef == queryText || (b.User != null && b.User.Phone == queryText) || b.BookingRef.Contains(queryText)) && b.Room != null && b.Room.Branch == currentBranch);

                // หากค้นหาตรงๆ ไม่พบ ลองค้นหาแบบกว้าง
                if (targetBooking == null)
                {
                    targetBooking = _context.Bookings
                        .Include(b => b.Room)
                            .ThenInclude(r => r!.RoomType)
                        .Include(b => b.User)
                        .Include(b => b.Payments)
                        .FirstOrDefault(b => (b.BookingRef.Contains(queryText) || (b.GuestName != null && b.GuestName.Contains(queryText))) && b.Room != null && b.Room.Branch == currentBranch);
                }

                if (targetBooking == null)
                {
                    ViewBag.SearchError = $"ไม่พบข้อมูลการจองที่ตรงกับ '{queryText}' ในสาขา{ViewBag.CurrentBranchTh}";
                }
            }

            // ดึงห้องว่างทั้งหมดของสาขานี้ เพื่อให้พนักงานเลือกจัดห้องให้ลูกค้าตอนเช็คอิน
            var availableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch && (r.Status == "Available" || (targetBooking != null && r.RoomId == targetBooking.RoomId)))
                .OrderBy(r => r.RoomTypeId)
                .ThenBy(r => r.RoomNo)
                .ToList();

            // ดึงรายการรอเช็คอินในวันนี้
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

            ViewBag.SearchQuery = queryText;
            ViewBag.TargetBooking = targetBooking;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.PendingArrivals = pendingArrivals;

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

            return View();
        }

        // POST: ยืนยันการเช็คอินและมอบกุญแจห้อง
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessCheckIn(int bookingId, int roomId, string? notes = null)
        {
            var booking = _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการเช็คอิน";
                return RedirectToAction(nameof(CheckInDesk));
            }

            var newRoom = _context.Rooms.Find(roomId);
            if (newRoom == null)
            {
                TempData["ErrorMessage"] = "ไม่พบห้องพักที่เลือก";
                return RedirectToAction(nameof(CheckInDesk), new { bookingRef = booking.BookingRef });
            }

            // ตรวจสอบสิทธิ์สาขา: พนักงานต้อนรับจัดการได้เฉพาะสาขาตนเองเท่านั้น
            string currentBranch = GetCurrentBranch();
            var role = HttpContext.Session.GetString("Role");
            if (role == "Staff" && (newRoom.Branch != currentBranch || (booking.Room != null && booking.Room.Branch != currentBranch)))
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์จัดการการเช็คอินของสาขาอื่น";
                return RedirectToAction(nameof(BookingList));
            }

            // หากมีการสลับห้อง ให้ปล่อยห้องเดิมกลับเป็น Available
            if (booking.RoomId != roomId && booking.Room != null)
            {
                booking.Room.Status = "Available";
            }

            // ผูกห้องใหม่และอัปเดตสถานะ
            booking.RoomId = roomId;
            booking.BookingStatus = "CheckIn";
            newRoom.Status = "Occupied";

            // ปรับสถานะการชำระเงินเป็น Paid เมื่อส่งมอบห้อง
            var payment = _context.Payments.FirstOrDefault(p => p.BookingId == bookingId);
            if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                if (payment.TransferDate == null) payment.TransferDate = DateTime.Now;
            }
            else
            {
                payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = booking.TotalPrice,
                    PaymentMethod = "PromptPay",
                    PaymentStatus = "Paid",
                    TransferDate = DateTime.Now
                };
                _context.Payments.Add(payment);
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"เช็คอินสำเร็จ! แขก: {booking.GuestName} เข้าพักห้อง {newRoom.RoomNo} เรียบร้อยแล้ว";
            return RedirectToAction(nameof(BookingReceipt), new { id = booking.BookingId });
        }

        // ==========================================
        // 3. เช็คเอาท์ (Check-out)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOutBooking(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking != null)
            {
                // ตรวจสอบสิทธิ์สาขา: พนักงานต้อนรับจัดการได้เฉพาะสาขาตนเองเท่านั้น
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && booking.Room != null && booking.Room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เช็คเอาท์ห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(BookingList));
                }

                booking.BookingStatus = "Completed";
                if (booking.Room != null)
                {
                    // เปลี่ยนสถานะห้องเป็นรอทำความสะอาด (Cleaning) ตามมาตรฐานโรงแรม
                    booking.Room.Status = "Cleaning";
                    _context.SaveChanges();

                    // ส่งต่องานทำความสะอาดไปยังแดชบอร์ดแม่บ้านทันที
                    string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงานต้อนรับ";
                    string guestName = booking.User?.FullName ?? "ลูกค้าทั่วไป";
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

                TempData["SuccessMessage"] = $"เช็คเอาท์แขกห้อง {(booking.Room != null ? booking.Room.RoomNo : "")} เรียบร้อยแล้ว (ส่งรายการทำความสะอาดไปยังฝั่งแม่บ้านแล้ว)";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการเช็คเอาท์";
            }

            // ถ้ามี Referer ให้กลับไปหน้าเดิม (เช่น BookingList หรือ Index หรือ DailyOverview)
            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(BookingList));
        }

        // GET Action เช็คอินแบบเร็วจากตาราง
        public IActionResult CheckInBooking(int id)
        {
            var booking = _context.Bookings.Include(b => b.Room).FirstOrDefault(b => b.BookingId == id);
            if (booking != null)
            {
                booking.BookingStatus = "CheckIn";
                if (booking.Room != null)
                {
                    booking.Room.Status = "Occupied";
                }
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"รับเช็คอินคุณ {booking.GuestName} เข้าห้อง {(booking.Room != null ? booking.Room.RoomNo : "")} เรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(BookingList));
        }

        // ==========================================
        // 4. หน้าจัดการสถานะห้องพัก (Room Status Management)
        // ==========================================
        public ActionResult Index(string? branch = null, string? filterStatus = null)
        {
            string currentBranch = GetCurrentBranch(branch);

            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch);

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "ALL")
            {
                query = query.Where(r => r.Status == filterStatus);
            }

            var rooms = query.OrderBy(r => r.RoomTypeId).ThenBy(r => r.RoomNo).ToList();

            // ดึงข้อมูลแขกที่พักอยู่ในแต่ละห้องแบบเรียลไทม์
            var activeBookings = _context.Bookings
                .Where(b => b.Room != null && b.Room.Branch == currentBranch && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn") && b.RoomId.HasValue)
                .GroupBy(b => b.RoomId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            ViewBag.ActiveBookings = activeBookings;
            ViewBag.FilterStatus = filterStatus ?? "ALL";

            // สรุปยอดจำนวนห้องตามสถานะ
            var allBranchRooms = _context.Rooms.Where(r => r.Branch == currentBranch).ToList();
            ViewBag.CountAvailable = allBranchRooms.Count(r => r.Status == "Available");
            ViewBag.CountOccupied = allBranchRooms.Count(r => r.Status == "Occupied");
            ViewBag.CountCleaning = allBranchRooms.Count(r => r.Status == "Cleaning");
            ViewBag.CountMaintenance = allBranchRooms.Count(r => r.Status == "Maintenance");
            ViewBag.CountTotal = allBranchRooms.Count;

            return View(rooms);
        }

        // อัปเดตสถานะห้องพักแบบรวดเร็วใน 1 คลิก (เช่น ทำความสะอาดเสร็จแล้ว -> ว่าง)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickUpdateRoomStatus(int roomId, string newStatus)
        {
            var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
            {
                // ตรวจสอบสิทธิ์สาขา: พนักงานจัดการได้เฉพาะสาขาตนเอง
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์แก้ไขสถานะห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(Index));
                }

                room.Status = newStatus;
                _context.SaveChanges();

                string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงาน";

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
                else if (newStatus == "Available")
                {
                    // ปลดล็อกงานแม่บ้านเดิม (ถ้ามี)
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

        // ส่งคำขอทำความสะอาดไปยังฝั่งแม่บ้าน (Send Cleaning Request to Housekeeping)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestCleaning(int roomId, string priority = "Normal", string? notes = null)
        {
            var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == roomId);
            if (room != null)
            {
                // ตรวจสอบสิทธิ์สาขา: พนักงานจัดการได้เฉพาะสาขาตนเอง
                string currentBranch = GetCurrentBranch();
                var role = HttpContext.Session.GetString("Role");
                if (role == "Staff" && room.Branch != currentBranch)
                {
                    TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ส่งงานห้องพักของสาขาอื่น";
                    return RedirectToAction(nameof(Index));
                }

                room.Status = "Cleaning";
                _context.SaveChanges();

                string staffName = HttpContext.Session.GetString("FullName") ?? "พนักงานต้อนรับ";
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

            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(Index));
        }

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

        // ==========================================
        // 5. ระบบ Walk-in สมบูรณ์แบบ (Walk-in Booking)
        // ==========================================
        [HttpGet]
        public IActionResult WalkinBooking(string? branch = null, int? preselectedRoomId = null)
        {
            string currentBranch = GetCurrentBranch(branch);

            // ดึงเฉพาะห้องที่ "ว่าง" และ "อยู่ในสาขานี้"
            var availableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available" && r.Branch == currentBranch)
                .OrderBy(r => r.RoomTypeId)
                .ThenBy(r => r.RoomNo)
                .ToList();

            var roomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

            ViewBag.AvailableRooms = availableRooms;
            ViewBag.RoomTypes = roomTypes;
            ViewBag.PreselectedRoomId = preselectedRoomId;

            var model = new WalkinViewModel
            {
                CheckInDate = DateOnly.FromDateTime(DateTime.Today),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                NumberOfGuests = 2,
                PaymentMethod = "Cash"
            };

            if (preselectedRoomId.HasValue)
            {
                model.RoomId = preselectedRoomId.Value;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WalkinBooking(WalkinViewModel model)
        {
            string currentBranch = GetCurrentBranch();

            if (ModelState.IsValid)
            {
                // คำนวณยอดเงินสำรองหากส่งมาเป็น 0
                if (model.TotalPrice <= 0)
                {
                    var selectedRoom = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
                    decimal basePrice = selectedRoom?.RoomType?.BasePrice ?? 1200;
                    int nights = model.CheckOutDate.DayNumber - model.CheckInDate.DayNumber;
                    if (nights <= 0) nights = 1;
                    decimal calculated = basePrice * nights;
                    if (model.AddBreakfast) calculated += (300 * nights);
                    if (model.AddExtraBed) calculated += (500 * nights);
                    if (model.AddAirportTransfer) calculated += 800;
                    if (model.AddLateCheckout) calculated += 300;
                    model.TotalPrice = calculated;
                }
                // 1. จัดการข้อมูลลูกค้า (Guest)
                var guest = _context.Users.FirstOrDefault(u => u.IdCardOrPassport == model.IdCardOrPassport);
                if (guest == null && !string.IsNullOrEmpty(model.Email))
                {
                    guest = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                }

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

                // 2. จัดการข้อมูลการจอง (Booking)
                var newBooking = new Booking
                {
                    BookingRef = "WK-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    UserId = guest.UserId,
                    GuestName = model.GuestName,
                    RoomId = model.RoomId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    TotalPrice = model.TotalPrice,
                    BookingStatus = "CheckIn" // Walk-in เช็คอินเข้าห้องทันที
                };
                _context.Bookings.Add(newBooking);
                _context.SaveChanges();

                // 3. ระบบชำระเงิน (Payment)
                var newPayment = new Payment
                {
                    BookingId = newBooking.BookingId,
                    Amount = model.TotalPrice,
                    TransferDate = DateTime.Now,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = "Paid" // ชำระเงินหน้าเคาน์เตอร์แล้ว
                };
                _context.Payments.Add(newPayment);

                // 4. เปลี่ยนสถานะห้องพักเป็น Occupied
                var room = _context.Rooms.Find(model.RoomId);
                if (room != null)
                {
                    room.Status = "Occupied";
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = $"บันทึกการเข้าพักแบบ Walk-in สำเร็จ (รหัสอ้างอิง: {newBooking.BookingRef})";
                return RedirectToAction(nameof(BookingReceipt), new { id = newBooking.BookingId });
            }

            // กรณี ModelState ไม่ผ่าน โหลดห้องว่างกลับไปแสดงใหม่
            ViewBag.AvailableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available" && r.Branch == currentBranch)
                .ToList();
            ViewBag.RoomTypes = _context.Roomtypes.ToList();

            return View(model);
        }

        // ==========================================
        // 6. รายชื่อผู้เข้าพัก (Booking List) พร้อมระบบกรอง & ค้นหาอัจฉริยะ
        // ==========================================
        public IActionResult BookingList(string? tab = null, string? search = null, string? branch = null)
        {
            string currentBranch = GetCurrentBranch(branch);

            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.Room != null && b.Room.Branch == currentBranch)
                .AsQueryable();

            // ค้นหาตามคำค้นหา
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

            var allBranchBookings = query.OrderByDescending(b => b.BookingId).ToList();

            // สถิติตัวเลขในแท็บ
            var today = DateOnly.FromDateTime(DateTime.Today);
            int countAll = allBranchBookings.Count;
            int countPending = allBranchBookings.Count(b => b.BookingStatus == "Pending" || b.BookingStatus == "Confirmed" || b.BookingStatus == "Reserved");
            int countInHouse = allBranchBookings.Count(b => b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn");
            int countCompleted = allBranchBookings.Count(b => b.BookingStatus == "Completed");

            // กรองตามแท็บที่เลือก
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

            ViewBag.ActiveTab = activeTab;
            ViewBag.SearchText = search;
            ViewBag.CountAll = countAll;
            ViewBag.CountPending = countPending;
            ViewBag.CountInHouse = countInHouse;
            ViewBag.CountCompleted = countCompleted;

            // ดึงห้องว่างสาขานี้สำหรับใช้ใน Modal จัดห้องตอนเช็คอิน
            ViewBag.AvailableRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == currentBranch && r.Status == "Available")
                .ToList();

            return View(filteredList);
        }

        // ==========================================
        // 7. พิมพ์ใบเสร็จรับเงิน (Booking Receipt)
        // ==========================================
        public IActionResult BookingReceipt(int id)
        {
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

            // ตรวจสอบสิทธิ์สาขา: พนักงานต้อนรับดูได้เฉพาะสาขาตนเองเท่านั้น
            string currentBranch = GetCurrentBranch();
            var role = HttpContext.Session.GetString("Role");
            if (role == "Staff" && booking.Room != null && booking.Room.Branch != currentBranch)
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ดูใบเสร็จรับเงินของสาขาอื่น";
                return RedirectToAction(nameof(BookingList));
            }

            var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()
                          ?? _context.Payments.FirstOrDefault(p => p.BookingId == id);
            ViewBag.Payment = payment;

            return View(booking);
        }

        // ==========================================
        // ยืนยันการรับชำระเงิน (Confirm Payment Status to Paid)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(int bookingId, string paymentMethod = "Cash")
        {
            var booking = _context.Bookings
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบรายการจองที่ต้องการยืนยันการชำระเงิน";
                return RedirectToAction(nameof(BookingList));
            }

            var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()
                          ?? _context.Payments.FirstOrDefault(p => p.BookingId == bookingId);

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

            if (booking.BookingStatus == "Pending")
            {
                booking.BookingStatus = "Confirmed";
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"บันทึกยืนยันการรับชำระเงินสำเร็จ (รหัส {booking.BookingRef} ยอด ฿{booking.TotalPrice:N2})";

            string? referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(BookingReceipt), new { id = bookingId });
        }

        // ==========================================
        // 8. ส่ง Feedback / รายงานกะ ไปยังฝ่ายบัญชี (Send Feedback To Accounting)
        // ==========================================
        [HttpGet]
        public IActionResult AccountingFeedback(string? branch = null)
        {
            string currentBranch = GetCurrentBranch(branch);
            var today = DateOnly.FromDateTime(DateTime.Today);
            DateTime startOfDay = today.ToDateTime(TimeOnly.MinValue);
            DateTime endOfDay = today.ToDateTime(TimeOnly.MaxValue);

            // คำนวณยอดเงินสดและเงินโอนของวันนี้ในสาขานี้
            var paymentsToday = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Where(p => p.Booking != null && p.Booking.Room != null && p.Booking.Room.Branch == currentBranch && p.TransferDate >= startOfDay && p.TransferDate <= endOfDay && (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed"))
                .ToList();

            decimal cashTotal = paymentsToday.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            decimal transferTotal = paymentsToday.Where(p => p.PaymentMethod == "PromptPay" || p.PaymentMethod == "CreditCard").Sum(p => p.Amount);

            int checkInsToday = _context.Bookings
                .Count(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckInDate == today && (b.BookingStatus == "CheckIn" || b.BookingStatus == "CheckedIn"));

            int checkOutsToday = _context.Bookings
                .Count(b => b.Room != null && b.Room.Branch == currentBranch && b.CheckOutDate == today && b.BookingStatus == "Completed");

            ViewBag.SuggestedCash = cashTotal;
            ViewBag.SuggestedTransfer = transferTotal;
            ViewBag.CheckInsCount = checkInsToday;
            ViewBag.CheckOutsCount = checkOutsToday;
            ViewBag.RecentFeedbacks = AccountingFeedbackService.GetAll(currentBranch);

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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AccountingFeedback(AccountingFeedback model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.StaffName = HttpContext.Session.GetString("FullName") ?? model.StaffName ?? "พนักงานต้อนรับ";
                model.Branch = GetCurrentBranch(model.Branch);

                AccountingFeedbackService.Add(model);

                TempData["SuccessMessage"] = "ส่งรายงานและ Feedback ถึงฝ่ายบัญชีเรียบร้อยแล้ว!";
                return RedirectToAction(nameof(AccountingFeedback));
            }

            ViewBag.RecentFeedbacks = AccountingFeedbackService.GetAll(model.Branch);
            return View(model);
        }
    }
}
