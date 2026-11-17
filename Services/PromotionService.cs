using System;
using System.Collections.Generic;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// คลาส PromotionResult: โครงสร้างข้อมูลสำหรับส่งผลลัพธ์การคำนวณโปรโมชั่นกลับไปยัง Controller หรือ View
    /// </summary>
    public class PromotionResult
    {
        /// <summary>
        /// ยอดส่วนลดรวมทั้งหมดที่ลูกค้าได้รับ (หน่วย: บาท)
        /// </summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// รายการชื่อโปรโมชั่นหรือคูปองที่ถูกนำมาคำนวณส่วนลดสำเร็จ
        /// </summary>
        public List<string> AppliedPromotions { get; set; } = new List<string>();

        /// <summary>
        /// ข้อความแจ้งเตือนข้อผิดพลาด (เช่น โค้ดคูปองไม่ถูกต้อง, เงื่อนไขไม่ครบ)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// เซอร์วิส PromotionService: ระบบคำนวณโปรโมชั่นและส่วนลดห้องพักของ Easy Stay Hotel
    /// 
    /// ตรรกะทางธุรกิจ (Business Rules):
    /// 1. โปรโมชั่นจองล่วงหน้า (Early Bird): จองล่วงหน้า >30 วัน รับส่วนลด 15% จากราคาห้องพักรวม
    /// 2. โปรโมชั่นพักยาวคุ้มกว่า (Long Stay): จองพักต่อเนื่อง >= 3 คืน ลดค่าห้องในคืนที่ 3 ลง 10% (คำนวณลดเฉพาะคืนที่ 3)
    /// 3. โปรโมชั่นต้อนรับเดือนเกิด (Birthday): ลูกค้าเข้าพักในเดือนเกิดของตนเอง รับส่วนลด 10%
    /// 4. โปรโมชั่นเข้าพักแบบกลุ่ม (Group): จองห้องพักพร้อมกัน >= 2 ห้อง รับส่วนลดห้องละ 15% (เปิดใช้ผ่านคูปอง GROUP15)
    /// 5. โปรโมชั่นวันธรรมดาน่าพัก (Weekday): จองเข้าพักวันธรรมดา (จันทร์-พฤหัสบดี) เฉพาะห้อง Deluxe ขึ้นไป รับส่วนลด 5%
    /// </summary>
    public class PromotionService
    {
        /// <summary>
        /// คำนวณส่วนลดทั้งหมด ทั้งส่วนลดอัตโนมัติ (Auto Applied) และส่วนลดจากโค้ดคูปอง (Coupon Code)
        /// </summary>
        /// <param name="baseRoomPrice">ราคาห้องพักต่อคืน</param>
        /// <param name="nights">จำนวนคืนที่เข้าพัก</param>
        /// <param name="rooms">จำนวนห้องที่จอง</param>
        /// <param name="checkInDate">วันที่เช็คอิน</param>
        /// <param name="guestDateOfBirth">วันเดือนปีเกิดของลูกค้า (ถ้ามี)</param>
        /// <param name="couponCode">รหัสคูปองส่วนลดที่ลูกค้ากรอก</param>
        /// <param name="roomTypeName">ชื่อประเภทห้องพัก (ใช้ตรวจสอบเงื่อนไขห้อง Deluxe ขึ้นไป)</param>
        /// <returns>PromotionResult ที่มียอดส่วนลดรวม รายชื่อโปรโมชั่นที่ได้รับ และข้อความแจ้งเตือน</returns>
        public PromotionResult CalculateDiscount(
            decimal baseRoomPrice, 
            int nights, 
            int rooms, 
            DateTime checkInDate, 
            DateTime? guestDateOfBirth, 
            string? couponCode,
            string? roomTypeName = null)
        {
            var result = new PromotionResult();
            decimal autoDiscount = 0;
            decimal couponDiscount = 0;
            
            // คำนวณยอดรวมค่าห้องพักก่อนหักส่วนลด (ราคาต่อคืน x จำนวนคืน x จำนวนห้อง)
            decimal totalRoomPrice = baseRoomPrice * (nights > 0 ? nights : 1) * (rooms > 0 ? rooms : 1);

            // คำนวณจำนวนวันจองล่วงหน้า (Check-in ล่วงหน้ากี่วันนับจากวันนี้)
            int daysInAdvance = (checkInDate.Date - DateTime.Today).Days;

            // ตรวจสอบเงื่อนไขว่าเป็นห้องประเภท Deluxe ขึ้นไปหรือไม่ (ดีลักซ์, สวีท, วิลล่า หรือราคาพื้นฐาน >= 2,000 บาท)
            bool isDeluxeOrAbove = !string.IsNullOrEmpty(roomTypeName) && (
                roomTypeName.IndexOf("Deluxe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("ดีลักซ์", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("Suite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("สวีท", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("Executive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("Family", StringComparison.OrdinalIgnoreCase) >= 0 ||
                roomTypeName.IndexOf("Villa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                baseRoomPrice >= 2000m
            );

            // ตรวจสอบเงื่อนไขว่าวันเช็คอินเป็นวันธรรมดา (จันทร์ ถึง พฤหัสบดี) หรือไม่
            bool isWeekday = checkInDate.DayOfWeek >= DayOfWeek.Monday && checkInDate.DayOfWeek <= DayOfWeek.Thursday;

            // ตรวจสอบเงื่อนไขว่าเดือนที่เข้าพักตรงกับเดือนเกิดของลูกค้าหรือไม่
            bool isBirthdayMonth = guestDateOfBirth.HasValue && (guestDateOfBirth.Value.Month == checkInDate.Month);

            // =========================================================================
            // กลุ่มที่ 1: ส่วนลดอัตโนมัติ (Auto-applied Discounts) คำนวณให้ทันทีเมื่อเข้าเงื่อนไข
            // =========================================================================

            // 1. โปรโมชั่น Early Bird: จองล่วงหน้ามากกว่า 30 วัน ลด 15% จากราคารวม
            bool isEarlyBirdApplied = false;
            if (daysInAdvance > 30)
            {
                decimal disc = totalRoomPrice * 0.15m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่น Early Bird จองล่วงหน้า >30 วัน (-15%: ฿{disc:N0})");
                isEarlyBirdApplied = true;
            }

            // 2. โปรโมชั่นพักยาวคุ้มกว่า: จองต่อเนื่องตั้งแต่ 3 คืนขึ้นไป ลดเฉพาะคืนที่ 3 ลง 10%
            bool isLongStayApplied = false;
            if (nights >= 3)
            {
                // ลด 10% ของราคา 1 คืน สำหรับทุกห้องที่จอง
                decimal disc = (baseRoomPrice * 0.10m) * (rooms > 0 ? rooms : 1);
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นพักยาวคุ้มกว่า (-10% คืนที่ 3: ฿{disc:N0})");
                isLongStayApplied = true;
            }

            // 3. โปรโมชั่นต้อนรับเดือนเกิด: เข้าพักในเดือนเกิด ลด 10% จากราคารวม
            bool isBirthdayApplied = false;
            if (isBirthdayMonth)
            {
                decimal disc = totalRoomPrice * 0.10m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นต้อนรับเดือนเกิด (-10%: ฿{disc:N0})");
                isBirthdayApplied = true;
            }

            // 5. โปรโมชั่นวันธรรมดาน่าพัก: เช็คอินวันจันทร์-พฤหัสบดี สำหรับห้อง Deluxe ขึ้นไป ลด 5%
            bool isWeekdayApplied = false;
            if (isWeekday && isDeluxeOrAbove)
            {
                decimal disc = totalRoomPrice * 0.05m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นวันธรรมดาน่าพัก Deluxe+ (-5%: ฿{disc:N0})");
                isWeekdayApplied = true;
            }

            // =========================================================================
            // กลุ่มที่ 2: ระบบคูปองส่วนลดพิเศษ (Coupon Codes)
            // =========================================================================
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                string cleanCode = couponCode.Trim().ToUpper();

                switch (cleanCode)
                {
                    // 4. โปรโมชั่นเข้าพักแบบกลุ่ม: เมื่อจองตั้งแต่ 2 ห้องขึ้นไปพร้อมกัน รับส่วนลดห้องละ 15%
                    case "GROUP15":
                    case "EASYGROUP":
                        if (rooms >= 2)
                        {
                            decimal disc = totalRoomPrice * 0.15m;
                            couponDiscount += disc;
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} เข้าพักแบบกลุ่ม >=2 ห้อง (-15%: ฿{disc:N0})");
                        }
                        else
                        {
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เมื่อจองห้องพักตั้งแต่ 2 ห้องขึ้นไปพร้อมกันเท่านั้น (ปัจจุบันจอง {rooms} ห้อง)";
                        }
                        break;

                    // คูปอง Early Bird (กรณีลูกค้าต้องการกรอกรหัสด้วยตนเอง)
                    case "EARLYBIRD":
                    case "EARLY15":
                    case "EARLY30":
                        if (isEarlyBirdApplied)
                        {
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลด Early Bird 15% ให้อัตโนมัติแล้ว";
                        }
                        else if (daysInAdvance > 30)
                        {
                            decimal disc = totalRoomPrice * 0.15m;
                            couponDiscount += disc;
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} จองล่วงหน้า (-15%: ฿{disc:N0})");
                        }
                        else
                        {
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เฉพาะการจองล่วงหน้ามากกว่า 30 วันเท่านั้น (ปัจจุบันจองล่วงหน้า {daysInAdvance} วัน)";
                        }
                        break;

                    // คูปอง พักยาวคุ้มกว่า
                    case "LONGSTAY":
                        if (isLongStayApplied)
                        {
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดพักยาวคุ้มกว่า 10% (คืนที่ 3) ให้อัตโนมัติแล้ว";
                        }
                        else if (nights >= 3)
                        {
                            decimal disc = (baseRoomPrice * 0.10m) * (rooms > 0 ? rooms : 1);
                            couponDiscount += disc;
                            result.AppliedPromotions.Add($"คูปอง LONGSTAY (-10% คืนที่ 3: ฿{disc:N0})");
                        }
                        else
                        {
                            result.ErrorMessage = $"คูปอง LONGSTAY ใช้ได้เมื่อจองพักต่อเนื่องตั้งแต่ 3 คืนขึ้นไปเท่านั้น (ปัจจุบันจอง {nights} คืน)";
                        }
                        break;

                    // คูปอง ต้อนรับเดือนเกิด
                    case "BIRTHDAY10":
                    case "HBD10":
                        if (isBirthdayApplied)
                        {
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดต้อนรับเดือนเกิด 10% ให้อัตโนมัติแล้ว";
                        }
                        else if (isBirthdayMonth)
                        {
                            decimal disc = totalRoomPrice * 0.10m;
                            couponDiscount += disc;
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} เดือนเกิด (-10%: ฿{disc:N0})");
                        }
                        else if (!guestDateOfBirth.HasValue)
                        {
                            result.ErrorMessage = "กรุณาเข้าสู่ระบบด้วยบัญชีสมาชิกที่มีข้อมูลวันเกิด เพื่อใช้คูปองต้อนรับเดือนเกิด";
                        }
                        else
                        {
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เฉพาะการเข้าพักในเดือนเกิดของคุณเท่านั้น (เดือนเกิด: {guestDateOfBirth.Value.Month}, เดือนที่เข้าพัก: {checkInDate.Month})";
                        }
                        break;

                    // คูปอง วันธรรมดาน่าพัก
                    case "WEEKDAY5":
                        if (isWeekdayApplied)
                        {
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดวันธรรมดาสำหรับห้อง Deluxe+ 5% ให้อัตโนมัติแล้ว";
                        }
                        else if (!isWeekday)
                        {
                            result.ErrorMessage = "คูปอง WEEKDAY5 ใช้ได้เฉพาะการเข้าพักในวันธรรมดา (จันทร์ - พฤหัสบดี) เท่านั้น";
                        }
                        else if (!isDeluxeOrAbove)
                        {
                            result.ErrorMessage = "คูปอง WEEKDAY5 ใช้ได้เฉพาะห้องพักประเภท Deluxe ขึ้นไปเท่านั้น";
                        }
                        else
                        {
                            decimal disc = totalRoomPrice * 0.05m;
                            couponDiscount += disc;
                            result.AppliedPromotions.Add($"คูปอง WEEKDAY5 (-5%: ฿{disc:N0})");
                        }
                        break;

                    // คูปองต้อนรับสมาชิกใหม่
                    case "WELCOME100":
                        decimal welcomeDisc = Math.Min(100m, totalRoomPrice);
                        couponDiscount += welcomeDisc;
                        result.AppliedPromotions.Add($"คูปอง WELCOME100 ต้อนรับสมาชิกใหม่ (-฿{welcomeDisc:N0})");
                        break;

                    // คูปองพิเศษ Easy Stay
                    case "EASYSTAY":
                        decimal easyDisc = totalRoomPrice * 0.05m;
                        couponDiscount += easyDisc;
                        result.AppliedPromotions.Add($"คูปอง EASYSTAY ส่วนลดพิเศษ (-5%: ฿{easyDisc:N0})");
                        break;

                    default:
                        result.ErrorMessage = $"รหัสคูปอง '{cleanCode}' ไม่ถูกต้อง หรือหมดอายุแล้ว";
                        break;
                }
            }

            // คำนวณยอดส่วนลดรวม (จำกัดไม่ให้ส่วนลดเกินมูลค่าห้องพักทั้งหมด)
            result.TotalDiscount = Math.Min(autoDiscount + couponDiscount, totalRoomPrice);
            return result;
        }
    }
}
