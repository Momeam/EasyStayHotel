// นำเข้า System สำหรับชนิดข้อมูลพื้นฐานและฟังก์ชัน DateTime, Math
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List
using System.Collections.Generic;
// นำเข้า System.IO สำหรับอ่านและเขียนไฟล์บนดิสก์
using System.IO;
// นำเข้า System.Linq สำหรับฟังก์ชัน LINQ เช่น Where, Average, FirstOrDefault
using System.Linq;
// นำเข้า System.Text.Json สำหรับ Serialization ข้อมูลเป็น JSON
using System.Text.Json;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก ReviewService สำหรับบันทึกและจัดการรีวิวความประทับใจของลูกค้า
    public static class ReviewService
    {
        // พาธไดเรกทอรีเก็บข้อมูล (wwwroot/data)
        private static readonly string DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        // พาธไฟล์ JSON สำหรับบันทึกรีวิว (wwwroot/data/guest_reviews.json)
        private static readonly string FilePath = Path.Combine(DataDirectory, "guest_reviews.json");
        // อ็อบเจกต์ล็อกเธรดเพื่อความปลอดภัยในการเข้าถึงไฟล์
        private static readonly object FileLock = new object();

        // สแตติกคอนสตรัคเตอร์ ตรวจสอบและสร้างไฟล์ข้อมูลเริ่มต้นเมื่อคลาสถูกเรียกใช้ครั้งแรก
        static ReviewService()
        {
            EnsureDataFileExists();
        }

        // เมธอดส่วนตัวสำหรับตรวจสอบและสร้างข้อมูลรีวิวตัวอย่างเริ่มต้น
        private static void EnsureDataFileExists()
        {
            lock (FileLock)
            {
                // ตรวจสอบว่าไดเรกทอรี data มีอยู่แล้วหรือไม่
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }

                // ตรวจสอบว่าไฟล์ guest_reviews.json มีอยู่แล้วหรือไม่
                if (!File.Exists(FilePath))
                {
                    // กำหนดข้อมูลรีวิวตัวอย่าง 5 รายการจากทั้ง 3 สาขา
                    var initialReviews = new List<GuestReview>
                    {
                        new GuestReview
                        {
                            ReviewId = "REV-2026-001",
                            BookingId = 1,
                            BookingRef = "ES-20261015091234",
                            GuestName = "คุณธนภัทร วัฒนกุล",
                            GuestEmail = "thanapat@example.com",
                            Branch = "Bangkok",
                            RoomTypeName = "Deluxe Room (ดีลักซ์)",
                            Rating = 5,
                            CleanlinessRating = 5,
                            ServiceRating = 5,
                            LocationRating = 5,
                            Title = "ห้องพักสวยมาก เตียงนอนสบาย การเดินทางสะดวก",
                            Comment = "ประทับใจการบริการของพนักงานต้อนรับมากครับ ให้คำแนะนำดีเยี่ยม ห้องพักสะอาด กลิ่นหอม วิวเมืองกรุงเทพฯ สวยงาม แนะนำเลยครับ",
                            CreatedAt = DateTime.Now.AddDays(-12),
                            StaffReply = "ขอบพระคุณคุณธนภัทรเป็นอย่างยิ่งครับ ทาง Easy Stay สาขากรุงเทพฯ ยินดีที่ได้ให้บริการและหวังว่าจะได้ต้อนรับท่านอีกครั้งครับ",
                            StaffRepliedAt = DateTime.Now.AddDays(-11)
                        },
                        new GuestReview
                        {
                            ReviewId = "REV-2026-002",
                            BookingId = 2,
                            BookingRef = "ES-20261022143055",
                            GuestName = "คุณศิริพร บุญเสริม",
                            GuestEmail = "siriporn@example.com",
                            Branch = "Chonburi",
                            RoomTypeName = "Executive Suite (ห้องสวีท)",
                            Rating = 5,
                            CleanlinessRating = 5,
                            ServiceRating = 5,
                            LocationRating = 5,
                            Title = "วิวทะเลพัทยาสวยสะกดใจ อ่างจากุซซี่ดีมาก",
                            Comment = "พาครอบครัวมาพักผ่อนที่สาขาชลบุรี-พัทยา ห้องสวีทกว้างขวาง อ่างจากุซซี่แช่สบายมาก อาหารเช้าอร่อยหลากหลาย คุ้มค่าราคามากค่ะ",
                            CreatedAt = DateTime.Now.AddDays(-8),
                            StaffReply = "ขอบพระคุณคุณศิริพรและครอบครัวครับ ทางเราดีใจเป็นอย่างยิ่งที่ท่านได้รับความสุขในการเข้าพักครับ",
                            StaffRepliedAt = DateTime.Now.AddDays(-7)
                        },
                        new GuestReview
                        {
                            ReviewId = "REV-2026-003",
                            BookingId = 3,
                            BookingRef = "ES-20261101112040",
                            GuestName = "คุณกานต์พิชชา รัตนเวช",
                            GuestEmail = "karn@example.com",
                            Branch = "Phuket",
                            RoomTypeName = "Executive Suite (ห้องสวีท)",
                            Rating = 5,
                            CleanlinessRating = 5,
                            ServiceRating = 5,
                            LocationRating = 5,
                            Title = "บรรยากาศรีสอร์ทภูเก็ตเงียบสงบ พนักงานดูแลดุจคนในครอบครัว",
                            Comment = "ประทับใจระบบการเช็คอินที่รวดเร็ว ไม่ต้องรอนาน ห้องพักสะอาดสะอ้าน แม่บ้านทำความสะอาดเรียบร้อยมาก มีโอกาสจะกลับมาพักอีกแน่นอนค่ะ",
                            CreatedAt = DateTime.Now.AddDays(-5)
                        },
                        new GuestReview
                        {
                            ReviewId = "REV-2026-004",
                            BookingId = 4,
                            BookingRef = "ES-20261108164512",
                            GuestName = "คุณณัฐพงษ์ สิทธิโชค",
                            GuestEmail = "nattapong@example.com",
                            Branch = "Bangkok",
                            RoomTypeName = "Standard Room (สแตนดาร์ด)",
                            Rating = 4,
                            CleanlinessRating = 5,
                            ServiceRating = 4,
                            LocationRating = 5,
                            Title = "ห้องพักมาตรฐานแต่สิ่งอำนวยความสะดวกครบครัน",
                            Comment = "คุ้มราคามากครับสำหรับห้อง Standard แอร์เย็นฉ่ำ ไวไฟเร็วมาก เหมาะสำหรับคนมาทำงานหรือมาเที่ยวในเมือง ใกล้สถานีรถไฟฟ้า",
                            CreatedAt = DateTime.Now.AddDays(-3)
                        },
                        new GuestReview
                        {
                            ReviewId = "REV-2026-005",
                            BookingId = 5,
                            BookingRef = "ES-20261112085020",
                            GuestName = "คุณวิภาวรรณ เลิศปัญญา",
                            GuestEmail = "wipawan@example.com",
                            Branch = "Chonburi",
                            RoomTypeName = "Deluxe Room (ดีลักซ์)",
                            Rating = 5,
                            CleanlinessRating = 5,
                            ServiceRating = 5,
                            LocationRating = 4,
                            Title = "ระเบียงกว้าง รับลมทะเลชิลมาก",
                            Comment = "จองตอนมีโปรโมชั่น Early Bird ลด 15% คุ้มค่ามาก ห้องพักสวยเหมือนในรูป เตียงนุ่มหลับสบาย แนะนำห้องดีลักซ์เลยค่ะ",
                            CreatedAt = DateTime.Now.AddDays(-2)
                        }
                    };

                    // บันทึกรีวิวเริ่มต้นลงไฟล์
                    SaveReviewsToFile(initialReviews);
                }
            }
        }

        // เมธอดดึงรีวิวทั้งหมด
        public static List<GuestReview> GetAllReviews()
        {
            lock (FileLock)
            {
                try
                {
                    if (!File.Exists(FilePath)) return new List<GuestReview>();
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<List<GuestReview>>(json) ?? new List<GuestReview>();
                }
                catch
                {
                    return new List<GuestReview>();
                }
            }
        }

        // เมธอดดึงรีวิวแยกตามสาขา
        public static List<GuestReview> GetReviewsByBranch(string branch)
        {
            var all = GetAllReviews();
            if (string.IsNullOrEmpty(branch) || branch == "ALL") return all;
            return all.Where(r => r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // เมธอดดึงรีวิวแยกตามประเภทห้องพัก
        public static List<GuestReview> GetReviewsByRoomType(string roomTypeName)
        {
            var all = GetAllReviews();
            if (string.IsNullOrEmpty(roomTypeName)) return all;
            return all.Where(r => r.RoomTypeName.IndexOf(roomTypeName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  roomTypeName.IndexOf(r.RoomTypeName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        // เมธอดค้นหารีวิวตาม BookingId
        public static GuestReview? GetReviewByBookingId(int bookingId)
        {
            return GetAllReviews().FirstOrDefault(r => r.BookingId == bookingId);
        }

        // เมธอดคำนวณคะแนนเฉลี่ย (Average Rating)
        public static double GetAverageRating(string? roomTypeName = null)
        {
            var list = string.IsNullOrEmpty(roomTypeName) ? GetAllReviews() : GetReviewsByRoomType(roomTypeName);
            if (!list.Any()) return 4.9;
            return Math.Round(list.Average(r => r.Rating), 1);
        }

        // เมธอดนับจำนวนรีวิวทั้งหมด
        public static int GetReviewCount(string? roomTypeName = null)
        {
            var list = string.IsNullOrEmpty(roomTypeName) ? GetAllReviews() : GetReviewsByRoomType(roomTypeName);
            return list.Count;
        }

        // เมธอดเพิ่มรีวิวใหม่หรืออัปเดตรีวิวเดิม
        public static void AddReview(GuestReview review)
        {
            lock (FileLock)
            {
                var all = GetAllReviews();
                var existing = all.FirstOrDefault(r => r.BookingId == review.BookingId);
                if (existing != null)
                {
                    existing.Rating = review.Rating;
                    existing.CleanlinessRating = review.CleanlinessRating;
                    existing.ServiceRating = review.ServiceRating;
                    existing.LocationRating = review.LocationRating;
                    existing.Title = review.Title;
                    existing.Comment = review.Comment;
                    existing.CreatedAt = DateTime.Now;
                }
                else
                {
                    all.Insert(0, review);
                }
                SaveReviewsToFile(all);
            }
        }

        // เมธอดสำหรับพนักงานในการตอบกลับรีวิวของลูกค้า
        public static void ReplyReview(string reviewId, string reply)
        {
            lock (FileLock)
            {
                var all = GetAllReviews();
                var rev = all.FirstOrDefault(r => r.ReviewId == reviewId);
                if (rev != null)
                {
                    rev.StaffReply = reply;
                    rev.StaffRepliedAt = DateTime.Now;
                    SaveReviewsToFile(all);
                }
            }
        }

        // เมธอดบันทึกข้อมูลรีวิวลงไฟล์ JSON
        private static void SaveReviewsToFile(List<GuestReview> reviews)
        {
            string json = JsonSerializer.Serialize(reviews, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
