// นำเข้า System สำหรับชนิดข้อมูลพื้นฐานและฟังก์ชัน DateTime
using System;
// นำเข้า System.Globalization สำหรับจัดการวัฒนธรรมและปฏิทินสากล
using System.Globalization;

// กำหนดเนมสเปซ EasyStayHotel.Services สำหรับจัดกลุ่มคลาสเซอร์วิส
namespace EasyStayHotel.Services
{
    // คลาส DateHelper เป็นสแตติกคลาสสำหรับช่วยแปลงและปรับเทียบวันที่ระหว่าง พ.ศ. และ ค.ศ.
    public static class DateHelper
    {
        // กำหนดอ็อบเจกต์ CultureInfo.InvariantCulture สำหรับการแปลงข้อความเป็นวันที่แบบมาตรฐานสากล
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // เมธอด NormalizeToGregorian สำหรับแปลง DateTime ให้เป็นปี ค.ศ. ที่ถูกต้อง
        public static DateTime NormalizeToGregorian(DateTime dt)
        {
            // ตรวจสอบว่าวันที่เป็นค่าเริ่มต้น (default) หรือค่าน้อยสุด/มากสุดหรือไม่ ถ้าใช่ให้คืนค่าเดิมทันที
            if (dt == default || dt == DateTime.MinValue || dt == DateTime.MaxValue)
            {
                // คืนค่า dt เดิมโดยไม่ต้องปรับเทียบ
                return dt;
            }

            // ดึงปีปัจจุบันของอ็อบเจกต์ dt ออกมาเป็นตัวเลขจำนวนเต็ม
            int year = dt.Year;
            // ตรวจสอบกรณีปี พ.ศ. ถูกแปลงซ้อน 2 ครั้งจนปีมากกว่าหรือเท่ากับ 3000 (เช่น ค.ศ. 2026 -> 2569 -> 3112)
            if (year >= 3000)
            {
                // ลบปีออก 1086 ปี (543 x 2) เพื่อคืนค่ากลับมาเป็นปี ค.ศ. สากล
                return dt.AddYears(-1086);
            }
            // ตรวจสอบกรณีปีเป็น พ.ศ. (ปีมากกว่าหรือเท่ากับ 2400 เช่น 2569)
            if (year >= 2400)
            {
                // ลบปีออก 543 ปี เพื่อแปลงจากปี พ.ศ. ให้เป็นปี ค.ศ.
                return dt.AddYears(-543);
            }
            // หากปีเป็น ค.ศ. อยู่แล้ว (น้อยกว่า 2400) ให้คืนค่า dt นั้นทันที
            return dt;
        }

        // เมธอด NormalizeToGregorian แบบ Overload สำหรับแปลง DateOnly ให้เป็นปี ค.ศ.
        public static DateOnly NormalizeToGregorian(DateOnly d)
        {
            // ตรวจสอบว่าค่า DateOnly เป็นค่าเริ่มต้นหรือค่าน้อยสุด/มากสุดหรือไม่
            if (d == default || d == DateOnly.MinValue || d == DateOnly.MaxValue)
            {
                // คืนค่า d เดิมทันที
                return d;
            }

            // ดึงตัวเลขปีของ DateOnly ออกมา
            int year = d.Year;
            // ตรวจสอบกรณีปีเกิน 3000 จากการแปลง พ.ศ. ซ้อนสองครั้ง
            if (year >= 3000)
            {
                // ลบปีออก 1086 ปี เพื่อคืนค่าเป็นปี ค.ศ.
                return d.AddYears(-1086);
            }
            // ตรวจสอบกรณีปีเป็น พ.ศ. (ตั้งแต่ปี 2400 ขึ้นไป)
            if (year >= 2400)
            {
                // ลบปีออก 543 ปี เพื่อปรับให้เป็นปี ค.ศ.
                return d.AddYears(-543);
            }
            // หากเป็นปี ค.ศ. สากลอยู่แล้ว ให้คืนค่าเดิม
            return d;
        }

        // เมธอด ParseGregorian สำหรับแปลงข้อความวันที่ (string) ให้ออกมาเป็น DateTime ปี ค.ศ.
        public static DateTime ParseGregorian(string? dateStr, DateTime fallback)
        {
            // ตรวจสอบว่าข้อความวันที่ว่างเปล่า หรือเป็นช่องว่างหรือไม่
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                // หากไม่มีข้อมูล ให้คืนค่า fallback ที่ผ่านการปรับเป็นปี ค.ศ. แล้ว
                return NormalizeToGregorian(fallback);
            }

            // ตัดช่องว่างส่วนเกินด้านหน้าและด้านหลังข้อความวันที่
            dateStr = dateStr.Trim();

            // ลองแปลงข้อความเป็น DateTime โดยใช้ InvariantCulture (รูปแบบสากล เช่น yyyy-MM-dd)
            if (DateTime.TryParse(dateStr, Invariant, DateTimeStyles.None, out DateTime parsed))
            {
                // หากแปลงสำเร็จ ให้นำผลลัพธ์มาปรับเทียบปี ค.ศ. แล้วคืนค่า
                return NormalizeToGregorian(parsed);
            }

            // หากไม่สำเร็จ ให้ลองแปลงตาม Culture ปัจจุบันของระบบ
            if (DateTime.TryParse(dateStr, out parsed))
            {
                // หากแปลงสำเร็จ ปรับเทียบปี ค.ศ. แล้วคืนค่า
                return NormalizeToGregorian(parsed);
            }

            // หากไม่สามารถแปลงข้อความเป็นวันที่ได้เลย ให้คืนค่า fallback สำรอง
            return NormalizeToGregorian(fallback);
        }

        // เมธอด ToIsoDate สำหรับแปลง DateTime ให้อยู่ในรูปแบบ ISO string (yyyy-MM-dd)
        public static string ToIsoDate(DateTime dt)
        {
            // ปรับเทียบปีให้เป็น ค.ศ. ก่อน
            dt = NormalizeToGregorian(dt);
            // แปลงเป็นข้อความในรูปแบบ "ปีค.ศ.-เดือน-วัน" ตาม InvariantCulture
            return dt.ToString("yyyy-MM-dd", Invariant);
        }

        // เมธอด ToIsoDate แบบ Overload สำหรับแปลง DateOnly เป็น ISO string (yyyy-MM-dd)
        public static string ToIsoDate(DateOnly d)
        {
            // ปรับเทียบปีให้เป็น ค.ศ. ก่อน
            d = NormalizeToGregorian(d);
            // แปลงเป็นข้อความรูปแบบ "yyyy-MM-dd"
            return d.ToString("yyyy-MM-dd", Invariant);
        }

        // เมธอด ToThaiDisplayDate สำหรับแปลง DateTime เป็นข้อความวันที่ภาษาไทยแบบย่อ (เช่น 19 พ.ย. 2569)
        public static string ToThaiDisplayDate(DateTime dt)
        {
            // ปรับเทียบปีให้เป็น ค.ศ. สากลก่อนเพื่อความแม่นยำ
            dt = NormalizeToGregorian(dt);
            // อาเรย์เก็บชื่อย่อเดือนภาษาไทย ทั้ง 12 เดือน (ดัชนี 0 เป็นค่าว่างเพื่อให้ตรงกับเลขเดือน 1-12)
            string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            // จำกัดค่าตัวเลขเดือนให้อยู่ระหว่าง 1 ถึง 12 ป้องกันข้อผิดพลาด IndexOutOfRange
            int month = Math.Clamp(dt.Month, 1, 12);
            // แปลงปี ค.ศ. ให้เป็นปี พ.ศ. โดยการบวก 543
            int thaiYear = dt.Year + 543;
            // ประกอบข้อความ วันที่ ชื่อย่อเดือน และปี พ.ศ. แล้วคืนค่า
            return $"{dt.Day} {thaiMonths[month]} {thaiYear}";
        }

        // เมธอด ToThaiFullDate สำหรับแปลง DateTime เป็นข้อความวันที่ภาษาไทยแบบเต็ม (เช่น 19 พฤศจิกายน 2569)
        public static string ToThaiFullDate(DateTime dt)
        {
            // ปรับเทียบปีให้เป็น ค.ศ. สากลก่อน
            dt = NormalizeToGregorian(dt);
            // อาเรย์เก็บชื่อเต็มเดือนภาษาไทย ทั้ง 12 เดือน
            string[] thaiMonths = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            // จำกัดค่าตัวเลขเดือนให้อยู่ระหว่าง 1 ถึง 12
            int month = Math.Clamp(dt.Month, 1, 12);
            // แปลงปี ค.ศ. ให้เป็นปี พ.ศ. (+543)
            int thaiYear = dt.Year + 543;
            // ประกอบข้อความ วันที่ ชื่อเต็มเดือน และปี พ.ศ. แล้วคืนค่า
            return $"{dt.Day} {thaiMonths[month]} {thaiYear}";
        }
    }
}
