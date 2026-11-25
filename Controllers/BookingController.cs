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
    public class BookingController : Controller
    {
        private readonly EasystaydbContext _context;
        private readonly IEmailService _emailService;

        public BookingController(EasystaydbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

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
            if (string.IsNullOrEmpty(branch))
            {
                branch = "Bangkok";
            }

            DateTime actualCheckIn = DateHelper.NormalizeToGregorian(checkIn == null || checkIn == default ? DateTime.Today : checkIn.Value);
            DateTime actualCheckOut = DateHelper.NormalizeToGregorian(checkOut == null || checkOut == default ? actualCheckIn.AddDays(1) : checkOut.Value);

            if (actualCheckIn >= actualCheckOut)
            {
                actualCheckOut = actualCheckIn.AddDays(1);
            }

            if (rooms < 1) rooms = 1;
            if (guests < 1) guests = 1;

            var allRoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

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
            ViewBag.CheckIn = DateHelper.ToIsoDate(actualCheckIn);
            ViewBag.CheckOut = DateHelper.ToIsoDate(actualCheckOut);
            ViewBag.Nights = (actualCheckOut - actualCheckIn).Days;
            ViewBag.RoomsReq = rooms;
            ViewBag.GuestsReq = guests;
            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.MinPrice = minPrice ?? 500;
            ViewBag.MaxPrice = maxPrice ?? 10000;
            ViewBag.AllRoomTypes = allRoomTypes;
            ViewBag.TotalFound = groupedRoomTypes.Count;

            ViewBag.AvailBkk = _context.Rooms.Count(r => r.Branch == "Bangkok" && r.Status == "Available");
            ViewBag.AvailChon = _context.Rooms.Count(r => r.Branch == "Chonburi" && r.Status == "Available");
            ViewBag.AvailPhuket = _context.Rooms.Count(r => r.Branch == "Phuket" && r.Status == "Available");

            return View(groupedRoomTypes);
        }

        [HttpGet]
        public IActionResult Checkout(int roomId, string checkIn, string checkOut, int guests = 2, int rooms = 1)
        {
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (!sessionUserId.HasValue)
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบสมาชิกก่อนดำเนินการจองห้องพัก";
                string returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            DateTime checkInDate = DateHelper.ParseGregorian(checkIn, DateTime.Today);
            DateTime checkOutDate = DateHelper.ParseGregorian(checkOut, checkInDate.AddDays(1));
            if (checkInDate >= checkOutDate)
            {
                checkOutDate = checkInDate.AddDays(1);
            }

            var room = _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefault(r => r.RoomId == roomId);

            if (room == null || room.RoomType == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลห้องพักที่เลือก กรุณาค้นหาใหม่อีกครั้ง";
                return RedirectToAction("Search");
            }

            if (room.Status != "Available")
            {
                TempData["ErrorMessage"] = $"ขออภัย ห้องพักหมายเลข {room.RoomNo} ({room.RoomType.TypeName}) ไม่ว่างหรือมีผู้จองแล้ว กรุณาเลือกห้องพักอื่นที่ยังว่าง";
                return RedirectToAction("Search", new { branch = room.Branch });
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

            var branchAvailableTypes = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == room.Branch && r.Status == "Available" && r.RoomType != null)
                .ToList()
                .GroupBy(r => r.RoomType!)
                .Select(g =>
                {
                    decimal rtPrice = g.Key.BasePrice;
                    if (rtPrice <= 0)
                    {
                        string rtName = (g.Key.TypeName ?? "").ToLower();
                        if (rtName.Contains("suite") || rtName.Contains("สวีท")) rtPrice = 3800;
                        else if (rtName.Contains("deluxe") || rtName.Contains("ดีลักซ์")) rtPrice = 2200;
                        else rtPrice = 1200;
                    }
                    return new
                    {
                        RoomTypeId = g.Key.RoomTypeId,
                        TypeName = g.Key.TypeName ?? "Standard Room",
                        BasePrice = rtPrice,
                        Capacity = g.Key.Capacity > 0 ? g.Key.Capacity : 2,
                        AvailableCount = g.Count()
                    };
                })
                .OrderBy(x => x.BasePrice)
                .ToList();

            int maxAvailCurrentType = branchAvailableTypes
                .FirstOrDefault(x => x.RoomTypeId == room.RoomType.RoomTypeId)?.AvailableCount ?? 1;
            if (maxAvailCurrentType < 1) maxAvailCurrentType = 1;

            int validRoomsCount = Math.Clamp(rooms > 0 ? rooms : 1, 1, maxAvailCurrentType);

            var model = new CustomerCheckoutViewModel
            {
                RoomId = room.RoomId,
                RoomTypeName = room.RoomType.TypeName ?? "Unknown Room",
                BasePrice = basePrice,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Nights = nights,
                Guests = guests > 0 ? guests : 2,
                RoomsCount = validRoomsCount,
                TotalPrice = basePrice * nights * validRoomsCount
            };

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
            ViewBag.RoomsCount = validRoomsCount;
            ViewBag.MaxAvailableRooms = maxAvailCurrentType;
            ViewBag.BranchAvailableRoomTypesJson = System.Text.Json.JsonSerializer.Serialize(branchAvailableTypes);

            return View(model);
        }

        [HttpPost]
        public IActionResult CalculateDiscount([FromBody] PromoRequestData data)
        {
            var promoService = new PromotionService();

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

            DateTime checkInDate = DateHelper.ParseGregorian(data.CheckInDate, DateTime.Today);

            int roomCount = data.Rooms > 0 ? data.Rooms : 1;
            int nightsCount = data.Nights > 0 ? data.Nights : 1;

            decimal effectiveBasePrice = data.TotalRoomPricePerNight > 0
                ? (data.TotalRoomPricePerNight / roomCount)
                : data.BasePrice;

            var result = promoService.CalculateDiscount(
                effectiveBasePrice,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(CustomerCheckoutViewModel model)
        {
            int? currentSessionUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentSessionUserId.HasValue)
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบสมาชิกก่อนยืนยันการจองห้องพัก";
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
                if (room == null || room.Status != "Available")
                {
                    TempData["ErrorMessage"] = "ขออภัย ห้องพักนี้ไม่ว่างหรือมีผู้จองไปแล้ว กรุณาเลือกห้องพักอื่นที่ยังว่าง";
                    return RedirectToAction("Search");
                }

                var requestedByType = new Dictionary<int, int>();
                int primaryRoomTypeId = room.RoomTypeId ?? 1;
                int primaryCount = model.RoomsCount > 0 ? model.RoomsCount : 1;
                requestedByType[primaryRoomTypeId] = primaryCount;

                if (model.ExtraRoomTypeIds != null && model.ExtraRoomCounts != null)
                {
                    int pairCount = Math.Min(model.ExtraRoomTypeIds.Count, model.ExtraRoomCounts.Count);
                    for (int i = 0; i < pairCount; i++)
                    {
                        int extRtId = model.ExtraRoomTypeIds[i];
                        int extQty = model.ExtraRoomCounts[i];
                        if (extRtId > 0 && extQty > 0)
                        {
                            if (requestedByType.ContainsKey(extRtId))
                                requestedByType[extRtId] += extQty;
                            else
                                requestedByType[extRtId] = extQty;
                        }
                    }
                }

                var roomsToReserve = new List<Room>();
                foreach (var kvp in requestedByType)
                {
                    int rtId = kvp.Key;
                    int needed = kvp.Value;
                    var matchingAvail = _context.Rooms
                        .Include(r => r.RoomType)
                        .Where(r => r.Branch == room.Branch && r.RoomTypeId == rtId && r.Status == "Available")
                        .OrderBy(r => r.RoomId != model.RoomId)
                        .ThenBy(r => r.RoomNo)
                        .Take(needed)
                        .ToList();

                    if (matchingAvail.Count < needed)
                    {
                        string rtTitle = matchingAvail.FirstOrDefault()?.RoomType?.TypeName
                            ?? _context.Roomtypes.FirstOrDefault(x => x.RoomTypeId == rtId)?.TypeName
                            ?? "ที่เลือก";
                        TempData["ErrorMessage"] = $"ขออภัย ห้องพักประเภท {rtTitle} ในสาขานี้มีห้องว่างเหลือเพียง {matchingAvail.Count} ห้อง (คุณเลือก {needed} ห้อง) กรุณาปรับจำนวนห้องใหม่";
                        return RedirectToAction("Checkout", new
                        {
                            roomId = model.RoomId,
                            checkIn = DateHelper.ToIsoDate(model.CheckInDate),
                            checkOut = DateHelper.ToIsoDate(model.CheckOutDate),
                            guests = model.Guests,
                            rooms = Math.Max(1, matchingAvail.Count)
                        });
                    }

                    roomsToReserve.AddRange(matchingAvail);
                }

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

                model.CheckInDate = DateHelper.NormalizeToGregorian(model.CheckInDate);
                model.CheckOutDate = DateHelper.NormalizeToGregorian(model.CheckOutDate);
                if (model.CheckOutDate <= model.CheckInDate)
                {
                    model.CheckOutDate = model.CheckInDate.AddDays(1);
                }

                int totalBookedRooms = roomsToReserve.Count;
                string baseBookingRef = "ES-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal sumBasePrices = roomsToReserve.Sum(r => r.RoomType?.BasePrice > 0 ? r.RoomType.BasePrice : 1200m);
                if (sumBasePrices <= 0) sumBasePrices = totalBookedRooms * 1200m;

                decimal allocatedSoFar = 0m;
                var createdBookings = new List<Booking>();

                for (int idx = 0; idx < totalBookedRooms; idx++)
                {
                    var targetRoom = roomsToReserve[idx];
                    decimal roomWeight = (targetRoom.RoomType?.BasePrice > 0 ? targetRoom.RoomType.BasePrice : 1200m) / sumBasePrices;
                    decimal roomPriceShare = (idx == totalBookedRooms - 1)
                        ? Math.Max(0m, model.TotalPrice - allocatedSoFar)
                        : Math.Round(model.TotalPrice * roomWeight, 2);
                    allocatedSoFar += roomPriceShare;

                    string roomBookingRef = totalBookedRooms == 1
                        ? baseBookingRef
                        : $"{baseBookingRef}-R{idx + 1}";

                    var newBooking = new Booking
                    {
                        BookingRef = roomBookingRef,
                        UserId = guest.UserId,
                        GuestName = model.GuestName,
                        RoomId = targetRoom.RoomId,
                        CheckInDate = DateOnly.FromDateTime(model.CheckInDate),
                        CheckOutDate = DateOnly.FromDateTime(model.CheckOutDate),
                        TotalPrice = roomPriceShare,
                        BookingStatus = "Pending"
                    };
                    _context.Bookings.Add(newBooking);
                    _context.SaveChanges();

                    var newPayment = new Payment
                    {
                        BookingId = newBooking.BookingId,
                        Amount = roomPriceShare,
                        TransferDate = DateTime.Now,
                        PaymentMethod = "PromptPay",
                        PaymentStatus = "Pending",
                        SlipImageUrl = slipFileName != null ? "/uploads/slips/" + slipFileName : null
                    };
                    _context.Payments.Add(newPayment);

                    targetRoom.Status = "Reserved";
                    createdBookings.Add(newBooking);
                }

                _context.SaveChanges();

                try
                {
                    string branchName = room?.Branch ?? "Bangkok";
                    string roomSummaryName = totalBookedRooms > 1
                        ? string.Join(", ", roomsToReserve.GroupBy(r => r.RoomType?.TypeName ?? "ห้องพักมาตรฐาน").Select(g => $"{g.Key} x{g.Count()} ห้อง"))
                        : (room?.RoomType?.TypeName ?? model.RoomTypeName ?? "ห้องพักมาตรฐาน");
                    string primaryRefForVoucher = createdBookings.First().BookingRef;
                    string voucherUrl = Url.Action("BookingVoucher", "Booking", new { bookingRef = primaryRefForVoucher }, Request.Scheme) ?? "";

                    await _emailService.SendBookingConfirmationEmailAsync(
                        guest.Email,
                        model.GuestName,
                        baseBookingRef,
                        roomSummaryName,
                        branchName,
                        createdBookings.First().CheckInDate.ToString("dd/MM/yyyy"),
                        createdBookings.First().CheckOutDate.ToString("dd/MM/yyyy"),
                        model.TotalPrice,
                        voucherUrl
                    );
                }
                catch
                {
                }

                return RedirectToAction("BookingSuccess", new { bookingRef = baseBookingRef });
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

        [HttpGet]
        public IActionResult BookingSuccess(string bookingRef)
        {
            string cleanRef = (bookingRef ?? "").Trim();
            string baseRef = cleanRef.Contains("-R")
                ? cleanRef.Substring(0, cleanRef.LastIndexOf("-R"))
                : cleanRef;

            var groupBookings = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b => b.BookingRef == cleanRef || b.BookingRef == baseRef || b.BookingRef.StartsWith(baseRef + "-R"))
                .OrderBy(b => b.BookingId)
                .ToList();

            ViewBag.BookingRef = cleanRef;
            ViewBag.BaseBookingRef = baseRef;
            ViewBag.GroupBookings = groupBookings;
            ViewBag.PrimaryVoucherRef = groupBookings.FirstOrDefault()?.BookingRef ?? cleanRef;
            return View();
        }

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
                booking = query.FirstOrDefault(b => b.BookingRef == cleanRef)
                          ?? query.OrderBy(b => b.BookingId).FirstOrDefault(b => b.BookingRef.StartsWith(cleanRef + "-R"));
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

            string currentRef = booking.BookingRef ?? "";
            string baseGroupRef = currentRef.Contains("-R")
                ? currentRef.Substring(0, currentRef.LastIndexOf("-R"))
                : currentRef;

            var groupBookings = query
                .Where(b => b.BookingRef == baseGroupRef || b.BookingRef.StartsWith(baseGroupRef + "-R"))
                .OrderBy(b => b.BookingId)
                .ToList();

            ViewBag.GroupBookings = groupBookings;
            ViewBag.BaseGroupRef = baseGroupRef;

            var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
            ViewBag.LatestPayment = latestPayment;

            return View(booking);
        }

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
                RefundDeadline = DateTime.Now.AddDays(3),
                Status = "Pending"
            };

            bool created = RefundService.CreateRequest(refundReq);
            if (created)
            {
                booking.BookingStatus = "RefundRequested";
                _context.SaveChanges();

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

    public class PromoRequestData
    {
        public decimal BasePrice { get; set; }
        public decimal TotalRoomPricePerNight { get; set; }
        public int Nights { get; set; }
        public int Rooms { get; set; } = 1;
        public string? CheckInDate { get; set; }
        public string? CouponCode { get; set; }
        public string? Email { get; set; }
        public string? RoomTypeName { get; set; }
    }
}
