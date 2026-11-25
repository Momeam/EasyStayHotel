using System;
using System.Collections.Generic;

namespace EasyStayHotel.Services
{
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
            string? couponCode,
            string? roomTypeName = null)
        {
            var result = new PromotionResult();
            decimal autoDiscount = 0;
            decimal couponDiscount = 0;

            checkInDate = DateHelper.NormalizeToGregorian(checkInDate);
            if (guestDateOfBirth.HasValue)
            {
                guestDateOfBirth = DateHelper.NormalizeToGregorian(guestDateOfBirth.Value);
            }

            decimal totalRoomPrice = baseRoomPrice * (nights > 0 ? nights : 1) * (rooms > 0 ? rooms : 1);

            int daysInAdvance = (checkInDate.Date - DateTime.Today).Days;

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

            bool isWeekday = checkInDate.DayOfWeek >= DayOfWeek.Monday && checkInDate.DayOfWeek <= DayOfWeek.Thursday;

            bool isBirthdayMonth = guestDateOfBirth.HasValue && (guestDateOfBirth.Value.Month == checkInDate.Month);

            bool isEarlyBirdApplied = false;
            if (daysInAdvance > 30)
            {
                decimal disc = totalRoomPrice * 0.15m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่น Early Bird จองล่วงหน้า >30 วัน (-15%: ฿{disc:N0})");
                isEarlyBirdApplied = true;
            }

            bool isLongStayApplied = false;
            if (nights >= 3)
            {
                decimal disc = (baseRoomPrice * 0.10m) * (rooms > 0 ? rooms : 1);
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นพักยาวคุ้มกว่า (-10% คืนที่ 3: ฿{disc:N0})");
                isLongStayApplied = true;
            }

            bool isBirthdayApplied = false;
            if (isBirthdayMonth)
            {
                decimal disc = totalRoomPrice * 0.10m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นต้อนรับเดือนเกิด (-10%: ฿{disc:N0})");
                isBirthdayApplied = true;
            }

            bool isWeekdayApplied = false;
            if (isWeekday && isDeluxeOrAbove)
            {
                decimal disc = totalRoomPrice * 0.05m;
                autoDiscount += disc;
                result.AppliedPromotions.Add($"โปรโมชั่นวันธรรมดาน่าพัก Deluxe+ (-5%: ฿{disc:N0})");
                isWeekdayApplied = true;
            }

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                string cleanCode = couponCode.Trim().ToUpper();

                switch (cleanCode)
                {
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

                    case "WELCOME100":
                        decimal welcomeDisc = Math.Min(100m, totalRoomPrice);
                        couponDiscount += welcomeDisc;
                        result.AppliedPromotions.Add($"คูปอง WELCOME100 ต้อนรับสมาชิกใหม่ (-฿{welcomeDisc:N0})");
                        break;

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

            result.TotalDiscount = Math.Min(autoDiscount + couponDiscount, totalRoomPrice);
            return result;
        }
    }
}
