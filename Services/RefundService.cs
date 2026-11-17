using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    public static class RefundService
    {
        private static readonly object _lock = new object();
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string _filePath = Path.Combine(_dataDir, "refund_requests.json");

        private static void EnsureFileExists()
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            if (!File.Exists(_filePath))
            {
                var initialList = new List<RefundRequest>();
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public static List<RefundRequest> GetAll(string? branch = null, string? status = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    if (!string.IsNullOrEmpty(branch) && branch != "ALL")
                    {
                        list = list.Where(r => r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (!string.IsNullOrEmpty(status) && status != "ALL")
                    {
                        list = list.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    return list.OrderByDescending(r => r.RequestedAt).ToList();
                }
                catch
                {
                    return new List<RefundRequest>();
                }
            }
        }

        public static RefundRequest? GetById(string refundId)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();
                    return list.FirstOrDefault(r => r.RefundId == refundId);
                }
                catch
                {
                    return null;
                }
            }
        }

        public static RefundRequest? GetByBookingId(int bookingId)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();
                    return list.FirstOrDefault(r => r.BookingId == bookingId);
                }
                catch
                {
                    return null;
                }
            }
        }

        public static bool CreateRequest(RefundRequest request)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    // ตรวจสอบว่าเคยมีคำขอเดิมที่ยังรอหรืออนุมัติแล้วหรือไม่
                    var existing = list.FirstOrDefault(r => r.BookingId == request.BookingId);
                    if (existing != null && existing.Status != "Rejected")
                    {
                        return false;
                    }

                    if (existing != null && existing.Status == "Rejected")
                    {
                        list.Remove(existing);
                    }

                    request.RequestedAt = DateTime.Now;
                    request.RefundDeadline = DateTime.Now.AddDays(3); // กำหนดคืนเงินภายใน 3 วันทำการ
                    request.Status = "Pending";

                    list.Add(request);
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool Approve(string refundId, string adminName, string? adminNote = null, string? slipUrl = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    var target = list.FirstOrDefault(r => r.RefundId == refundId);
                    if (target == null) return false;

                    target.Status = "Approved";
                    target.ProcessedBy = adminName;
                    target.ProcessedAt = DateTime.Now;
                    if (!string.IsNullOrEmpty(adminNote)) target.AdminNote = adminNote;
                    if (!string.IsNullOrEmpty(slipUrl)) target.RefundSlipUrl = slipUrl;

                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool Reject(string refundId, string adminName, string rejectionReason)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<RefundRequest>>(json) ?? new List<RefundRequest>();

                    var target = list.FirstOrDefault(r => r.RefundId == refundId);
                    if (target == null) return false;

                    target.Status = "Rejected";
                    target.ProcessedBy = adminName;
                    target.ProcessedAt = DateTime.Now;
                    target.AdminNote = rejectionReason;

                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        // คำนวณวันที่ทำรายการจอง (Booking Date)
        public static DateTime GetBookingDate(Booking booking)
        {
            // 1. ตรวจสอบจาก TransferDate ในตาราง Payment
            var paymentDate = booking.Payments?.OrderBy(p => p.PaymentId).FirstOrDefault()?.TransferDate;
            if (paymentDate.HasValue) return paymentDate.Value;

            // 2. ดึงจากหมายเลข BookingRef เช่น "ES-20261116120000" หรือ "WK-25691109205114"
            if (!string.IsNullOrEmpty(booking.BookingRef))
            {
                var match = Regex.Match(booking.BookingRef, @"\d{8}");
                if (match.Success && DateTime.TryParseExact(match.Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    if (parsedDate.Year > 2400)
                    {
                        parsedDate = parsedDate.AddYears(-543);
                    }
                    return parsedDate;
                }
            }

            // 3. Fallback เป็นวันเช็คอิน หรือ เวลาปัจจุบัน
            return booking.CheckInDate.ToDateTime(TimeOnly.MinValue);
        }

        // ตรวจสอบเงื่อนไขว่าสามารถขอคืนเงินได้หรือไม่ (เงื่อนไข ภายใน 7 วันหลังจากการจอง)
        public static bool IsEligibleForRefund(Booking booking, out string reason, out int daysRemaining)
        {
            daysRemaining = 0;

            // 1. ตรวจสอบว่ามีคำขอเดิมอยู่แล้วหรือไม่
            var existing = GetByBookingId(booking.BookingId);
            if (existing != null)
            {
                if (existing.Status == "Pending")
                {
                    reason = "คำขอคืนเงินอยู่ระหว่างการตรวจสอบโดยฝ่ายบัญชี";
                    return false;
                }
                if (existing.Status == "Approved")
                {
                    reason = "การจองนี้ได้รับการคืนเงินเรียบร้อยแล้ว";
                    return false;
                }
            }

            // 2. ตรวจสอบสถานะการจอง
            if (booking.BookingStatus == "Cancelled" || booking.BookingStatus == "Refunded")
            {
                reason = "รายการจองนี้ถูกยกเลิกหรือคืนเงินแล้ว";
                return false;
            }

            if (booking.BookingStatus == "CheckIn" || booking.BookingStatus == "CheckedIn" || booking.BookingStatus == "Completed")
            {
                reason = "ไม่สามารถขอคืนเงินได้เนื่องจากเข้าพักแล้ว";
                return false;
            }

            // 3. ตรวจสอบเงื่อนไข 7 วันหลังจากการจอง
            DateTime bookingDate = GetBookingDate(booking);
            double daysElapsed = (DateTime.Now - bookingDate).TotalDays;

            if (daysElapsed > 7)
            {
                reason = "ไม่สามารถขอคืนเงินได้ เนื่องจากเกินกำหนด 7 วันหลังจากการจอง";
                return false;
            }

            daysRemaining = Math.Max(0, 7 - (int)Math.Floor(daysElapsed));
            reason = $"สามารถขอคืนเงินได้ (เหลือเวลาอีก {daysRemaining} วัน)";
            return true;
        }
    }
}
