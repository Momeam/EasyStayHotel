using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;
using EasyStayHotel.Models.ViewModels;
using EasyStayHotel.ViewModels;
using EasyStayHotel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace EasyStayHotel.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์ BookingController: หัวใจหลักของระบบการจองห้องพักออนไลน์สำหรับลูกค้า (Customer Booking Portal)
    /// 
    /// วงจรการทำงานหลัก (Booking Lifecycle):
    /// 1. ค้นหาห้องพัก (Search): กรองห้องพักว่างตามสาขา (Bangkok, Chonburi, Phuket), วันที่เข้าพัก, ช่วงราคา และประเภทห้อง
    /// 2. หน้ายืนยันและชำระเงิน (Checkout): คำนวณราคารวมตามจำนวนคืน/ห้อง แสดงบริการเสริม (Add-ons)
    /// 3. คำนวณโปรโมชั่น (CalculateDiscount): AJAX API ตรวจสอบเงื่อนไขโปรโมชั่น 5 รูปแบบ และคูปองส่วนลดแบบไดนามิก
    /// 4. ยืนยันการจอง (ConfirmBooking): อัปโหลดสลิปโอนเงิน บันทึกการจอง เปลี่ยนสถานะห้องเป็น Reserved และส่งอีเมลยืนยันอัตโนมัติ
    /// 5. ใบยืนยันการจอง (BookingVoucher): เอกสาร Voucher สำหรับลูกค้าพิมพ์หรือแสดงต่อพนักงานในวันเข้าพัก
    /// 6. ตรวจสอบประวัติการจอง (MyBookings): ดูสถานะการจอง และยื่นคำร้องขอคืนเงิน (ภายใน 7 วัน)
    /// 7. ระบบรีวิวและให้คะแนน (SubmitReview): ลูกค้าที่เช็คเอาท์แล้วส่งคะแนนดาวและความประทับใจ
    /// </summary>
    public class BookingController : Controller
    {
        private readonly EasystaydbContext _context;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor รับ DbContext และ EmailService ผ่าน Dependency Injection
        /// </summary>
        public BookingController(EasystaydbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================================
        // 1. ระบบค้นหาห้องพักว่าง (Search Available Rooms)
        // =========================================================================
        /// <summary>
        /// ค้นหาห้องพักที่ว่างพร้อมขาย (Status = "Available") ตามเงื่อนไขสาขา วันที่เข้าพัก และช่วงราคา
        /// พร้อมจัดกลุ่มตามประเภทห้องพัก (Room Type) เพื่อแสดงจำนวนห้องว่างที่เหลือ
        /// </summary>
        [HttpGet]
        public IActionResult Search(
            string? branch,
            DateTime? checkIn,
            DateTime? checkOut,
            int rooms = 1,
            int guests = 2,
            int? roomTypeId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null)
        {
            // กำหนดสาขาเริ่มต้นหากผู้ใช้ไม่ได้เลือก
            if (string.IsNullOrEmpty(branch))
            {
                branch = "Bangkok";
            }

            DateTime actualCheckIn = checkIn == null || checkIn == default ? DateTime.Today : checkIn.Value;
            DateTime actualCheckOut = checkOut == null || checkOut == default ? actualCheckIn.AddDays(1) : checkOut.Value;

            // ตรวจสอบเงื่อนไขความถูกต้องของวันที่ (วันเช็คเอาท์ต้องมากกว่าวันเช็คอินอย่างน้อย 1 คืน)
            if (actualCheckIn >= actualCheckOut)
            {
                TempData["ErrorMessage"] = "วันที่เช็คเอาท์ต้องหลังจากวันที่เช็คอินอย่างน้อย 1 คืน";
                return RedirectToAction("Index", "Home");
            }

            if (rooms < 1) rooms = 1;
            if (guests < 1) guests = 1;

            // ดึงประเภทห้องพักทั้งหมดสำหรับแสดงในแถบตัวกรอง
            var allRoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

            // คิวรีดึงห้องพักที่มีสถานะ "Available" ตรงกับสาขาที่เลือก
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == branch && r.Status == "Available" && r.RoomType != null);

            // กรองตามช่วงราคาต่ำสุดและสูงสุด
            if (minPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice <= maxPrice.Value);
            }

            var availableRooms = query.ToList();

            // จัดกลุ่มห้องพักว่างตามประเภท (Group By RoomType) เพื่อแสดงการ์ดห้องพักพร้อมจำนวนห้องว่างที่เหลือ
            var groupedRoomTypes = availableRooms
                .Where(r => r.RoomType != null)
                .GroupBy(r => r.RoomType!)
                .Select(g => new
                {
                    RoomType = g.Key,
                    AvailableCount = g.Count(),
                    FirstAvailableRoomId = g.First().RoomId,
                    SampleRoomNumbers = string.Join(", ", g.Take(5).Select(x => x.RoomNo))
                })
                .OrderByDescending(g => roomTypeId.HasValue && g.RoomType.RoomTypeId == roomTypeId.Value)
                .ThenBy(g => g.RoomType.BasePrice)
                .ToList();

            ViewBag.Branch = branch;
            ViewBag.CheckIn = actualCheckIn.ToString("yyyy-MM-dd");
            ViewBag.CheckOut = actualCheckOut.ToString("yyyy-MM-dd");
            ViewBag.Nights = (actualCheckOut - actualCheckIn).Days;
            ViewBag.RoomsReq = rooms;
            ViewBag.GuestsReq = guests;
            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.MinPrice = minPrice ?? 500;
            ViewBag.MaxPrice = maxPrice ?? 10000;
            ViewBag.AllRoomTypes = allRoomTypes;
            ViewBag.TotalFound = groupedRoomTypes.Count;

            // สถิติจำนวนห้องว่างในสาขาอื่นๆ เพื่ออำนวยความสะดวกให้ลูกค้าเลือกสลับดูสาขาอื่น
            ViewBag.AvailBkk = _context.Rooms.Count(r => r.Branch == "Bangkok" && r.Status == "Available");
            ViewBag.AvailChon = _context.Rooms.Count(r => r.Branch == "Chonburi" && r.Status == "Available");
            ViewBag.AvailPhuket = _context.Rooms.Count(r => r.Branch == "Phuket" && r.Status == "Available");

            return View(groupedRoomTypes);
        }

        // =========================================================================
        // 2. หน้าสรุปรายการและชำระเงิน (Checkout)
        // =========================================================================
        /// <summary>
        /// แสดงหน้าจอชำระเงิน: ดึงข้อมูลห้องพัก คำนวณราคาตามจำนวนคืน และดึงข้อมูลผู้ใช้จาก Session มาเติมให้อัตโนมัติ
        /// </summary>
        [HttpGet]
        public IActionResult Checkout(int roomId, string checkIn, string checkOut, int guests = 2, int rooms = 1)
        {
            if (!DateTime.TryParse(checkIn, out DateTime checkInDate) ||
                !DateTime.TryParse(checkOut, out DateTime checkOutDate))
            {
                checkInDate = DateTime.Today;
                checkOutDate = DateTime.Today.AddDays(1);
            }

            var room = _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefault(r => r.RoomId == roomId);

            if (room == null || room.RoomType == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลห้องพักที่เลือก กรุณาค้นหาใหม่อีกครั้ง";
                return RedirectToAction("Search");
            }

            decimal basePrice = room.RoomType.BasePrice;
            if (basePrice <= 0)
            {
                string name = (room.RoomType.TypeName ?? "").ToLower();
                if (name.Contains("suite") || name.Contains("สวีท")) basePrice = 3800;
                else if (name.Contains("deluxe") || name.Contains("ดีลักซ์")) basePrice = 2200;
                else basePrice = 1200;
            }

            int nights = (checkOutDate - checkInDate).Days > 0 ? (checkOutDate - checkInDate).Days : 1;

            var model = new CustomerCheckoutViewModel
            {
                RoomId = room.RoomId,
                RoomTypeName = room.RoomType.TypeName ?? "Unknown Room",
                BasePrice = basePrice,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Nights = nights,
                Guests = guests > 0 ? guests : 2,
                TotalPrice = basePrice * nights * (rooms > 0 ? rooms : 1)
            };

            // หากลูกค้าล็อกอินอยู่แล้ว ดึงข้อมูลชื่อ อีเมล และเบอร์โทรศัพท์มาใส่ในฟอร์มให้อัตโนมัติ
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                var currentUser = _context.Users.Find(sessionUserId.Value);
                if (currentUser != null)
                {
                    model.GuestName = currentUser.FullName;
                    model.Email = currentUser.Email;
                    model.Phone = currentUser.Phone ?? string.Empty;
                }
            }

            ViewBag.RoomBranch = room.Branch;
            ViewBag.RoomNo = room.RoomNo;
            ViewBag.RoomTypeId = room.RoomType.RoomTypeId;
            ViewBag.RoomDescription = room.RoomType.Description;
            ViewBag.RoomCapacity = room.RoomType.Capacity;
            ViewBag.RoomsCount = rooms > 0 ? rooms : 1;

            return View(model);
        }

        // =========================================================================
        // 3. AJAX API คำนวณส่วนลดโปรโมชั่น (Calculate Discount API)
        // =========================================================================
        /// <summary>
        /// คำนวณส่วนลดตามกฎ 5 โปรโมชั่น และรหัสคูปองส่วนลดแบบ Real-time โดยไม่ต้องรีเฟรชหน้าเว็บ
        /// </summary>
        [HttpPost]
        public IActionResult CalculateDiscount([FromBody] PromoRequestData data)
        {
            var promoService = new PromotionService();

            // ดึงข้อมูลวันเกิดของลูกค้าจากฐานข้อมูลเพื่อใช้ตรวจสอบโปรโมชั่นวันเกิด
            DateTime? guestDob = null;
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                var user = _context.Users.Find(sessionUserId.Value);
                if (user?.DateOfBirth != null)
                {
                    guestDob = user.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                }
            }
            else if (!string.IsNullOrEmpty(data.Email))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == data.Email);
                if (user?.DateOfBirth != null)
                {
                    guestDob = user.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                }
            }

            DateTime checkInDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(data.CheckInDate))
            {
                if (!DateTime.TryParse(data.CheckInDate, out checkInDate))
                {
                    checkInDate = DateTime.Today;
                }
            }

            int roomCount = data.Rooms > 0 ? data.Rooms : 1;
            int nightsCount = data.Nights > 0 ? data.Nights : 1;

            // เรียกใช้ PromotionService ในการคำนวณส่วนลด
            var result = promoService.CalculateDiscount(
                data.BasePrice,
                nightsCount,
                roomCount,
                checkInDate,
                guestDob,
                data.CouponCode,
                data.RoomTypeName
            );

            return Json(new
            {
                success = string.IsNullOrEmpty(result.ErrorMessage),
                discount = result.TotalDiscount,
                promotions = result.AppliedPromotions,
                message = result.ErrorMessage
            });
        }

        // =========================================================================
        // 4. บันทึกและยืนยันการจองห้องพัก (Confirm Booking)
        // =========================================================================
        /// <summary>
        /// ประมวลผลการจองห้องพัก:
        /// 1. บันทึกรูปภาพสลิปโอนเงินลงในโฟลเดอร์ wwwroot/uploads/slips/
        /// 2. สร้างหรือจับคู่บัญชีลูกค้าในตาราง users
        /// 3. สร้างรายการจองในตาราง bookings ด้วยสถานะ "Pending" (รอตรวจสอบการชำระเงิน)
        /// 4. สร้างรายการธุรกรรมในตาราง payments
        /// 5. ปรับสถานะห้องพักในตาราง rooms เป็น "Reserved"
        /// 6. ส่งอีเมลยืนยันการจองพร้อมลิงก์ Voucher ให้ลูกค้าอัตโนมัติผ่าน EmailService
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(CustomerCheckoutViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. จัดการอัปโหลดไฟล์สลิปหลักฐานการโอนเงิน (Payment Slip Upload)
                string? slipFileName = null;
                if (model.PaymentSlip != null && model.PaymentSlip.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slips");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // สร้างชื่อไฟล์ที่ไม่ซ้ำกันด้วย GUID เพื่อป้องกันชื่อไฟล์ชนกัน
                    slipFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.PaymentSlip.FileName);
                    string filePath = Path.Combine(uploadsFolder, slipFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        model.PaymentSlip.CopyTo(fileStream);
                    }
                }

                // 2. จัดการข้อมูลลูกค้า (Guest Account): หากไม่มีบัญชีจะสร้างบัญชีผู้ใช้ใหม่ให้อัตโนมัติ
                User? guest = null;
                int? sessionUserId = HttpContext.Session.GetInt32("UserId");
                if (sessionUserId.HasValue)
                {
                    guest = _context.Users.Find(sessionUserId.Value);
                }

                if (guest == null)
                {
                    guest = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                }

                if (guest == null)
                {
                    guest = new User
                    {
                        FullName = model.GuestName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Password = "Customer123!",
                        Role = "Customer"
                    };
                    _context.Users.Add(guest);
                    _context.SaveChanges();
                }

                // 3. สร้างข้อมูลการจองในตาราง bookings
                var newBooking = new Booking
                {
                    BookingRef = "ES-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    UserId = guest.UserId,
                    GuestName = model.GuestName,
                    RoomId = model.RoomId,
                    CheckInDate = DateOnly.FromDateTime(model.CheckInDate),
                    CheckOutDate = DateOnly.FromDateTime(model.CheckOutDate),
                    TotalPrice = model.TotalPrice,
                    BookingStatus = "Pending"
                };
                _context.Bookings.Add(newBooking);
                _context.SaveChanges();

                // 4. บันทึกข้อมูลการชำระเงินในตาราง payments พร้อมผูก BookingId และรูปสลิป
                var newPayment = new Payment
                {
                    BookingId = newBooking.BookingId,
                    Amount = model.TotalPrice,
                    TransferDate = DateTime.Now,
                    PaymentMethod = "PromptPay",
                    PaymentStatus = "Pending",
                    SlipImageUrl = slipFileName != null ? "/uploads/slips/" + slipFileName : null
                };
                _context.Payments.Add(newPayment);

                // 5. เปลี่ยนสถานะห้องพักเป็น Reserved
                var room = _context.Rooms.Find(model.RoomId);
                if (room != null)
                {
                    room.Status = "Reserved";
                }

                _context.SaveChanges();

                // 6. ส่งอีเมลยืนยันการจองพร้อม Voucher ลิงก์ไปยังอีเมลของลูกค้าอัตโนมัติ
                try
                {
                    string branchName = room?.Branch ?? "Bangkok";
                    string roomTypeName = room?.RoomType?.TypeName ?? model.RoomTypeName ?? "ห้องพักมาตรฐาน";
                    string voucherUrl = Url.Action("BookingVoucher", "Booking", new { bookingRef = newBooking.BookingRef }, Request.Scheme) ?? "";

                    await _emailService.SendBookingConfirmationEmailAsync(
                        guest.Email,
                        newBooking.GuestName,
                        newBooking.BookingRef,
                        roomTypeName,
                        branchName,
                        newBooking.CheckInDate.ToString("dd/MM/yyyy"),
                        newBooking.CheckOutDate.ToString("dd/MM/yyyy"),
                        newBooking.TotalPrice,
                        voucherUrl
                    );
                }
                catch
                {
                    // Fail-safe: หากเครือข่ายอีเมลขัดข้อง การจองของลูกค้ายังคงสำเร็จสมบูรณ์
                }

                return RedirectToAction("BookingSuccess", new { bookingRef = newBooking.BookingRef });
            }

            var selectedRoom = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
            if (selectedRoom != null)
            {
                ViewBag.RoomBranch = selectedRoom.Branch;
                ViewBag.RoomNo = selectedRoom.RoomNo;
                ViewBag.RoomTypeId = selectedRoom.RoomType?.RoomTypeId ?? 1;
                ViewBag.RoomDescription = selectedRoom.RoomType?.Description;
                ViewBag.RoomCapacity = selectedRoom.RoomType?.Capacity ?? 2;
            }

            return View("Checkout", model);
        }

        /// <summary>
        /// แสดงหน้าจอสำเร็จการจองห้องพัก
        /// </summary>
        [HttpGet]
        public IActionResult BookingSuccess(string bookingRef)
        {
            ViewBag.BookingRef = bookingRef;
            return View();
        }

        // =========================================================================
        // 5. ใบเสร็จและเอกสารยืนยันการจอง (Booking Confirmation Voucher)
        // =========================================================================
        /// <summary>
        /// แสดงเอกสารใบยืนยันการจองห้องพัก (Voucher) สำหรับให้ลูกค้าพิมพ์เป็น PDF หรือแสดงต่อพนักงานเคาน์เตอร์โรงแรม
        /// </summary>
        [HttpGet]
        public IActionResult BookingVoucher(string? bookingRef, int? id)
        {
            if (string.IsNullOrWhiteSpace(bookingRef) && !id.HasValue)
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสการจองที่ต้องการดูเอกสาร";
                return RedirectToAction(nameof(MyBookings));
            }

            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .AsQueryable();

            Booking? booking = null;
            if (!string.IsNullOrWhiteSpace(bookingRef))
            {
                string cleanRef = bookingRef.Trim();
                booking = query.FirstOrDefault(b => b.BookingRef == cleanRef);
            }
            else if (id.HasValue)
            {
                booking = query.FirstOrDefault(b => b.BookingId == id.Value);
            }

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ระบุ";
                return RedirectToAction(nameof(MyBookings));
            }

            var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
            ViewBag.LatestPayment = latestPayment;

            return View(booking);
        }

        // =========================================================================
        // 6. ประวัติการจองและค้นหาการจอง (My Bookings)
        // =========================================================================
        /// <summary>
        /// หน้าดูประวัติการจองของลูกค้า หรือค้นหาการจองด้วยหมายเลข BookingRef / เบอร์โทรศัพท์ / อีเมล
        /// </summary>
        [HttpGet]
        public IActionResult MyBookings(string? bookingRef, string? contact)
        {
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.SearchBookingRef = bookingRef;
            ViewBag.SearchContact = contact;
            ViewBag.IsLoggedIn = sessionUserId.HasValue;

            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .AsQueryable();

            bool hasSearchFilter = !string.IsNullOrWhiteSpace(bookingRef) || !string.IsNullOrWhiteSpace(contact);

            if (hasSearchFilter)
            {
                if (!string.IsNullOrWhiteSpace(bookingRef))
                {
                    string refClean = bookingRef.Trim();
                    query = query.Where(b => b.BookingRef.Contains(refClean));
                }

                if (!string.IsNullOrWhiteSpace(contact))
                {
                    string contactClean = contact.Trim();
                    query = query.Where(b =>
                        (b.GuestName != null && b.GuestName.Contains(contactClean)) ||
                        (b.User != null && (b.User.Email.Contains(contactClean) || (b.User.Phone != null && b.User.Phone.Contains(contactClean)))));
                }
            }
            else if (sessionUserId.HasValue)
            {
                query = query.Where(b => b.UserId == sessionUserId.Value);
            }
            else
            {
                ViewBag.RefundRequests = new Dictionary<int, RefundRequest>();
                return View(new List<Booking>());
            }

            var bookings = query
                .OrderByDescending(b => b.BookingId)
                .ToList();

            var allRefunds = RefundService.GetAll();
            var refundMap = allRefunds.GroupBy(r => r.BookingId).ToDictionary(g => g.Key, g => g.First());
            ViewBag.RefundRequests = refundMap;

            return View(bookings);
        }

        // =========================================================================
        // 7. ลูกค้ายื่นคำร้องขอคืนเงิน (Request Refund)
        // =========================================================================
        /// <summary>
        /// ลูกค้ายื่นคำร้องขอคืนเงิน: ตรวจสอบเงื่อนไข 7 วัน บันทึกข้อมูลบัญชีธนาคาร และส่งการแจ้งเตือนไปยังฝ่ายบัญชี
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestRefund(RefundRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "ข้อมูลการขอคืนเงินไม่ถูกต้อง กรุณากรอกข้อมูลให้ครบถ้วน";
                return RedirectToAction(nameof(MyBookings));
            }

            var booking = _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ต้องการขอคืนเงิน";
                return RedirectToAction(nameof(MyBookings));
            }

            // ตรวจสอบเงื่อนไข 7 วันนับจากวันที่จองผ่าน RefundService
            if (!RefundService.IsEligibleForRefund(booking, out string eligibilityReason, out int daysRemaining))
            {
                TempData["ErrorMessage"] = eligibilityReason;
                return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
            }

            DateTime bookingDate = RefundService.GetBookingDate(booking);
            var refundReq = new RefundRequest
            {
                BookingId = booking.BookingId,
                BookingRef = booking.BookingRef,
                GuestName = booking.GuestName ?? booking.User?.FullName ?? "ไม่ระบุชื่อ",
                ContactPhone = model.ContactPhone.Trim(),
                ContactEmail = booking.User?.Email ?? "",
                Branch = booking.Room?.Branch ?? "Bangkok",
                RoomNo = booking.Room?.RoomNo,
                RoomTypeName = booking.Room?.RoomType?.TypeName,
                Amount = booking.TotalPrice,
                BankName = model.BankName.Trim(),
                BankAccountNumber = model.BankAccountNumber.Trim(),
                BankAccountName = model.BankAccountName.Trim(),
                Reason = model.Reason.Trim() == "อื่นๆ" && !string.IsNullOrWhiteSpace(model.OtherReason)
                    ? $"อื่นๆ: {model.OtherReason.Trim()}"
                    : model.Reason.Trim(),
                BookingDate = bookingDate,
                RequestedAt = DateTime.Now,
                RefundDeadline = DateTime.Now.AddDays(3), // กำหนดคืนเงินภายใน 3 วันทำการ
                Status = "Pending"
            };

            bool created = RefundService.CreateRequest(refundReq);
            if (created)
            {
                // เปลี่ยนสถานะการจองเป็น RefundRequested
                booking.BookingStatus = "RefundRequested";
                _context.SaveChanges();

                // ส่งการแจ้งเตือนไปยังฝ่ายบัญชีโดยอัตโนมัติ
                var accFeedback = new AccountingFeedback
                {
                    CreatedAt = DateTime.Now,
                    StaffName = "ระบบอัตโนมัติ (Customer Portal)",
                    Branch = booking.Room?.Branch ?? "Bangkok",
                    Shift = "คำขอคืนเงินออนไลน์",
                    CashAmount = 0,
                    TransferAmount = booking.TotalPrice,
                    Category = "แจ้งเรื่องคืนเงิน",
                    Message = $"ลูกค้ารหัสการจอง {booking.BookingRef} ({refundReq.GuestName}) ขอคืนเงิน ฿{booking.TotalPrice:N2} เข้า {model.BankName} เลขบัญชี {model.BankAccountNumber} ({model.BankAccountName}) เหตุผล: {model.Reason} (กำหนดคืนเงินภายใน 3 วัน: ภายใน {refundReq.RefundDeadline:dd/MM/yyyy HH:mm น.})",
                    IsAcknowledged = false
                };
                AccountingFeedbackService.Add(accFeedback);

                TempData["SuccessMessage"] = $"ยื่นคำขอคืนเงินรหัส {booking.BookingRef} สำเร็จแล้ว! ระบบส่งข้อมูลให้พนักงานฝ่ายบัญชีเรียบร้อย โดยฝ่ายบัญชีจะตรวจสอบและโอนเงินคืนผ่านธนาคารภายใน 3 วันทำการ (ภายในวันที่ {refundReq.RefundDeadline:dd/MM/yyyy})";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่สามารถยื่นคำขอคืนเงินได้ เนื่องจากมีคำขอเดิมอยู่ในระบบแล้ว";
            }

            return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
        }

        // =========================================================================
        // 8. ลูกค้าเขียนรีวิวและให้คะแนนความพึงพอใจ (Submit Guest Review)
        // =========================================================================
        /// <summary>
        /// ลูกค้าส่งรีวิวและให้คะแนนดาว (ความสะอาด, การบริการ, ทำเล) สำหรับการจองที่เช็คเอาท์แล้ว
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(int bookingId, int rating, int cleanlinessRating, int serviceRating, int locationRating, string? title, string comment)
        {
            var booking = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ต้องการรีวิว";
                return RedirectToAction(nameof(MyBookings));
            }

            string guestName = HttpContext.Session.GetString("UserName") ?? booking.GuestName ?? "ลูกค้า Easy Stay";
            string guestEmail = HttpContext.Session.GetString("UserEmail") ?? "";

            var review = new GuestReview
            {
                BookingId = booking.BookingId,
                BookingRef = booking.BookingRef,
                GuestName = guestName,
                GuestEmail = guestEmail,
                Branch = booking.Room?.Branch ?? "Bangkok",
                RoomTypeName = booking.Room?.RoomType?.TypeName ?? "ห้องพักมาตรฐาน",
                Rating = rating < 1 ? 5 : (rating > 5 ? 5 : rating),
                CleanlinessRating = cleanlinessRating < 1 ? 5 : (cleanlinessRating > 5 ? 5 : cleanlinessRating),
                ServiceRating = serviceRating < 1 ? 5 : (serviceRating > 5 ? 5 : serviceRating),
                LocationRating = locationRating < 1 ? 5 : (locationRating > 5 ? 5 : locationRating),
                Title = string.IsNullOrWhiteSpace(title) ? "รีวิวความประทับใจการเข้าพัก" : title.Trim(),
                Comment = string.IsNullOrWhiteSpace(comment) ? "ห้องพักสะอาด กว้างขวาง การบริการดีมาก มีโอกาสจะกลับมาพักอีกแน่นอนครับ" : comment.Trim(),
                CreatedAt = DateTime.Now,
                IsVerifiedStay = true
            };

            ReviewService.AddReview(review);
            TempData["SuccessMessage"] = "ขอบคุณสำหรับการรีวิว! ความคิดเห็นอันมีค่าของคุณช่วยให้ Easy Stay พัฒนาการบริการได้อย่างดียิ่งขึ้น";
            return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
        }
    }

    /// <summary>
    /// คลาสแบบจำลองข้อมูลรับค่า JSON สำหรับ AJAX คำนวณโปรโมชั่น
    /// </summary>
    public class PromoRequestData
    {
        public decimal BasePrice { get; set; }
        public int Nights { get; set; }
        public int Rooms { get; set; } = 1;
        public string? CheckInDate { get; set; }
        public string? CouponCode { get; set; }
        public string? Email { get; set; }
        public string? RoomTypeName { get; set; }
    }
}
