// นำเข้า System สำหรับการใช้งานฟังก์ชันพื้นฐาน เช่น DateTime, Math, Exception
using System;
// นำเข้า System.Collections.Generic สำหรับ List และคอลเลกชันต่างๆ
using System.Collections.Generic;
// นำเข้า System.IO สำหรับการอ่าน เขียน และจัดการไฟล์บนดิสก์
using System.IO;
// นำเข้า System.Linq สำหรับการคิวรีข้อมูล (Where, OrderBy, FirstOrDefault)
using System.Linq;
// นำเข้า System.Text.Json สำหรับแปลงข้อมูลระหว่าง C# Object และ JSON
using System.Text.Json;
// นำเข้า System.Text.RegularExpressions สำหรับการสกัดข้อมูลวันที่จากสตริงด้วย Regex
using System.Text.RegularExpressions;
// นำเข้า โมเดลของโปรเจกต์ EasyStayHotel.Models
using EasyStayHotel.Models;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก RefundService ทำหน้าที่จัดการคำขอคืนเงินของลูกค้า บันทึกลง JSON พร้อมกลไก Thread-safety
    public static class RefundService
    {
        // อ็อบเจกต์สำหรับใช้เป็นกุญแจในการล็อกเธรด (Thread-safety Lock) ป้องกันการแย่งเขียนไฟล์ JSON พร้อมกัน
        private static readonly object _lock = new object();

        // พาธไดเรกทอรีสำหรับจัดเก็บไฟล์ข้อมูล (wwwroot/data)
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        // พาธไฟล์ JSON สำหรับเก็บคำขอคืนเงิน (wwwroot/data/refund_requests.json)
        private static readonly string _filePath = Path.Combine(_dataDir, "refund_requests.json");

        // เมธอดส่วนตัวสำหรับตรวจสอบและสร้างไฟล์ refund_requests.json หากยังไม่มีอยู่ในระบบ
        private static void EnsureFileExists()
        {
            // ตรวจสอบว่าโฟลเดอร์ data มีอยู่แล้วหรือไม่
            if (!Directory.Exists(_dataDir))
            {
                // หากยังไม่มี ให้สร้างโฟลเดอร์ wwwroot/data ขึ้นมา
                Directory.CreateDirectory(_dataDir);
            }

            // ตรวจสอบว่าไฟล์ refund_requests.json มีอยู่แล้วหรือไม่
            if (!File.Exists(_filePath))
            {
                // สร้าง List ว่างสำหรับคำขอคืนเงิน
                var initialList = new List<RefundRequest>();
                // แปลง List ว่างเป็นสตริง JSON โดยจัดรูปแบบให้มีช่องไฟสวยงาม (Indented)
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                // บันทึกสตริง JSON ลงในไฟล์
                File.WriteAllText(_filePath, json);
            }
        }

        // เมธอดสำหรับดึงรายการคำขอคืนเงินทั้งหมด พร้อมรองรับการกรองตามสาขาและสถานะ
        public static List<RefundRequest> GetAll(string? branch = null, string? status = null)
        {
            // ล็อกเธรดเพื่อความปลอดภัยในการอ่านข้อมูลพร้อมกันหลายผู้ใช้
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์ก่อนอ่าน
                    EnsureFileExists();
                    // อ่านข้อความ JSON ทั้งหมดจากไฟล์
                    var json = File.ReadAllText(_filePath);
                    // แปลง JSON กลับเป็น List ของ RefundRequest หากเป็น null ให้สร้าง List ว่าง
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    // ตรวจสอบเงื่อนไขตัวกรองสาขา (หากมีการระบุสาขาและไม่ใช่ ALL)
                    if (!string.IsNullOrEmpty(branch) && branch != "ALL")
                    {
                        // กรองเฉพาะรายการที่ตรงกับสาขาที่ระบุ (ไม่สนตัวพิมพ์เล็ก-ใหญ่)
                        list = list.Where(r => r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    // ตรวจสอบเงื่อนไขตัวกรองสถานะคำขอ (หากมีการระบุและไม่ใช่ ALL)
                    if (!string.IsNullOrEmpty(status) && status != "ALL")
                    {
                        // กรองเฉพาะรายการที่มีสถานะตรงกับที่ต้องการ
                        list = list.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    // เรียงลำดับคำขอตามเวลาที่ส่งเรื่อง จากใหม่สุดไปเก่าสุด แล้วส่งกลับ
                    return list.OrderByDescending(r => r.RequestedAt).ToList();
                }
                catch
                {
                    // กรณีเกิดข้อผิดพลาดในการอ่าน ให้ส่งกลับ List ว่างเพื่อป้องกันโปรแกรมขัดข้อง
                    return new List<RefundRequest>();
                }
            }
        }

        // เมธอดสำหรับค้นหาคำขอคืนเงินตามรหัส RefundId
        public static RefundRequest? GetById(string refundId)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON จากไฟล์
                    var json = File.ReadAllText(_filePath);
                    // แปลงข้อมูล JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();
                    // ค้นหารายการคำขอที่มี RefundId ตรงกับที่ระบุ
                    return list.FirstOrDefault(r => r.RefundId == refundId);
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด ให้คืนค่า null
                    return null;
                }
            }
        }

        // เมธอดสำหรับค้นหาคำขอคืนเงินตามรหัสการจอง BookingId
        public static RefundRequest? GetByBookingId(int bookingId)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON จากไฟล์
                    var json = File.ReadAllText(_filePath);
                    // แปลงข้อมูล JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();
                    // ค้นหารายการคำขอที่มี BookingId ตรงกับที่ระบุ
                    return list.FirstOrDefault(r => r.BookingId == bookingId);
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด ให้คืนค่า null
                    return null;
                }
            }
        }

        // เมธอดสำหรับสร้างคำขอคืนเงินใหม่เข้าสู่ระบบ
        public static bool CreateRequest(RefundRequest request)
        {
            // ล็อกเธรดเพื่อป้องกันการเขียนชนกัน
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON ที่มีอยู่เดิม
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    // ตรวจสอบว่าเคยมีการยื่นคำขอสำหรับ BookingId นี้ไปแล้วหรือไม่
                    var existing = list.FirstOrDefault(r => r.BookingId == request.BookingId);
                    // หากมีคำขอเดิมอยู่และสถานะไม่ใช่ Rejected (เช่น Pending หรือ Approved) จะไม่อนุญาตให้ยื่นซ้ำ
                    if (existing != null && existing.Status != "Rejected")
                    {
                        // ปฏิเสธการสร้างคำขอซ้ำ
                        return false;
                    }

                    // หากมีคำขอเดิมที่เคยถูก Rejected ให้ลบรายการเดิมออก เพื่อเปิดทางให้ยื่นใหม่ได้
                    if (existing != null && existing.Status == "Rejected")
                    {
                        // ลบคำขอเก่าที่ถูกปฏิเสธทิ้ง
                        list.Remove(existing);
                    }

                    // บันทึกวันเวลาที่ยื่นคำขอเป็นเวลาปัจจุบัน
                    request.RequestedAt = DateTime.Now;
                    // กำหนดวันครบกำหนดคืนเงินเป็น 3 วันทำการนับจากปัจจุบัน (SLA 3 วัน)
                    request.RefundDeadline = DateTime.Now.AddDays(3);
                    // กำหนดสถานะเริ่มต้นของคำขอเป็น Pending (รอตรวจสอบ)
                    request.Status = "Pending";

                    // เพิ่มคำขอใหม่ลงใน List
                    list.Add(request);
                    // แปลง List ที่อัปเดตแล้วเป็น JSON สตริงแบบมีจัดย่อหน้า
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // เขียนทับไฟล์ JSON เดิมด้วยข้อมูลใหม่
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true แสดงว่าสร้างคำขอสำเร็จ
                    return true;
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }

        // เมธอดสำหรับฝ่ายบัญชีในการอนุมัติคำขอคืนเงิน (Approve)
        public static bool Approve(string refundId, string adminName, string? adminNote = null, string? slipUrl = null)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON เดิม
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    // ค้นหารายการคำขอที่ต้องการอนุมัติตาม RefundId
                    var target = list.FirstOrDefault(r => r.RefundId == refundId);
                    // หากไม่พบคำขอ ให้คืนค่า false ทันที
                    if (target == null) return false;

                    // ปรับสถานะคำขอเป็น Approved (อนุมัติแล้ว)
                    target.Status = "Approved";
                    // บันทึกชื่อเจ้าหน้าที่ฝ่ายบัญชีผู้ดำเนินการ
                    target.ProcessedBy = adminName;
                    // บันทึกวันเวลาที่ดำเนินการอนุมัติ
                    target.ProcessedAt = DateTime.Now;
                    // หากมีหมายเหตุเพิ่มเติมจากฝ่ายบัญชี ให้บันทึกลงใน AdminNote
                    if (!string.IsNullOrEmpty(adminNote)) target.AdminNote = adminNote;
                    // หากมีแนบสลิปการโอนเงินคืน ให้บันทึกลิงก์ลงใน RefundSlipUrl
                    if (!string.IsNullOrEmpty(slipUrl)) target.RefundSlipUrl = slipUrl;

                    // แปลงข้อมูลที่อัปเดตเป็น JSON
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // บันทึกลงไฟล์ JSON
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true สำเร็จ
                    return true;
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }

        // เมธอดสำหรับฝ่ายบัญชีในการปฏิเสธคำขอคืนเงิน (Reject)
        public static bool Reject(string refundId, string adminName, string rejectionReason)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON เดิม
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    // ค้นหารายการคำขอตาม RefundId
                    var target = list.FirstOrDefault(r => r.RefundId == refundId);
                    // หากไม่พบ ให้คืนค่า false
                    if (target == null) return false;

                    // ปรับสถานะคำขอเป็น Rejected (ปฏิเสธ)
                    target.Status = "Rejected";
                    // บันทึกชื่อเจ้าหน้าที่ผู้ดำเนินการ
                    target.ProcessedBy = adminName;
                    // บันทึกวันเวลาที่ปฏิเสธ
                    target.ProcessedAt = DateTime.Now;
                    // บันทึกเหตุผลการปฏิเสธลงใน AdminNote
                    target.AdminNote = rejectionReason;

                    // แปลงข้อมูลที่อัปเดตเป็น JSON
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // บันทึกลงไฟล์ JSON
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true สำเร็จ
                    return true;
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }

        // เมธอดตัวช่วยสำหรับค้นหาวันที่ทำการจองจริงจากอ็อบเจกต์ Booking
        public static DateTime GetBookingDate(Booking booking)
        {
            // ลำดับที่ 1: ตรวจสอบวันเวลาที่มีการโอนเงินชำระเงินในตาราง Payments
            var paymentDate = booking.Payments?.OrderBy(p => p.PaymentId).FirstOrDefault()?.TransferDate;
            // หากมีวันชำระเงิน ให้ใช้วันชำระเงินเป็นหลัก
            if (paymentDate.HasValue) return paymentDate.Value;

            // ลำดับที่ 2: สกัดตัวเลขวันที่ 8 หลัก (yyyyMMdd) จากรหัส BookingRef เช่น ES-25691117...
            if (!string.IsNullOrEmpty(booking.BookingRef))
            {
                // ค้นหาชุดตัวเลข 8 หลักด้วย Regular Expression
                var match = Regex.Match(booking.BookingRef, @"\d{8}");
                // ตรวจสอบว่าพบตัวเลข 8 หลักและสามารถแปลงเป็น DateTime ตามรูปแบบ yyyyMMdd ได้หรือไม่
                if (match.Success && DateTime.TryParseExact(match.Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    // ตรวจสอบว่าปีที่สกัดได้เป็น พ.ศ. หรือไม่ (หากเกิน 2400)
                    if (parsedDate.Year > 2400)
                    {
                        // ลบ 543 ปีเพื่อแปลงกลับเป็นปี ค.ศ. สากล
                        parsedDate = parsedDate.AddYears(-543);
                    }
                    // คืนค่าวินิจฉัยวันที่จอง
                    return parsedDate;
                }
            }

            // ลำดับที่ 3: หากไม่มีข้อมูลข้างต้น ให้ใช้วันเช็คอินเป็นค่าสำรอง (Fallback)
            return booking.CheckInDate.ToDateTime(TimeOnly.MinValue);
        }

        // เมธอดตรวจสอบเงื่อนไขความถูกต้องในการขอคืนเงิน (ต้องไม่เกิน 7 วัน และยังไม่เคยเช็คอินเข้าพัก)
        public static bool IsEligibleForRefund(Booking booking, out string reason, out int daysRemaining)
        {
            // กำหนดค่าเริ่มต้นของจำนวนวันที่เหลืออยู่เป็น 0
            daysRemaining = 0;

            // ตรวจสอบว่ามีคำขอเดิมของรายการจองนี้อยู่ในระบบแล้วหรือไม่
            var existing = GetByBookingId(booking.BookingId);
            // หากมีคำขอเดิมอยู่แล้ว
            if (existing != null)
            {
                // หากสถานะยังอยู่ระหว่างตรวจสอบ
                if (existing.Status == "Pending")
                {
                    // ระบุเหตุผลว่าอยู่ระหว่างดำเนินการ
                    reason = "คำขอคืนเงินอยู่ระหว่างการตรวจสอบโดยฝ่ายบัญชี";
                    // คืนค่า false ไม่อนุญาตให้ยื่นซ้ำ
                    return false;
                }
                // หากได้รับการอนุมัติคืนเงินไปแล้ว
                if (existing.Status == "Approved")
                {
                    // ระบุเหตุผลว่าได้รับเงินคืนเรียบร้อยแล้ว
                    reason = "การจองนี้ได้รับการคืนเงินเรียบร้อยแล้ว";
                    // คืนค่า false
                    return false;
                }
            }

            // ตรวจสอบสถานะการจอง หากถูกยกเลิกหรือคืนเงินไปแล้ว
            if (booking.BookingStatus == "Cancelled" || booking.BookingStatus == "Refunded")
            {
                // ระบุเหตุผล
                reason = "รายการจองนี้ถูกยกเลิกหรือคืนเงินแล้ว";
                // คืนค่า false
                return false;
            }

            // ตรวจสอบว่าแขกได้เช็คอินเข้าพักไปแล้ว หรือเช็คเอาท์เสร็จสิ้นแล้วหรือไม่
            if (booking.BookingStatus == "CheckIn" || booking.BookingStatus == "CheckedIn" || booking.BookingStatus == "Completed")
            {
                // ระบุเหตุผลว่าเข้าพักไปแล้วจึงไม่สามารถขอคืนเงินได้
                reason = "ไม่สามารถขอคืนเงินได้เนื่องจากเข้าพักแล้ว";
                // คืนค่า false
                return false;
            }

            // หาวันที่จองห้องพัก
            DateTime bookingDate = GetBookingDate(booking);
            // คำนวณจำนวนวันที่ผ่านมานับตั้งแต่วันที่จองจนถึงปัจจุบัน
            double daysElapsed = (DateTime.Now - bookingDate).TotalDays;

            // เงื่อนไขกฎ 7 วัน: หากเวลาที่ผ่านไปเกินกว่า 7 วัน
            if (daysElapsed > 7)
            {
                // ระบุเหตุผลว่าเกินกำหนดเวลา 7 วัน
                reason = "ไม่สามารถขอคืนเงินได้ เนื่องจากเกินกำหนด 7 วันหลังจากการจอง";
                // คืนค่า false
                return false;
            }

            // คำนวณจำนวนวันที่เหลืออยู่ในสิทธิ์การขอคืนเงิน (7 วัน - วันที่ผ่านมา)
            daysRemaining = Math.Max(0, 7 - (int)Math.Floor(daysElapsed));
            // ระบุข้อความแจ้งว่ายังอยู่ในเกณฑ์และสามารถยื่นเรื่องได้
            reason = $"สามารถขอคืนเงินได้ (เหลือเวลาอีก {daysRemaining} วัน)";
            // คืนค่า true อนุญาตให้ยื่นขอคืนเงินได้
            return true;
        }
    }
}
