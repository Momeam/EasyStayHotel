// นำเข้า Microsoft.AspNetCore.Mvc สำหรับโครงสร้าง Controller, Action Results และ Routing
using Microsoft.AspNetCore.Mvc;
// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Request Context
using Microsoft.AspNetCore.Http;
// นำเข้าโมเดลหลักของโปรเจกต์ EasyStayHotel.Models
using EasyStayHotel.Models;
// นำเข้า EntityFrameworkCore สำหรับ Include, ThenInclude, ToList
using Microsoft.EntityFrameworkCore;
// นำเข้า ViewModels ที่เกี่ยวข้อง
using EasyStayHotel.Models.ViewModels;
// นำเข้า ViewModels ทั่วไป
using EasyStayHotel.ViewModels;
// นำเข้าเซอร์วิสต่างๆ เช่น PromotionService, RefundService, ReviewService, DateHelper
using EasyStayHotel.Services;
// นำเข้า System สำหรับการใช้งานคลาสพื้นฐาน เช่น DateTime, Guid, Math
using System;
// นำเข้า System.Collections.Generic สำหรับ List และ Dictionary
using System.Collections.Generic;
// นำเข้า System.Linq สำหรับฟังก์ชัน Where, GroupBy, Select, OrderBy, FirstOrDefault
using System.Linq;
// นำเข้า System.IO สำหรับจัดการไฟล์และโฟลเดอร์รูปภาพสลิปโอนเงิน
using System.IO;
// นำเข้า System.Threading.Tasks สำหรับการประมวลผล Asynchronous (async/await)
using System.Threading.Tasks;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers
{
    // ตัวควบคุมหลักสำหรับระบบค้นหาห้องพัก จองห้องพัก คำนวณโปรโมชั่น ชำระเงิน และขอคืนเงิน
    public class BookingController : Controller
    {
        // ตัวแปรเชื่อมโยงฐานข้อมูล MySQL ผ่าน Entity Framework Core
        private readonly EasystaydbContext _context;
        // เซอร์วิสสำหรับส่งอีเมลยืนยันการจองห้องพัก
        private readonly IEmailService _emailService;

        // คอนสตรัคเตอร์รับ DbContext และ EmailService ผ่าน Dependency Injection
        public BookingController(EasystaydbContext context, IEmailService emailService)
        {
            // กำหนดค่า DbContext
            _context = context;
            // กำหนดค่า EmailService
            _emailService = emailService;
        }

        // 1. ระบบค้นหาห้องพักว่างตามสาขา วันที่เข้าพัก ช่วงราคา และประเภทห้อง
        [HttpGet]
        public IActionResult Search(
            // สาขาของโรงแรม (Bangkok, Chonburi, Phuket)
            string? branch,
            // วันที่ต้องการเช็คอิน
            DateTime? checkIn,
            // วันที่ต้องการเช็คเอาท์
            DateTime? checkOut,
            // จำนวนห้องที่ต้องการจอง (ค่าเริ่มต้น 1)
            int rooms = 1,
            // จำนวนผู้เข้าพัก (ค่าเริ่มต้น 2)
            int guests = 2,
            // รหัสประเภทห้องพักที่ต้องการกรองเฉพาะเจาะจง
            int? roomTypeId = null,
            // ราคาขั้นต่ำต่อคืน
            decimal? minPrice = null,
            // ราคาขั้นสูงสุดต่อคืน
            decimal? maxPrice = null)
        {
            // กำหนดสาขาเริ่มต้นเป็นกรุงเทพฯ หากผู้ใช้ไม่ได้เลือก
            if (string.IsNullOrEmpty(branch))
            {
                branch = "Bangkok";
            }

            // ปรับเทียบวันที่เช็คอินให้อยู่ในปี ค.ศ. สากลเสมอ ป้องกันปัญหาปี พ.ศ. 3112
            DateTime actualCheckIn = DateHelper.NormalizeToGregorian(checkIn == null || checkIn == default ? DateTime.Today : checkIn.Value);
            // ปรับเทียบวันที่เช็คเอาท์ให้อยู่ในปี ค.ศ. สากล (หากไม่มีให้เป็นวันถัดไปจากวันเช็คอิน)
            DateTime actualCheckOut = DateHelper.NormalizeToGregorian(checkOut == null || checkOut == default ? actualCheckIn.AddDays(1) : checkOut.Value);

            // ตรวจสอบเงื่อนไขวันที่ วันเช็คเอาท์ต้องมากกว่าวันเช็คอินอย่างน้อย 1 คืน
            if (actualCheckIn >= actualCheckOut)
            {
                actualCheckOut = actualCheckIn.AddDays(1);
            }

            // ตรวจสอบจำนวนห้องขั้นต่ำอย่างน้อย 1 ห้อง
            if (rooms < 1) rooms = 1;
            // ตรวจสอบจำนวนผู้เข้าพักขั้นต่ำอย่างน้อย 1 ท่าน
            if (guests < 1) guests = 1;

            // ดึงประเภทห้องพักทั้งหมดสำหรับแสดงในตัวกรองด้านข้าง โดยเรียงตามราคาจากน้อยไปมาก
            var allRoomTypes = _context.Roomtypes.OrderBy(rt => rt.BasePrice).ToList();

            // คิวรีดึงเฉพาะห้องพักที่สถานะว่าง (Available) ในสาขาที่เลือก และโหลดข้อมูลประเภทห้องมาด้วย
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Branch == branch && r.Status == "Available" && r.RoomType != null);

            // กรองราคาขั้นต่ำหากผู้ใช้ระบุ
            if (minPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice >= minPrice.Value);
            }

            // กรองราคาขั้นสูงสุดหากผู้ใช้ระบุ
            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.RoomType!.BasePrice <= maxPrice.Value);
            }

            // ดึงรายการห้องพักที่ว่างทั้งหมดตามเงื่อนไขออกมาเป็น List
            var availableRooms = query.ToList();

            // จัดกลุ่มห้องว่างตามประเภทห้อง เพื่อแสดงจำนวนห้องว่างที่เหลือและหาห้องแรกสำหรับจอง
            var groupedRoomTypes = availableRooms
                .Where(r => r.RoomType != null)
                .GroupBy(r => r.RoomType!)
                .Select(g => new
                {
                    // ข้อมูลประเภทห้องพัก (RoomType)
                    RoomType = g.Key,
                    // จำนวนห้องที่ยังว่างในประเภทนี้
                    AvailableCount = g.Count(),
                    // รหัสห้องว่างแรกที่สามารถเปิดจองได้
                    FirstAvailableRoomId = g.First().RoomId,
                    // ตัวอย่างหมายเลขห้องว่าง 5 ห้องแรก
                    SampleRoomNumbers = string.Join(", ", g.Take(5).Select(x => x.RoomNo))
                })
                // เรียงลำดับประเภทห้องที่เลือกไว้ขึ้นก่อน
                .OrderByDescending(g => roomTypeId.HasValue && g.RoomType.RoomTypeId == roomTypeId.Value)
                // ตามด้วยเรียงราคาจากน้อยไปมาก
                .ThenBy(g => g.RoomType.BasePrice)
                .ToList();

            // ส่งค่าพารามิเตอร์การค้นหาไปยัง View ผ่าน ViewBag
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

            // ส่งจำนวนห้องว่างของแต่ละสาขาสำหรับแสดงผลที่แถบด้านบน
            ViewBag.AvailBkk = _context.Rooms.Count(r => r.Branch == "Bangkok" && r.Status == "Available");
            ViewBag.AvailChon = _context.Rooms.Count(r => r.Branch == "Chonburi" && r.Status == "Available");
            ViewBag.AvailPhuket = _context.Rooms.Count(r => r.Branch == "Phuket" && r.Status == "Available");

            // แสดงหน้าจอ Search พร้อมข้อมูลกลุ่มห้องพักว่าง
            return View(groupedRoomTypes);
        }

        // 2. หน้าชำระเงินและสรุปรายการจอง (Checkout) - บังคับต้องเข้าสู่ระบบสมาชิกก่อนทำการจอง
        [HttpGet]
        public IActionResult Checkout(int roomId, string checkIn, string checkOut, int guests = 2, int rooms = 1)
        {
            // ตรวจสอบว่าผู้ใช้เข้าสู่ระบบหรือยัง หากยังไม่ล็อกอินให้นำส่งไปหน้า Login พร้อมจดจำ URL เดิม (returnUrl)
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (!sessionUserId.HasValue)
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบสมาชิกก่อนดำเนินการจองห้องพัก";
                string returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // แปลงวันเช็คอินเป็นปี ค.ศ. สากล
            DateTime checkInDate = DateHelper.ParseGregorian(checkIn, DateTime.Today);
            // แปลงวันเช็คเอาท์เป็นปี ค.ศ. สากล
            DateTime checkOutDate = DateHelper.ParseGregorian(checkOut, checkInDate.AddDays(1));
            // ตรวจสอบเงื่อนไขว่าวันเช็คเอาท์ต้องมากกว่าวันเช็คอิน
            if (checkInDate >= checkOutDate)
            {
                checkOutDate = checkInDate.AddDays(1);
            }

            // ดึงข้อมูลห้องพักพร้อมประเภทห้องจากฐานข้อมูล MySQL
            var room = _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefault(r => r.RoomId == roomId);

            // ตรวจสอบว่าพบข้อมูลห้องพักในฐานข้อมูลหรือไม่
            if (room == null || room.RoomType == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลห้องพักที่เลือก กรุณาค้นหาใหม่อีกครั้ง";
                return RedirectToAction("Search");
            }

            // ป้องกันการเข้าถึงผ่าน URL ตรงสำหรับห้องที่ไม่ว่างหรือมีผู้จองแล้ว (Anti-Overbooking)
            if (room.Status != "Available")
            {
                TempData["ErrorMessage"] = $"ขออภัย ห้องพักหมายเลข {room.RoomNo} ({room.RoomType.TypeName}) ไม่ว่างหรือมีผู้จองแล้ว กรุณาเลือกห้องพักอื่นที่ยังว่าง";
                return RedirectToAction("Search", new { branch = room.Branch });
            }

            // คำนวณราคาห้องพักพื้นฐาน
            decimal basePrice = room.RoomType.BasePrice;
            // กรณีราคาห้องเป็น 0 ให้กำหนดราคาตามประเภทห้อง
            if (basePrice <= 0)
            {
                string name = (room.RoomType.TypeName ?? "").ToLower();
                if (name.Contains("suite") || name.Contains("สวีท")) basePrice = 3800;
                else if (name.Contains("deluxe") || name.Contains("ดีลักซ์")) basePrice = 2200;
                else basePrice = 1200;
            }

            // คำนวณจำนวนคืนที่เข้าพักจริง
            int nights = (checkOutDate - checkInDate).Days > 0 ? (checkOutDate - checkInDate).Days : 1;

            // ดึงข้อมูลห้องว่างทั้งหมดในสาขาเดียวกัน แยกตามประเภทห้องพัก เพื่อให้ลูกค้าเลือกจองหลายห้องพร้อมกันได้
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

            // หาจำนวนห้องว่างสูงสุดของประเภทห้องที่กำลังเลือกอยู่
            int maxAvailCurrentType = branchAvailableTypes
                .FirstOrDefault(x => x.RoomTypeId == room.RoomType.RoomTypeId)?.AvailableCount ?? 1;
            if (maxAvailCurrentType < 1) maxAvailCurrentType = 1;

            int validRoomsCount = Math.Clamp(rooms > 0 ? rooms : 1, 1, maxAvailCurrentType);

            // กำหนดโมเดล CustomerCheckoutViewModel พร้อมค่าเริ่มต้น
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

            // ดึงข้อมูลผู้ใช้จาก Session มาเติมในฟอร์มอัตโนมัติ (กรณีที่ลูกค้าล็อกอินอยู่แล้ว)
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

            // ส่งข้อมูลประกอบห้องพักไปยัง View
            ViewBag.RoomBranch = room.Branch;
            ViewBag.RoomNo = room.RoomNo;
            ViewBag.RoomTypeId = room.RoomType.RoomTypeId;
            ViewBag.RoomDescription = room.RoomType.Description;
            ViewBag.RoomCapacity = room.RoomType.Capacity;
            ViewBag.RoomsCount = validRoomsCount;
            ViewBag.MaxAvailableRooms = maxAvailCurrentType;
            ViewBag.BranchAvailableRoomTypesJson = System.Text.Json.JsonSerializer.Serialize(branchAvailableTypes);

            // แสดงหน้าจอ Checkout
            return View(model);
        }

        // 3. AJAX API สำหรับคำนวณส่วนลดโปรโมชั่นและคูปองแบบ Real-time
        [HttpPost]
        public IActionResult CalculateDiscount([FromBody] PromoRequestData data)
        {
            // สร้างอินสแตนซ์ของ PromotionService
            var promoService = new PromotionService();

            // ตัวแปรเก็บวันเกิดของลูกค้าเพื่อใช้คำนวณโปรโมชั่นเดือนเกิด
            DateTime? guestDob = null;
            // ดึงข้อมูลวันเกิดจากเซสชันของผู้ใช้ที่ล็อกอินอยู่
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                var user = _context.Users.Find(sessionUserId.Value);
                if (user?.DateOfBirth != null)
                {
                    guestDob = user.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                }
            }
            // หากไม่ได้ล็อกอิน ให้ค้นหาจากอีเมลที่กรอกในฟอร์ม
            else if (!string.IsNullOrEmpty(data.Email))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == data.Email);
                if (user?.DateOfBirth != null)
                {
                    guestDob = user.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                }
            }

            // แปลงวันเช็คอินเป็นปี ค.ศ.
            DateTime checkInDate = DateHelper.ParseGregorian(data.CheckInDate, DateTime.Today);

            // ตรวจสอบจำนวนห้องและจำนวนคืน
            int roomCount = data.Rooms > 0 ? data.Rooms : 1;
            int nightsCount = data.Nights > 0 ? data.Nights : 1;

            // กรณีจองหลายห้องที่มีหลายประเภทห้องรวมกัน ให้ใช้ราคาเฉลี่ยต่อห้องต่อคืนเพื่อให้ยอดรวมตรงกับราคาห้องทั้งหมด
            decimal effectiveBasePrice = data.TotalRoomPricePerNight > 0
                ? (data.TotalRoomPricePerNight / roomCount)
                : data.BasePrice;

            // เรียกฟังก์ชันคำนวณส่วนลดจาก PromotionService
            var result = promoService.CalculateDiscount(
                effectiveBasePrice,
                nightsCount,
                roomCount,
                checkInDate,
                guestDob,
                data.CouponCode,
                data.RoomTypeName
            );

            // ส่งผลลัพธ์การคำนวณกลับในรูปแบบ JSON
            return Json(new
            {
                success = string.IsNullOrEmpty(result.ErrorMessage),
                discount = result.TotalDiscount,
                promotions = result.AppliedPromotions,
                message = result.ErrorMessage
            });
        }

        // 4. บันทึกและยืนยันการจองห้องพัก (Confirm Booking) - บังคับต้องเข้าสู่ระบบสมาชิกก่อนยืนยันการจอง
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(CustomerCheckoutViewModel model)
        {
            // ป้องกันการส่งฟอร์มจองห้องพักโดยไม่ได้เข้าสู่ระบบ
            int? currentSessionUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentSessionUserId.HasValue)
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบสมาชิกก่อนยืนยันการจองห้องพัก";
                return RedirectToAction("Login", "Account");
            }

            // ตรวจสอบความถูกต้องของโมเดล
            if (ModelState.IsValid)
            {
                // ตรวจสอบความพร้อมของห้องพักหลักก่อนบันทึก ป้องกันการแย่งจองห้องเดียวกันพร้อมกัน
                var room = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
                if (room == null || room.Status != "Available")
                {
                    TempData["ErrorMessage"] = "ขออภัย ห้องพักนี้ไม่ว่างหรือมีผู้จองไปแล้ว กรุณาเลือกห้องพักอื่นที่ยังว่าง";
                    return RedirectToAction("Search");
                }

                // รวบรวมจำนวนห้องที่ลูกค้าต้องการจองทั้งหมดแยกตามประเภทห้อง (รองรับทั้งการเพิ่มจำนวนห้องเดิม และการกด "+ จองห้องพักเพิ่ม")
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

                // ค้นหาห้องว่างจริงในสาขาเดียวกันตามจำนวนที่ลูกค้าเลือกทั้งหมด
                var roomsToReserve = new List<Room>();
                foreach (var kvp in requestedByType)
                {
                    int rtId = kvp.Key;
                    int needed = kvp.Value;
                    var matchingAvail = _context.Rooms
                        .Include(r => r.RoomType)
                        .Where(r => r.Branch == room.Branch && r.RoomTypeId == rtId && r.Status == "Available")
                        .OrderBy(r => r.RoomId != model.RoomId) // ให้ห้องที่ลูกค้าคลิกเลือกมาเป็นลำดับแรก
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

                // บันทึกรูปภาพสลิปหลักฐานการโอนเงินลงในโฟลเดอร์ uploads/slips
                string? slipFileName = null;
                if (model.PaymentSlip != null && model.PaymentSlip.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slips");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // สร้างชื่อไฟล์ที่ไม่ซ้ำกันด้วย Guid
                    slipFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.PaymentSlip.FileName);
                    string filePath = Path.Combine(uploadsFolder, slipFileName);

                    // เขียนไฟล์สลิปลงดิสก์
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        model.PaymentSlip.CopyTo(fileStream);
                    }
                }

                // ค้นหาหรือสร้างบัญชีลูกค้าใหม่ลงในตาราง users
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

                // หากยังไม่มีบัญชี ให้สร้างบัญชีลูกค้าใหม่โดยอัตโนมัติ
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

                // ปรับเทียบวันเช็คอินและวันเช็คเอาท์ให้อยู่ในปี ค.ศ.
                model.CheckInDate = DateHelper.NormalizeToGregorian(model.CheckInDate);
                model.CheckOutDate = DateHelper.NormalizeToGregorian(model.CheckOutDate);
                if (model.CheckOutDate <= model.CheckInDate)
                {
                    model.CheckOutDate = model.CheckInDate.AddDays(1);
                }

                // คำนวณสัดส่วนยอดชำระของแต่ละห้องให้รวมกันตรงกับ model.TotalPrice พอดี 100%
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

                    // สร้างข้อมูลการจองลงตาราง bookings ด้วยสถานะเริ่มต้นเป็น Pending (รอตรวจสอบ)
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

                    // สร้างข้อมูลธุรกรรมการชำระเงินลงตาราง payments พร้อมผูกรหัสการจองและรูปสลิปเดียวกัน
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

                    // ปรับสถานะห้องพักในตาราง rooms เป็น Reserved เพื่อล็อกห้องไว้ทุกห้องที่จอง
                    targetRoom.Status = "Reserved";
                    createdBookings.Add(newBooking);
                }

                // บันทึกข้อมูลการชำระเงินและสถานะห้องพักทั้งหมดลง MySQL
                _context.SaveChanges();

                // จัดการส่งอีเมลยืนยันการจองพร้อม Voucher
                try
                {
                    string branchName = room?.Branch ?? "Bangkok";
                    string roomSummaryName = totalBookedRooms > 1
                        ? string.Join(", ", roomsToReserve.GroupBy(r => r.RoomType?.TypeName ?? "ห้องพักมาตรฐาน").Select(g => $"{g.Key} x{g.Count()} ห้อง"))
                        : (room?.RoomType?.TypeName ?? model.RoomTypeName ?? "ห้องพักมาตรฐาน");
                    string primaryRefForVoucher = createdBookings.First().BookingRef;
                    string voucherUrl = Url.Action("BookingVoucher", "Booking", new { bookingRef = primaryRefForVoucher }, Request.Scheme) ?? "";

                    // ส่งอีเมลยืนยันการจองพร้อมลิงก์ Booking Voucher ให้ลูกค้าอัตโนมัติ
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
                    // ป้องกันความผิดพลาดของอีเมลมาทำให้การจองล้มเหลว
                }

                // นำส่งไปยังหน้าจองสำเร็จ BookingSuccess (ส่ง baseBookingRef เพื่อให้แสดงครบทุกห้องในกลุ่ม)
                return RedirectToAction("BookingSuccess", new { bookingRef = baseBookingRef });
            }

            // หากข้อมูลในโมเดลไม่ถูกต้อง ให้โหลดข้อมูลห้องพักเดิมกลับมาแสดงใหม่
            var selectedRoom = _context.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.RoomId == model.RoomId);
            if (selectedRoom != null)
            {
                ViewBag.RoomBranch = selectedRoom.Branch;
                ViewBag.RoomNo = selectedRoom.RoomNo;
                ViewBag.RoomTypeId = selectedRoom.RoomType?.RoomTypeId ?? 1;
                ViewBag.RoomDescription = selectedRoom.RoomType?.Description;
                ViewBag.RoomCapacity = selectedRoom.RoomType?.Capacity ?? 2;
            }

            // แสดงหน้าจอ Checkout เดิมพร้อมข้อความเตือน
            return View("Checkout", model);
        }

        // 5. หน้าแสดงความสำเร็จในการจองห้องพัก (รองรับทั้งจองเดี่ยวและจองหลายห้องพร้อมกัน)
        [HttpGet]
        public IActionResult BookingSuccess(string bookingRef)
        {
            string cleanRef = (bookingRef ?? "").Trim();
            string baseRef = cleanRef.Contains("-R")
                ? cleanRef.Substring(0, cleanRef.LastIndexOf("-R"))
                : cleanRef;

            // ดึงรายการห้องทั้งหมดที่อยู่ในกลุ่มการจองเดียวกัน
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

        // 6. แสดงเอกสารยืนยันการจองห้องพัก (Booking Voucher) ขนาด A4 สำหรับพิมพ์หรือเซฟเป็น PDF
        [HttpGet]
        public IActionResult BookingVoucher(string? bookingRef, int? id)
        {
            // ตรวจสอบว่ามีการส่งรหัสจองหรือ id มาหรือไม่
            if (string.IsNullOrWhiteSpace(bookingRef) && !id.HasValue)
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสการจองที่ต้องการดูเอกสาร";
                return RedirectToAction(nameof(MyBookings));
            }

            // คิวรีดึงข้อมูลการจองพร้อมตารางที่เกี่ยวข้อง
            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .AsQueryable();

            Booking? booking = null;
            // ค้นหาตาม BookingRef (รองรับทั้งรหัสเต็ม เช่น ES-...-R1 และรหัสหลักของกลุ่ม เช่น ES-...)
            if (!string.IsNullOrWhiteSpace(bookingRef))
            {
                string cleanRef = bookingRef.Trim();
                booking = query.FirstOrDefault(b => b.BookingRef == cleanRef)
                          ?? query.OrderBy(b => b.BookingId).FirstOrDefault(b => b.BookingRef.StartsWith(cleanRef + "-R"));
            }
            // หรือค้นหาตาม BookingId
            else if (id.HasValue)
            {
                booking = query.FirstOrDefault(b => b.BookingId == id.Value);
            }

            // หากไม่พบข้อมูลการจอง
            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ระบุ";
                return RedirectToAction(nameof(MyBookings));
            }

            // ตรวจสอบว่ารายการจองนี้อยู่ในกลุ่มการจองหลายห้องพร้อมกันหรือไม่ (เช่น ES-2026...-R1, ES-2026...-R2)
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

            // ดึงข้อมูลการชำระเงินล่าสุด
            var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
            ViewBag.LatestPayment = latestPayment;

            // แสดงหน้าจอ BookingVoucher
            return View(booking);
        }

        // 7. หน้าตรวจสอบสถานะและประวัติการจองของลูกค้า (MyBookings)
        [HttpGet]
        public IActionResult MyBookings(string? bookingRef, string? contact)
        {
            // ดึง UserId จาก Session
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.SearchBookingRef = bookingRef;
            ViewBag.SearchContact = contact;
            ViewBag.IsLoggedIn = sessionUserId.HasValue;

            // คิวรีรายการจองพร้อมห้องพัก ผู้ใช้ และการชำระเงิน
            var query = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .AsQueryable();

            // ตรวจสอบว่ามีตัวกรองค้นหาหรือไม่
            bool hasSearchFilter = !string.IsNullOrWhiteSpace(bookingRef) || !string.IsNullOrWhiteSpace(contact);

            if (hasSearchFilter)
            {
                // กรองตามรหัสการจอง
                if (!string.IsNullOrWhiteSpace(bookingRef))
                {
                    string refClean = bookingRef.Trim();
                    query = query.Where(b => b.BookingRef.Contains(refClean));
                }

                // กรองตามข้อมูลติดต่อ เช่น ชื่อ อีเมล หรือเบอร์โทรศัพท์
                if (!string.IsNullOrWhiteSpace(contact))
                {
                    string contactClean = contact.Trim();
                    query = query.Where(b =>
                        (b.GuestName != null && b.GuestName.Contains(contactClean)) ||
                        (b.User != null && (b.User.Email.Contains(contactClean) || (b.User.Phone != null && b.User.Phone.Contains(contactClean)))));
                }
            }
            // หากไม่ได้ค้นหาเฉพาะเจาะจง ให้ดึงรายการของสมาชิกที่ล็อกอินอยู่
            else if (sessionUserId.HasValue)
            {
                query = query.Where(b => b.UserId == sessionUserId.Value);
            }
            // หากไม่ได้ล็อกอินและไม่ได้ค้นหา ให้คืนค่า List ว่าง
            else
            {
                ViewBag.RefundRequests = new Dictionary<int, RefundRequest>();
                return View(new List<Booking>());
            }

            // เรียงลำดับรายการจองจากล่าสุดไปเก่าสุด
            var bookings = query
                .OrderByDescending(b => b.BookingId)
                .ToList();

            // ดึงคำขอคืนเงินทั้งหมดมาจับคู่กับ BookingId สำหรับแสดงสถานะและปุ่มคืนเงิน
            var allRefunds = RefundService.GetAll();
            var refundMap = allRefunds.GroupBy(r => r.BookingId).ToDictionary(g => g.Key, g => g.First());
            ViewBag.RefundRequests = refundMap;

            // แสดงหน้าจอ MyBookings
            return View(bookings);
        }

        // 8. ระบบขอคืนเงินของลูกค้า (เงื่อนไขไม่เกิน 7 วันหลังจอง และกำหนดโอนคืนใน 3 วันทำการ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestRefund(RefundRequestViewModel model)
        {
            // ตรวจสอบความถูกต้องของโมเดล
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "ข้อมูลการขอคืนเงินไม่ถูกต้อง กรุณากรอกข้อมูลให้ครบถ้วน";
                return RedirectToAction(nameof(MyBookings));
            }

            // ค้นหารายการจองในฐานข้อมูล
            var booking = _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r!.RoomType)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .FirstOrDefault(b => b.BookingId == model.BookingId);

            // หากไม่พบรายการจอง
            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ต้องการขอคืนเงิน";
                return RedirectToAction(nameof(MyBookings));
            }

            // ตรวจสอบเงื่อนไขว่ายังสามารถขอคืนเงินได้หรือไม่ (จองไม่เกิน 7 วัน และยังไม่เช็คอิน)
            if (!RefundService.IsEligibleForRefund(booking, out string eligibilityReason, out int daysRemaining))
            {
                TempData["ErrorMessage"] = eligibilityReason;
                return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
            }

            // ดึงวันที่ทำการจอง
            DateTime bookingDate = RefundService.GetBookingDate(booking);
            // สร้างอ็อบเจกต์ RefundRequest สำหรับบันทึกลง JSON Datastore
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

            // บันทึกคำขอคืนเงิน และปรับสถานะการจองเป็น RefundRequested พร้อมส่งแจ้งเตือนให้ฝ่ายบัญชี
            bool created = RefundService.CreateRequest(refundReq);
            if (created)
            {
                // ปรับสถานะการจองใน MySQL เป็น RefundRequested
                booking.BookingStatus = "RefundRequested";
                _context.SaveChanges();

                // สร้างข้อความแจ้งเตือนอัตโนมัติส่งไปยังกระดานของฝ่ายบัญชี
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

                // แจ้งข้อความสำเร็จแก่ลูกค้า
                TempData["SuccessMessage"] = $"ยื่นคำขอคืนเงินรหัส {booking.BookingRef} สำเร็จแล้ว! ระบบส่งข้อมูลให้พนักงานฝ่ายบัญชีเรียบร้อย โดยฝ่ายบัญชีจะตรวจสอบและโอนเงินคืนผ่านธนาคารภายใน 3 วันทำการ (ภายในวันที่ {refundReq.RefundDeadline:dd/MM/yyyy})";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่สามารถยื่นคำขอคืนเงินได้ เนื่องจากมีคำขอเดิมอยู่ในระบบแล้ว";
            }

            // นำส่งกลับไปยังหน้าประวัติการจองเดิม
            return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
        }

        // 9. บันทึกคะแนนและรีวิวความประทับใจของลูกค้าหลังการเข้าพัก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(int bookingId, int rating, int cleanlinessRating, int serviceRating, int locationRating, string? title, string comment)
        {
            // ดึงข้อมูลการจองพร้อมประเภทห้อง
            var booking = _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .FirstOrDefault(b => b.BookingId == bookingId);

            // หากไม่พบรายการจอง
            if (booking == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลการจองที่ต้องการรีวิว";
                return RedirectToAction(nameof(MyBookings));
            }

            // ดึงชื่อและอีเมลของลูกค้า
            string guestName = HttpContext.Session.GetString("UserName") ?? booking.GuestName ?? "ลูกค้า Easy Stay";
            string guestEmail = HttpContext.Session.GetString("UserEmail") ?? "";

            // สร้างอ็อบเจกต์ GuestReview
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

            // บันทึกรีวิวผ่าน ReviewService
            ReviewService.AddReview(review);
            TempData["SuccessMessage"] = "ขอบคุณสำหรับการรีวิว! ความคิดเห็นอันมีค่าของคุณช่วยให้ Easy Stay พัฒนาการบริการได้อย่างดียิ่งขึ้น";
            return RedirectToAction(nameof(MyBookings), new { bookingRef = booking.BookingRef });
        }
    }

    // โมเดลสำหรับรับข้อมูล AJAX ในการคำนวณโปรโมชั่น
    public class PromoRequestData
    {
        // ราคาห้องพักต่อคืน
        public decimal BasePrice { get; set; }
        // ยอดรวมราคาห้องพักทั้งหมดต่อคืน (กรณีจองหลายห้องหรือหลายประเภทห้องพร้อมกัน)
        public decimal TotalRoomPricePerNight { get; set; }
        // จำนวนคืนที่เข้าพัก
        public int Nights { get; set; }
        // จำนวนห้องที่จอง
        public int Rooms { get; set; } = 1;
        // วันที่เช็คอิน (ข้อความ)
        public string? CheckInDate { get; set; }
        // รหัสคูปอง
        public string? CouponCode { get; set; }
        // อีเมลผู้จอง
        public string? Email { get; set; }
        // ชื่อประเภทห้องพัก
        public string? RoomTypeName { get; set; }
    }
}
