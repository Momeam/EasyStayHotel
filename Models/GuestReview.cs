using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models
{
    /// <summary>
    /// โมเดล GuestReview: จัดเก็บข้อมูลการรีวิวและให้คะแนนความพึงพอใจของลูกค้า (Guest Review & Rating System)
    /// หน้าที่: บันทึกความคิดเห็นและคะแนนดาวจากผู้เข้าพักจริง (Verified Guest) พร้อมข้อความตอบกลับจากโรงแรม
    /// การจัดเก็บข้อมูล: บันทึกลงในไฟล์ JSON (guest_reviews.json) ผ่าน ReviewService
    /// </summary>
    public class GuestReview
    {
        /// <summary>
        /// รหัสประจำตัวของรีวิว (GUID string แบบ 32 ตัวอักษร)
        /// </summary>
        public string ReviewId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// รหัสการจองห้องพักที่เกี่ยวข้อง (ต้องเป็นการจองที่เช็คเอาท์แล้วเท่านั้น)
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// รหัสอ้างอิงการจอง เช่น "ES-20261015091234"
        /// </summary>
        public string BookingRef { get; set; } = string.Empty;

        /// <summary>
        /// ชื่อ-นามสกุลของลูกค้าผู้เขียนรีวิว
        /// </summary>
        public string GuestName { get; set; } = string.Empty;

        /// <summary>
        /// อีเมลของลูกค้า
        /// </summary>
        public string? GuestEmail { get; set; }

        /// <summary>
        /// สาขาที่เข้าพัก เช่น "Bangkok", "Chonburi", "Phuket"
        /// </summary>
        public string Branch { get; set; } = "Bangkok";

        /// <summary>
        /// ชื่อประเภทห้องพักที่เข้าพัก เช่น "Deluxe Room", "Executive Suite"
        /// </summary>
        public string RoomTypeName { get; set; } = "Standard";
        
        /// <summary>
        /// คะแนนความพึงพอใจโดยรวม (1 ถึง 5 ดาว)
        /// </summary>
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        /// <summary>
        /// คะแนนด้านความสะอาดของห้องพัก (1 ถึง 5)
        /// </summary>
        public int CleanlinessRating { get; set; } = 5;

        /// <summary>
        /// คะแนนด้านการให้บริการของพนักงาน (1 ถึง 5)
        /// </summary>
        public int ServiceRating { get; set; } = 5;

        /// <summary>
        /// คะแนนด้านทำเลที่ตั้งและความสะดวกในการเดินทาง (1 ถึง 5)
        /// </summary>
        public int LocationRating { get; set; } = 5;
        
        /// <summary>
        /// หัวข้อสรุปความประทับใจของรีวิว
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// ข้อความรายละเอียดความคิดเห็นและข้อเสนอแนะจากลูกค้า
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// วันเวลาที่ลูกค้าส่งรีวิว
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// เครื่องหมายยืนยันว่าเข้าพักจริง (Verified Stay) เพื่อความโปร่งใสและน่าเชื่อถือ
        /// </summary>
        public bool IsVerifiedStay { get; set; } = true;
        
        /// <summary>
        /// ข้อความตอบกลับจากผู้บริหารหรือพนักงานของโรงแรม
        /// </summary>
        public string? StaffReply { get; set; }

        /// <summary>
        /// วันเวลาที่โรงแรมส่งข้อความตอบกลับ
        /// </summary>
        public DateTime? StaffRepliedAt { get; set; }
    }
}
