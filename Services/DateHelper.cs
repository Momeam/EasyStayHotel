using System;
using System.Globalization;

namespace EasyStayHotel.Services
{
    public static class DateHelper
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static DateTime NormalizeToGregorian(DateTime dt)
        {
            if (dt == default || dt == DateTime.MinValue || dt == DateTime.MaxValue)
            {
                return dt;
            }

            int year = dt.Year;
            if (year >= 3000)
            {
                return dt.AddYears(-1086);
            }
            if (year >= 2400)
            {
                return dt.AddYears(-543);
            }
            return dt;
        }

        public static DateOnly NormalizeToGregorian(DateOnly d)
        {
            if (d == default || d == DateOnly.MinValue || d == DateOnly.MaxValue)
            {
                return d;
            }

            int year = d.Year;
            if (year >= 3000)
            {
                return d.AddYears(-1086);
            }
            if (year >= 2400)
            {
                return d.AddYears(-543);
            }
            return d;
        }

        public static DateTime ParseGregorian(string? dateStr, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                return NormalizeToGregorian(fallback);
            }

            dateStr = dateStr.Trim();

            if (DateTime.TryParse(dateStr, Invariant, DateTimeStyles.None, out DateTime parsed))
            {
                return NormalizeToGregorian(parsed);
            }

            if (DateTime.TryParse(dateStr, out parsed))
            {
                return NormalizeToGregorian(parsed);
            }

            return NormalizeToGregorian(fallback);
        }

        public static string ToIsoDate(DateTime dt)
        {
            dt = NormalizeToGregorian(dt);
            return dt.ToString("yyyy-MM-dd", Invariant);
        }

        public static string ToIsoDate(DateOnly d)
        {
            d = NormalizeToGregorian(d);
            return d.ToString("yyyy-MM-dd", Invariant);
        }

        public static string ToThaiDisplayDate(DateTime dt)
        {
            dt = NormalizeToGregorian(dt);
            string[] thaiMonths = { "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            int month = Math.Clamp(dt.Month, 1, 12);
            int thaiYear = dt.Year + 543;
            return $"{dt.Day} {thaiMonths[month]} {thaiYear}";
        }

        public static string ToThaiFullDate(DateTime dt)
        {
            dt = NormalizeToGregorian(dt);
            string[] thaiMonths = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            int month = Math.Clamp(dt.Month, 1, 12);
            int thaiYear = dt.Year + 543;
            return $"{dt.Day} {thaiMonths[month]} {thaiYear}";
        }
    }
}
