// นำเข้า System สำหรับฟังก์ชันพื้นฐานและ DateTime
using System;
// นำเข้า System.Collections.Generic สำหรับโครงสร้างข้อมูล List
using System.Collections.Generic;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาส PromotionResult สำหรับเก็บผลลัพธ์การคำนวณส่วนลดทั้งหมด
    public class PromotionResult
    {
        // พร็อพเพอร์ตี้เก็บยอดเงินส่วนลดรวมทั้งหมด (บาท)
        public decimal TotalDiscount { get; set; }

        // พร็อพเพอร์ตี้เก็บรายการข้อความระบุชื่อโปรโมชั่นหรือคูปองที่ได้รับส่วนลด
        public List<string> AppliedPromotions { get; set; } = new List<string>();

        // พร็อพเพอร์ตี้เก็บข้อความแจ้งเตือนข้อผิดพลาด เช่น โค้ดคูปองไม่ถูกต้อง หรือไม่ตรงเงื่อนไข
        public string? ErrorMessage { get; set; }
    }

    // เซอร์วิสหลักสำหรับคำนวณโปรโมชั่นของโรงแรม ทั้งส่วนลดอัตโนมัติและรหัสคูปอง
    public class PromotionService
    {
        // ฟังก์ชันคำนวณส่วนลดตามเงื่อนไขโปรโมชั่นทั้งหมดของระบบ
        public PromotionResult CalculateDiscount(
            // ราคาพื้นฐานต่อห้องต่อคืน
            decimal baseRoomPrice,
            // จำนวนคืนที่เข้าพัก
            int nights,
            // จำนวนห้องที่จอง
            int rooms,
            // วันที่เช็คอินเข้าพัก
            DateTime checkInDate,
            // วันเกิดของลูกค้า (ถ้ามีระบุในโปรไฟล์สมาชิก)
            DateTime? guestDateOfBirth,
            // รหัสคูปองส่วนลดที่ลูกค้ากรอก
            string? couponCode,
            // ชื่อประเภทห้องพัก (ใช้ตรวจสอบเงื่อนไขห้อง Deluxe ขึ้นไป)
            string? roomTypeName = null)
        {
            // สร้างอ็อบเจกต์ผลลัพธ์เริ่มต้น
            var result = new PromotionResult();
            // ตัวแปรรวมยอดส่วนลดอัตโนมัติ
            decimal autoDiscount = 0;
            // ตัวแปรรวมยอดส่วนลดจากคูปอง
            decimal couponDiscount = 0;

            // ปรับเทียบวันที่เช็คอินให้อยู่ในปี ค.ศ. สากล ป้องกันปัญหาปี พ.ศ. คลาดเคลื่อน
            checkInDate = DateHelper.NormalizeToGregorian(checkInDate);
            // ตรวจสอบว่ามีข้อมูลวันเกิดของลูกค้าหรือไม่
            if (guestDateOfBirth.HasValue)
            {
                // หากมีวันเกิด ให้ปรับเทียบปีเกิดเป็น ค.ศ. สากล
                guestDateOfBirth = DateHelper.NormalizeToGregorian(guestDateOfBirth.Value);
            }

            // คำนวณราคาเต็มรวมของห้องพัก (ราคาต่อคืน x จำนวนคืน x จำนวนห้อง)
            decimal totalRoomPrice = baseRoomPrice * (nights > 0 ? nights : 1) * (rooms > 0 ? rooms : 1);

            // คำนวณจำนวนวันที่ลูกค้าจองล่วงหน้าเมื่อเทียบกับวันปัจจุบัน
            int daysInAdvance = (checkInDate.Date - DateTime.Today).Days;

            // ตรวจสอบว่าห้องที่เลือกเป็นระดับ Deluxe ขึ้นไปหรือไม่ (ตรวจสอบจากชื่อประเภทห้องหรือราคาต่อคืน)
            bool isDeluxeOrAbove = !string.IsNullOrEmpty(roomTypeName) && (
                // มีคำว่า Deluxe
                roomTypeName.IndexOf("Deluxe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า ดีลักซ์ ภาษาไทย
                roomTypeName.IndexOf("ดีลักซ์", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า Suite
                roomTypeName.IndexOf("Suite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า สวีท ภาษาไทย
                roomTypeName.IndexOf("สวีท", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า Executive
                roomTypeName.IndexOf("Executive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า Family
                roomTypeName.IndexOf("Family", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือมีคำว่า Villa
                roomTypeName.IndexOf("Villa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                // หรือราคาต่อคืนตั้งแต่ 2,000 บาทขึ้นไป
                baseRoomPrice >= 2000m
            );

            // ตรวจสอบว่าวันเช็คอินเป็นวันธรรมดา (จันทร์ - พฤหัสบดี) หรือไม่
            bool isWeekday = checkInDate.DayOfWeek >= DayOfWeek.Monday && checkInDate.DayOfWeek <= DayOfWeek.Thursday;

            // ตรวจสอบว่าเดือนที่เข้าพักตรงกับเดือนเกิดของลูกค้าหรือไม่
            bool isBirthdayMonth = guestDateOfBirth.HasValue && (guestDateOfBirth.Value.Month == checkInDate.Month);

            // ตัวแปรสถานะว่าได้รับส่วนลด Early Bird หรือไม่
            bool isEarlyBirdApplied = false;
            // เงื่อนไขโปรโมชั่น 1: จองล่วงหน้ามากกว่า 30 วัน
            if (daysInAdvance > 30)
            {
                // คำนวณส่วนลด 15% จากราคาเต็มรวมห้องพัก
                decimal disc = totalRoomPrice * 0.15m;
                // บวกยอดเข้าส่วนลดอัตโนมัติ
                autoDiscount += disc;
                // เพิ่มชื่อโปรโมชั่นลงในรายการที่ได้รับ
                result.AppliedPromotions.Add($"โปรโมชั่น Early Bird จองล่วงหน้า >30 วัน (-15%: ฿{disc:N0})");
                // ปรับสถานะเป็นจริง
                isEarlyBirdApplied = true;
            }

            // ตัวแปรสถานะว่าได้รับส่วนลด Long Stay หรือไม่
            bool isLongStayApplied = false;
            // เงื่อนไขโปรโมชั่น 2: จองพักต่อเนื่องตั้งแต่ 3 คืนขึ้นไป
            if (nights >= 3)
            {
                // คำนวณส่วนลด 10% เฉพาะคืนที่ 3 สำหรับทุกห้องที่จอง
                decimal disc = (baseRoomPrice * 0.10m) * (rooms > 0 ? rooms : 1);
                // บวกยอดเข้าส่วนลดอัตโนมัติ
                autoDiscount += disc;
                // เพิ่มชื่อโปรโมชั่นลงในรายการที่ได้รับ
                result.AppliedPromotions.Add($"โปรโมชั่นพักยาวคุ้มกว่า (-10% คืนที่ 3: ฿{disc:N0})");
                // ปรับสถานะเป็นจริง
                isLongStayApplied = true;
            }

            // ตัวแปรสถานะว่าได้รับส่วนลดเดือนเกิดหรือไม่
            bool isBirthdayApplied = false;
            // เงื่อนไขโปรโมชั่น 3: เข้าพักตรงกับเดือนเกิดของลูกค้า
            if (isBirthdayMonth)
            {
                // คำนวณส่วนลด 10% จากราคาเต็มรวมห้องพัก
                decimal disc = totalRoomPrice * 0.10m;
                // บวกยอดเข้าส่วนลดอัตโนมัติ
                autoDiscount += disc;
                // เพิ่มชื่อโปรโมชั่นลงในรายการที่ได้รับ
                result.AppliedPromotions.Add($"โปรโมชั่นต้อนรับเดือนเกิด (-10%: ฿{disc:N0})");
                // ปรับสถานะเป็นจริง
                isBirthdayApplied = true;
            }

            // ตัวแปรสถานะว่าได้รับส่วนลดวันธรรมดาหรือไม่
            bool isWeekdayApplied = false;
            // เงื่อนไขโปรโมชั่น 4: วันธรรมดาน่าพัก (จันทร์-พฤหัสบดี) เฉพาะห้อง Deluxe ขึ้นไป
            if (isWeekday && isDeluxeOrAbove)
            {
                // คำนวณส่วนลด 5% จากราคาเต็มรวมห้องพัก
                decimal disc = totalRoomPrice * 0.05m;
                // บวกยอดเข้าส่วนลดอัตโนมัติ
                autoDiscount += disc;
                // เพิ่มชื่อโปรโมชั่นลงในรายการที่ได้รับ
                result.AppliedPromotions.Add($"โปรโมชั่นวันธรรมดาน่าพัก Deluxe+ (-5%: ฿{disc:N0})");
                // ปรับสถานะเป็นจริง
                isWeekdayApplied = true;
            }

            // ตรวจสอบว่าลูกค้ามีการกรอกรหัสคูปองเข้ามาหรือไม่
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                // ตัดช่องว่างและแปลงรหัสคูปองเป็นตัวพิมพ์ใหญ่ทั้งหมดเพื่อตรวจสอบ
                string cleanCode = couponCode.Trim().ToUpper();

                // สวิตช์ตรวจสอบรหัสคูปองแต่ละประเภท
                switch (cleanCode)
                {
                    // โปรโมชั่น 5: คูปองจองเข้าพักแบบกลุ่ม 2 ห้องขึ้นไป
                    case "GROUP15":
                    case "EASYGROUP":
                        // ตรวจสอบว่าจองตั้งแต่ 2 ห้องขึ้นไปหรือไม่
                        if (rooms >= 2)
                        {
                            // คำนวณส่วนลด 15% จากราคาเต็มรวมห้องพัก
                            decimal disc = totalRoomPrice * 0.15m;
                            // บวกยอดเข้าส่วนลดคูปอง
                            couponDiscount += disc;
                            // บันทึกชื่อคูปองและยอดส่วนลด
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} เข้าพักแบบกลุ่ม >=2 ห้อง (-15%: ฿{disc:N0})");
                        }
                        else
                        {
                            // แจ้งเตือนกรณีจองไม่ถึง 2 ห้อง
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เมื่อจองห้องพักตั้งแต่ 2 ห้องขึ้นไปพร้อมกันเท่านั้น (ปัจจุบันจอง {rooms} ห้อง)";
                        }
                        break;

                    // คูปอง Early Bird แบบกรอกรหัสเอง
                    case "EARLYBIRD":
                    case "EARLY15":
                    case "EARLY30":
                        // ตรวจสอบว่าได้รับส่วนลด Early Bird อัตโนมัติไปแล้วหรือไม่
                        if (isEarlyBirdApplied)
                        {
                            // แจ้งเตือนว่าได้รับสิทธิ์อัตโนมัติไปแล้ว
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลด Early Bird 15% ให้อัตโนมัติแล้ว";
                        }
                        // ตรวจสอบว่าจองล่วงหน้ามากกว่า 30 วันหรือไม่
                        else if (daysInAdvance > 30)
                        {
                            // คำนวณส่วนลด 15%
                            decimal disc = totalRoomPrice * 0.15m;
                            // บวกยอดเข้าส่วนลดคูปอง
                            couponDiscount += disc;
                            // บันทึกรายการ
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} จองล่วงหน้า (-15%: ฿{disc:N0})");
                        }
                        else
                        {
                            // แจ้งเตือนกรณีจองล่วงหน้าไม่ถึง 30 วัน
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เฉพาะการจองล่วงหน้ามากกว่า 30 วันเท่านั้น (ปัจจุบันจองล่วงหน้า {daysInAdvance} วัน)";
                        }
                        break;

                    // คูปองพักยาวคุ้มกว่า
                    case "LONGSTAY":
                        // ตรวจสอบว่าได้รับส่วนลดพักยาวอัตโนมัติไปแล้วหรือไม่
                        if (isLongStayApplied)
                        {
                            // แจ้งเตือนว่าได้รับสิทธิ์อัตโนมัติไปแล้ว
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดพักยาวคุ้มกว่า 10% (คืนที่ 3) ให้อัตโนมัติแล้ว";
                        }
                        // ตรวจสอบว่าพัก 3 คืนขึ้นไปหรือไม่
                        else if (nights >= 3)
                        {
                            // คำนวณลด 10% ของคืนที่ 3
                            decimal disc = (baseRoomPrice * 0.10m) * (rooms > 0 ? rooms : 1);
                            // บวกยอดเข้าส่วนลดคูปอง
                            couponDiscount += disc;
                            // บันทึกรายการ
                            result.AppliedPromotions.Add($"คูปอง LONGSTAY (-10% คืนที่ 3: ฿{disc:N0})");
                        }
                        else
                        {
                            // แจ้งเตือนกรณีจองไม่ถึง 3 คืน
                            result.ErrorMessage = $"คูปอง LONGSTAY ใช้ได้เมื่อจองพักต่อเนื่องตั้งแต่ 3 คืนขึ้นไปเท่านั้น (ปัจจุบันจอง {nights} คืน)";
                        }
                        break;

                    // คูปองส่วนลดเดือนเกิด
                    case "BIRTHDAY10":
                    case "HBD10":
                        // ตรวจสอบว่าได้รับส่วนลดเดือนเกิดอัตโนมัติไปแล้วหรือไม่
                        if (isBirthdayApplied)
                        {
                            // แจ้งเตือนว่าระบบลดให้อัตโนมัติแล้ว
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดต้อนรับเดือนเกิด 10% ให้อัตโนมัติแล้ว";
                        }
                        // ตรวจสอบว่าเดือนเช็คอินตรงกับเดือนเกิดหรือไม่
                        else if (isBirthdayMonth)
                        {
                            // คำนวณลด 10% จากราคารวม
                            decimal disc = totalRoomPrice * 0.10m;
                            // บวกยอดเข้าส่วนลดคูปอง
                            couponDiscount += disc;
                            // บันทึกรายการ
                            result.AppliedPromotions.Add($"คูปอง {cleanCode} เดือนเกิด (-10%: ฿{disc:N0})");
                        }
                        // ตรวจสอบกรณีไม่มีข้อมูลวันเกิด
                        else if (!guestDateOfBirth.HasValue)
                        {
                            // แจ้งเตือนให้เข้าสู่ระบบเพื่อยืนยันวันเกิด
                            result.ErrorMessage = "กรุณาเข้าสู่ระบบด้วยบัญชีสมาชิกที่มีข้อมูลวันเกิด เพื่อใช้คูปองต้อนรับเดือนเกิด";
                        }
                        else
                        {
                            // แจ้งเตือนกรณีเดือนเข้าพักไม่ตรงกับเดือนเกิด
                            result.ErrorMessage = $"คูปอง {cleanCode} ใช้ได้เฉพาะการเข้าพักในเดือนเกิดของคุณเท่านั้น (เดือนเกิด: {guestDateOfBirth.Value.Month}, เดือนที่เข้าพัก: {checkInDate.Month})";
                        }
                        break;

                    // คูปองส่วนลดวันธรรมดา
                    case "WEEKDAY5":
                        // ตรวจสอบว่าได้รับส่วนลดอัตโนมัติแล้วหรือไม่
                        if (isWeekdayApplied)
                        {
                            // แจ้งเตือนว่าระบบลดให้อัตโนมัติแล้ว
                            result.ErrorMessage = "ระบบได้คำนวณส่วนลดวันธรรมดาสำหรับห้อง Deluxe+ 5% ให้อัตโนมัติแล้ว";
                        }
                        // ตรวจสอบว่าไม่ใช่วันธรรมดา
                        else if (!isWeekday)
                        {
                            // แจ้งเตือนเงื่อนไขวันธรรมดา
                            result.ErrorMessage = "คูปอง WEEKDAY5 ใช้ได้เฉพาะการเข้าพักในวันธรรมดา (จันทร์ - พฤหัสบดี) เท่านั้น";
                        }
                        // ตรวจสอบว่าไม่ใช่ห้อง Deluxe ขึ้นไป
                        else if (!isDeluxeOrAbove)
                        {
                            // แจ้งเตือนเงื่อนไขประเภทห้อง
                            result.ErrorMessage = "คูปอง WEEKDAY5 ใช้ได้เฉพาะห้องพักประเภท Deluxe ขึ้นไปเท่านั้น";
                        }
                        else
                        {
                            // คำนวณลด 5%
                            decimal disc = totalRoomPrice * 0.05m;
                            // บวกยอดเข้าส่วนลดคูปอง
                            couponDiscount += disc;
                            // บันทึกรายการ
                            result.AppliedPromotions.Add($"คูปอง WEEKDAY5 (-5%: ฿{disc:N0})");
                        }
                        break;

                    // คูปองต้อนรับสมาชิกใหม่ ลด 100 บาท
                    case "WELCOME100":
                        // คำนวณส่วนลด 100 บาท (โดยไม่เกินราคารวม)
                        decimal welcomeDisc = Math.Min(100m, totalRoomPrice);
                        // บวกยอดเข้าส่วนลดคูปอง
                        couponDiscount += welcomeDisc;
                        // บันทึกรายการ
                        result.AppliedPromotions.Add($"คูปอง WELCOME100 ต้อนรับสมาชิกใหม่ (-฿{welcomeDisc:N0})");
                        break;

                    // คูปองโปรโมชั่นพิเศษ Easy Stay ลด 5%
                    case "EASYSTAY":
                        // คำนวณลด 5%
                        decimal easyDisc = totalRoomPrice * 0.05m;
                        // บวกยอดเข้าส่วนลดคูปอง
                        couponDiscount += easyDisc;
                        // บันทึกรายการ
                        result.AppliedPromotions.Add($"คูปอง EASYSTAY ส่วนลดพิเศษ (-5%: ฿{easyDisc:N0})");
                        break;

                    // กรณีรหัสคูปองไม่ตรงกับเงื่อนไขใดๆ ในระบบ
                    default:
                        // บันทึกข้อความแจ้งเตือนว่าคูปองไม่ถูกต้อง
                        result.ErrorMessage = $"รหัสคูปอง '{cleanCode}' ไม่ถูกต้อง หรือหมดอายุแล้ว";
                        break;
                }
            }

            // คำนวณยอดส่วนลดรวมสุทธิ (จำกัดยอดส่วนลดรวมไม่ให้เกินราคาเต็มของห้องพัก)
            result.TotalDiscount = Math.Min(autoDiscount + couponDiscount, totalRoomPrice);
            // คืนค่าผลลัพธ์การคำนวณโปรโมชั่น
            return result;
        }
    }
}
