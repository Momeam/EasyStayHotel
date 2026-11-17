using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models
{
    /// <summary>
    /// โมเดล HousekeepingTask: จัดเก็บภารกิจทำความสะอาดห้องพักของแผนกแม่บ้าน
    /// หน้าที่: ติดตามสถานะงานทำความสะอาดตั้งแต่แขกเช็คเอาท์ จนกระทั่งแม่บ้านทำความสะอาดเสร็จและตรวจเช็คห้องพร้อมขาย
    /// การจัดเก็บข้อมูล: บันทึกลงในไฟล์ JSON (housekeeping_tasks.json) ผ่าน HousekeepingService
    /// </summary>
    public class HousekeepingTask
    {
        /// <summary>
        /// รหัสอ้างอิงใบสั่งงานทำความสะอาด เช่น "HK-20261117-0101"
        /// </summary>
        public string Id { get; set; } = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
        
        /// <summary>
        /// รหัสประจำตัวห้องพัก (เชื่อมโยงกับ RoomId ในฐานข้อมูล)
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// หมายเลขห้องพัก เช่น "101", "201"
        /// </summary>
        public string RoomNo { get; set; } = string.Empty;

        /// <summary>
        /// ชื่อประเภทห้องพัก เช่น "Deluxe King", "Standard Twin"
        /// </summary>
        public string RoomTypeName { get; set; } = "Standard";

        /// <summary>
        /// สาขาที่ห้องพักตั้งอยู่ เช่น "Bangkok", "Chonburi", "Phuket"
        /// </summary>
        public string Branch { get; set; } = "Bangkok";

        /// <summary>
        /// ชั้นที่ห้องพักตั้งอยู่ เช่น 1, 2, 3
        /// </summary>
        public int Floor { get; set; } = 1;
        
        /// <summary>
        /// สถานะขั้นตอนการทำงานของแม่บ้าน:
        /// - "Pending" = รอแม่บ้านมารับงานทำความสะอาด
        /// - "InProgress" = แม่บ้านกำลังปฏิบัติงานทำความสะอาดในห้อง
        /// - "Completed" = ทำความสะอาดเสร็จเรียบร้อยและห้องพร้อมเปิดขายใหม่
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// ลำดับความสำคัญของงาน: "Normal" (ปกติ), "Urgent" (ด่วนพิเศษ เช่น มีแขกใหม่รอเช็คอินต่อ), "VIP"
        /// </summary>
        public string Priority { get; set; } = "Normal";
        
        /// <summary>
        /// แหล่งที่มาของการสั่งงาน:
        /// - "CheckOut" = ระบบสร้างให้อัตโนมัติเมื่อแขกเช็คเอาท์ออกจากโรงแรม
        /// - "FrontDesk" = พนักงานต้อนรับแจ้งคำขอพิเศษเข้ามาโดยตรง
        /// </summary>
        public string Source { get; set; } = "CheckOut";
        
        /// <summary>
        /// ชื่อผู้ที่มอบหมายงาน (เช่น "พนักงานต้อนรับ 001")
        /// </summary>
        public string RequestedBy { get; set; } = "พนักงานต้อนรับ";

        /// <summary>
        /// วันเวลาที่สร้างใบสั่งงานทำความสะอาด
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// ชื่อแขกล่าสุดที่เพิ่งเช็คเอาท์ออกจากห้อง (ถ้ามี)
        /// </summary>
        public string? GuestName { get; set; }
        
        /// <summary>
        /// หมายเหตุหรือข้อความกำชับจากพนักงานต้อนรับ (เช่น มีแขกใหม่เข้า 14:00 น. หรือขอผ้าเช็ดตัวเพิ่ม)
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// ชื่อพนักงานแม่บ้านผู้รับหน้าที่ทำความสะอาดห้องนี้
        /// </summary>
        public string? HousekeeperName { get; set; }

        /// <summary>
        /// เวลาที่แม่บ้านเริ่มลงมือทำความสะอาด
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// เวลาที่แม่บ้านส่งมอบงานทำความสะอาดเสร็จสมบูรณ์
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// รายงานผลการทำงานหรือข้อสังเกตจากแม่บ้านหลังส่งมอบงาน (เช่น เปลี่ยนหลอดไฟแล้ว, อุปกรณ์ครบ)
        /// </summary>
        public string? CompletionNotes { get; set; }
        
        /// <summary>
        /// รายการตรวจสอบความสะอาด (Cleaning Checklist) เช่น เปลี่ยนผ้าปู, ทำความสะอาดห้องน้ำ, เติม Amenities
        /// </summary>
        public List<string> Checklist { get; set; } = new List<string>();
    }
}
