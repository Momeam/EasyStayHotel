using System;
using System.Collections.Generic;

namespace EasyStayHotel.Services
{
    // คลาสสำหรับเก็บผลลัพธ์การคำนวณเพื่อส่งกลับไปที่ Controller/หน้าเว็บ
    public class PromotionResult
    {
        public decimal TotalDiscount { get; set; }
        public List<string> AppliedPromotions { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
    }

    public class PromotionService
    {
        public PromotionResult CalculateDiscount(
            decimal baseRoomPrice, 
            int nights, 
            int rooms, 
            DateTime checkInDate, 
            DateTime? guestDateOfBirth, 
            string? couponCode)
        {
            var result = new PromotionResult();
            decimal autoDiscount = 0;
            decimal couponDiscount = 0;
            decimal totalRoomPrice = baseRoomPrice * nights * rooms;

            // ==========================================
            // กลุ่มที่ 1: ส่วนลดอัตโนมัติ (Auto Discounts)
            // ==========================================

            // 1. พักยาว 3 คืนขึ้นไป (ลด 10% เฉพาะคืนที่ 3)
            if (nights >= 3)
            {
                decimal discount = baseRoomPrice * 0.10m; 
                autoDiscount += discount;
                result.AppliedPromotions.Add("ส่วนลดพักยาวคุ้มกว่า (10% คืนที่ 3)");
            }

            // 2. วันธรรมดาน่าพัก (ลด 5% จากยอดรวมห้อง)
            if (checkInDate.DayOfWeek >= DayOfWeek.Monday && checkInDate.DayOfWeek <= DayOfWeek.Thursday)
            {
                decimal discount = totalRoomPrice * 0.05m;
                autoDiscount += discount;
                result.AppliedPromotions.Add("ส่วนลดเข้าพักวันธรรมดา (5%)");
            }

            // 3. ต้อนรับเดือนเกิด (ลด 10% จากยอดรวมห้อง)
            if (guestDateOfBirth.HasValue && guestDateOfBirth.Value.Month == checkInDate.Month)
            {
                decimal discount = totalRoomPrice * 0.10m;
                autoDiscount += discount;
                result.AppliedPromotions.Add("ส่วนลดต้อนรับเดือนเกิด (10%)");
            }

            // ==========================================
            // กลุ่มที่ 2: ระบบคูปอง (Coupon Codes)
            // ==========================================
            if (!string.IsNullOrEmpty(couponCode))
            {
                couponCode = couponCode.Trim().ToUpper();

                // คูปอง 1: Early Bird จองล่วงหน้า 30 วัน
                if (couponCode == "EARLY30" || couponCode == "EASYBIRDBLUE")
                {
                    int daysInAdvance = (checkInDate - DateTime.Today).Days;
                    if (daysInAdvance >= 30)
                    {
                        couponDiscount = totalRoomPrice * 0.15m;
                        result.AppliedPromotions.Add($"คูปอง {couponCode} (-15%)");
                    }
                    else
                    {
                        result.ErrorMessage = "คูปองนี้ใช้ได้เมื่อจองล่วงหน้า 30 วันขึ้นไปเท่านั้น";
                    }
                }
                // คูปอง 2: จองแบบกลุ่มตั้งแต่ 2 ห้องขึ้นไป
                else if (couponCode == "GROUP15")
                {
                    if (rooms >= 2)
                    {
                        couponDiscount = totalRoomPrice * 0.15m;
                        result.AppliedPromotions.Add($"คูปอง GROUP15 (-15%)");
                    }
                    else
                    {
                        result.ErrorMessage = "คูปอง GROUP15 ใช้ได้เมื่อจองตั้งแต่ 2 ห้องขึ้นไปเท่านั้น";
                    }
                }
                else
                {
                    result.ErrorMessage = "รหัสส่วนลดไม่ถูกต้อง หรือหมดอายุแล้ว";
                }
            }

            // สรุปยอดส่วนลดทั้งหมด
            result.TotalDiscount = autoDiscount + couponDiscount;
            return result;
        }
    }
}