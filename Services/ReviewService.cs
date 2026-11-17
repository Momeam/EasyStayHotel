using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// เซอร์วิส ReviewService: จัดการระบบรีวิวและให้คะแนนความพึงพอใจของผู้เข้าพัก (Guest Reviews & Ratings)
    /// รูปแบบการจัดเก็บ: จัดเก็บข้อมูลในไฟล์ JSON (wwwroot/data/guest_reviews.json)
    /// การออกแบบความปลอดภัย (Thread Safety): ใช้ lock (FileLock) เพื่อป้องกันปัญหา Race Condition เมื่อมีคำขอเขียนไฟล์พร้อมกัน
    /// </summary>
    public static class ReviewService
    {
        private static readonly string DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string FilePath = Path.Combine(DataDirectory, "guest_reviews.json");
        private static readonly object FileLock = new object();

        /// <summary>
        /// Static Constructor: ทำงานอัตโนมัติ 1 ครั้งเมื่อคลาสถูกเรียกใช้ เพื่อตรวจสอบและเตรียมไฟล์ข้อมูลรีวิวเริ่มต้น
        /// </summary>
        static ReviewService()
        {
            EnsureDataFileExists();
        }

        /// <summary>
        /// ตรวจสอบการมีอยู่ของไดเรกทอรีและไฟล์ข้อมูล หากยังไม่มี จะสร้างโฟลเดอร์และใส่ข้อมูลตัวอย่าง (Seed Data)
        /// </summary>
        private static void EnsureDataFileExists()
        {
            lock (FileLock)
            {
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }

                if (!File.Exists(FilePath))
                {
                    // ข้อมูลรีวิวตัวอย่างที่สมจริงจากผู้เข้าพักจริง (Seeded Reviews)
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

                    SaveReviewsToFile(initialReviews);
                }
            }
        }

        /// <summary>
        /// ดึงรายการรีวิวทั้งหมดที่มีในระบบ เรียงจากล่าสุดไปหาเก่าสุด
        /// </summary>
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

        /// <summary>
        /// ดึงรายการรีวิวแยกตามสาขาของโรงแรม (Bangkok, Chonburi, Phuket หรือ ALL)
        /// </summary>
        public static List<GuestReview> GetReviewsByBranch(string branch)
        {
            var all = GetAllReviews();
            if (string.IsNullOrEmpty(branch) || branch == "ALL") return all;
            return all.Where(r => r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// ดึงรายการรีวิวตามประเภทห้องพัก เพื่อแสดงในหน้ารายละเอียดห้องพักนั้นๆ
        /// </summary>
        public static List<GuestReview> GetReviewsByRoomType(string roomTypeName)
        {
            var all = GetAllReviews();
            if (string.IsNullOrEmpty(roomTypeName)) return all;
            return all.Where(r => r.RoomTypeName.IndexOf(roomTypeName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  roomTypeName.IndexOf(r.RoomTypeName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        /// <summary>
        /// ค้นหารีวิวจากการจอง (BookingId) เพื่อตรวจสอบว่าลูกค้าเคยรีวิวการจองนี้แล้วหรือยัง
        /// </summary>
        public static GuestReview? GetReviewByBookingId(int bookingId)
        {
            return GetAllReviews().FirstOrDefault(r => r.BookingId == bookingId);
        }

        /// <summary>
        /// คำนวณคะแนนเฉลี่ยดาว (Average Rating) ทศนิยม 1 ตำแหน่ง
        /// </summary>
        public static double GetAverageRating(string? roomTypeName = null)
        {
            var list = string.IsNullOrEmpty(roomTypeName) ? GetAllReviews() : GetReviewsByRoomType(roomTypeName);
            if (!list.Any()) return 4.9; // คะแนนมาตรฐานเริ่มต้น
            return Math.Round(list.Average(r => r.Rating), 1);
        }

        /// <summary>
        /// นับจำนวนรีวิวทั้งหมด หรือนับตามประเภทห้องพัก
        /// </summary>
        public static int GetReviewCount(string? roomTypeName = null)
        {
            var list = string.IsNullOrEmpty(roomTypeName) ? GetAllReviews() : GetReviewsByRoomType(roomTypeName);
            return list.Count;
        }

        /// <summary>
        /// บันทึกรีวิวใหม่เข้าสู่ระบบ หรืออัปเดตรีวิวเดิมหากเคยรีวิวการจองนั้นไปแล้ว
        /// </summary>
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

        /// <summary>
        /// บันทึกข้อความตอบกลับจากโรงแรม (Staff/Manager Reply) ไปยังรีวิวของลูกค้า
        /// </summary>
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

        /// <summary>
        /// ฟังก์ชันภายในสำหรับเขียนข้อมูลรายการรีวิวลงไฟล์ JSON
        /// </summary>
        private static void SaveReviewsToFile(List<GuestReview> reviews)
        {
            string json = JsonSerializer.Serialize(reviews, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
