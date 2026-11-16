using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;
using EasyStayHotel.Models.ViewModels;
using EasyStayHotel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace EasyStayHotel.Controllers
{
    public class BookingController : Controller
    {
        private readonly EasystaydbContext _context;

        public BookingController(EasystaydbContext context)
        {
            _context = context;
        }

        // GET: /Booking/Search
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
            // กำหนดค่าเริ่มต้นหากผู้ใช้กดเข้ามาจากปุ่มจองด่วนหรือเมนูโดยตรง
            if (string.IsNullOrEmpty(branch))
            {
                branch = "Bangkok";
            }

            DateTime actualCheckIn = checkIn == null || checkIn == default ? DateTime.Today : checkIn.Value;
            DateTime actualCheckOut = checkOut == null || checkOut == default ? actualCheckIn.AddDays(1) : checkOut.Value;

            if (actualCheckIn >= actualCheckOut)
            {
                TempData["ErrorMessage"] = "วันที่เช็คเอาท์ต้องหลังจากวันที่เช็คอินอย่างน้อย 1 คืน";
                return RedirectToAction("Index", "Home");
            }

            if (rooms < 1) rooms = 1;
            if (guests < 1) guests = 1;

            // ดึงประเภทห้องพักทั้งหมดสำหรับแสดงในตัวกรองด้านซ้าย
            var allRoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

            // ดึงข้อมูลห้องพักที่ "ว่าง" และ "ตรงกับสาขาที่เลือก"
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == branch && r.Status == "Available" && r.RoomType != null);

            if (minPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice <= maxPrice.Value);
            }

            var availableRooms = query.ToList();

            // จัดกลุ่มตามประเภทห้องพัก (RoomType)
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

            // จำนวนห้องว่างในสาขาต่างๆ เพื่อให้ลูกค้ากดสลับดูและจองสาขาอื่นได้สะดวก
            ViewBag.AvailBkk = _context.Rooms.Count(r => r.Branch == "Bangkok" && r.Status == "Available");
            ViewBag.AvailChon = _context.Rooms.Count(r => r.Branch == "Chonburi" && r.Status == "Available");
            ViewBag.AvailPhuket = _context.Rooms.Count(r => r.Branch == "Phuket" && r.Status == "Available");

            return View(groupedRoomTypes);
        }

        // GET: /Booking/Checkout
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

            // หากลูกค้าล็อกอินอยู่ ดึงข้อมูลชื่อ อีเมล เบอร์โทรมาเติมให้อัตโนมัติ
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

        // POST: /Booking/CalculateDiscount
        [HttpPost]
        public IActionResult CalculateDiscount([FromBody] PromoRequestData data)
        {
            var promoService = new PromotionService();

            // ดึงวันเกิดลูกค้าจาก Session (ถ้าล็อกอินอยู่) หรือจากอีเมลที่กรอก
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

            var result = promoService.CalculateDiscount(
                data.BasePrice,
                nightsCount,
                roomCount,
                checkInDate,
                guestDob,
                data.CouponCode
            );

            return Json(new
            {
                success = string.IsNullOrEmpty(result.ErrorMessage),
                discount = result.TotalDiscount,
                promotions = result.AppliedPromotions,
                message = result.ErrorMessage
            });
        }

        // POST: /Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmBooking(CustomerCheckoutViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. จัดการอัปโหลดไฟล์สลิป (Payment Slip)
                string? slipFileName = null;
                if (model.PaymentSlip != null && model.PaymentSlip.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slips");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    slipFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.PaymentSlip.FileName);
                    string filePath = Path.Combine(uploadsFolder, slipFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        model.PaymentSlip.CopyTo(fileStream);
                    }
                }

                // 2. จัดการข้อมูลลูกค้า (Guest) ค้นหาจาก Session หรืออีเมล
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

                // 3. สร้างข้อมูลการจอง (Booking)
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

                // 4. บันทึกข้อมูลการชำระเงิน (Payment) พร้อมแนบสลิป
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

                // 5. เปลี่ยนสถานะห้องพักเป็นจองแล้ว
                var room = _context.Rooms.Find(model.RoomId);
                if (room != null)
                {
                    room.Status = "Reserved";
                }

                _context.SaveChanges();

                return RedirectToAction("BookingSuccess", new { bookingRef = newBooking.BookingRef });
            }

            // กรณี ModelState ไม่ผ่าน ดึงข้อมูลประกอบของห้องกลับมาแสดงผล
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

        // GET: /Booking/BookingSuccess
        public IActionResult BookingSuccess(string bookingRef)
        {
            ViewBag.BookingRef = bookingRef;
            return View();
        }

        // GET: /Booking/MyBookings
        // หน้าตรวจสอบสถานะการจองและประวัติการจองของลูกค้า
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
                // ถ้ายังไม่ล็อกอินและยังไม่ได้กรอกค้นหา ให้ส่งรายการว่างไปแสดงหน้าค้นหาก่อน
                return View(new List<Booking>());
            }

            var bookings = query
                .OrderByDescending(b => b.BookingId)
                .ToList();

            return View(bookings);
        }
    }

    // คลาสตัวแทนสำหรับรับข้อมูล JSON จากหน้าเว็บ
    public class PromoRequestData
    {
        public decimal BasePrice { get; set; }
        public int Nights { get; set; }
        public int Rooms { get; set; } = 1;
        public string? CheckInDate { get; set; }
        public string? CouponCode { get; set; }
        public string? Email { get; set; }
    }
}