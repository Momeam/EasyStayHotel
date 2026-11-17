using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models
{
    public class HousekeepingTask
    {
        public string Id { get; set; } = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
        
        public int RoomId { get; set; }
        public string RoomNo { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = "Standard";
        public string Branch { get; set; } = "Bangkok";
        public int Floor { get; set; } = 1;
        
        // สถานะ: Pending (รอทำความสะอาด), InProgress (กำลังทำความสะอาด), Completed (ส่งงานแล้ว/พร้อมขาย)
        public string Status { get; set; } = "Pending";
        
        // ลำดับความสำคัญ: Normal (ปกติ), Urgent (ด่วน), VIP
        public string Priority { get; set; } = "Normal";
        
        // ที่มา: CheckOut (เช็คเอาท์แขก), FrontDesk (พนักงานต้อนรับแจ้ง)
        public string Source { get; set; } = "CheckOut";
        
        // ผู้แจ้งงาน (พนักงานต้อนรับ) และวันเวลาที่แจ้ง
        public string RequestedBy { get; set; } = "พนักงานต้อนรับ";
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        
        // ชื่อแขกล่าสุด (ถ้ามี)
        public string? GuestName { get; set; }
        
        // หมายเหตุ / คำขอพิเศษจากพนักงานต้อนรับ
        public string? Notes { get; set; }
        
        // ข้อมูลแม่บ้านผู้รับงาน / ส่งงาน
        public string? HousekeeperName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // รายงาน / หมายเหตุเมื่อส่งงาน
        public string? CompletionNotes { get; set; }
        
        // รายการตรวจสอบความสะอาด (Checklist)
        public List<string> Checklist { get; set; } = new List<string>();
    }
}
