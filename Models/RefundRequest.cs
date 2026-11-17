using System;

namespace EasyStayHotel.Models
{
    public class RefundRequest
    {
        public string RefundId { get; set; } = Guid.NewGuid().ToString("N");
        public int BookingId { get; set; }
        public string BookingRef { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string Branch { get; set; } = "Bangkok";
        public string? RoomNo { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal Amount { get; set; } // ยอดเงินที่ขอคืน

        // ข้อมูลธนาคารสำหรับโอนเงินคืน
        public string BankName { get; set; } = string.Empty; // เช่น ธนาคารกสิกรไทย, ธนาคารไทยพาณิชย์
        public string BankAccountNumber { get; set; } = string.Empty; // เลขบัญชี
        public string BankAccountName { get; set; } = string.Empty; // ชื่อเจ้าของบัญชี
        public string Reason { get; set; } = string.Empty; // เหตุผลในการขอคืนเงิน

        // วันเวลาและเงื่อนไข
        public DateTime BookingDate { get; set; } = DateTime.Now; // วันที่ทำรายการจอง (ต้องขอคืนภายใน 7 วัน)
        public DateTime RequestedAt { get; set; } = DateTime.Now; // วันเวลาที่กดยื่นขอคืนเงิน
        public DateTime RefundDeadline { get; set; } = DateTime.Now.AddDays(3); // กำหนดคืนเงินภายใน 3 วันทำการ

        // สถานะการพิจารณาโดยฝ่ายบัญชี
        public string Status { get; set; } = "Pending"; // "Pending" (รอฝ่ายบัญชีตรวจสอบ), "Approved" (โอนเงินคืนแล้ว), "Rejected" (ปฏิเสธ)
        public string? ProcessedBy { get; set; } // พนักงานบัญชีที่ตรวจสอบ/อนุมัติ
        public DateTime? ProcessedAt { get; set; }
        public string? AdminNote { get; set; } // หมายเหตุหรือเหตุผลประกอบ
        public string? RefundSlipUrl { get; set; } // หลักฐานสลิปการโอนเงินคืน (ถ้ามี)
    }
}
