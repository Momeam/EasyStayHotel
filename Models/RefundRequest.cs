using System;

namespace EasyStayHotel.Models
{
    /// <summary>
    /// โมเดล RefundRequest: จัดเก็บคำร้องขอยกเลิกการจองและขอเงินคืนของลูกค้า (Refund Management System)
    /// หน้าที่: ลูกค้าสามารถยื่นขอคืนเงินได้ภายใน 7 วันนับจากวันที่จอง (ตามนโยบายโรงแรม) โดยฝ่ายบัญชีจะเป็นผู้ตรวจสอบและโอนเงินคืนภายใน 3 วันทำการ
    /// การจัดเก็บข้อมูล: บันทึกลงในไฟล์ JSON (refund_requests.json) ผ่าน RefundService
    /// </summary>
    public class RefundRequest
    {
        /// <summary>
        /// รหัสอ้างอิงคำขอคืนเงิน (GUID 32 ตัวอักษร)
        /// </summary>
        public string RefundId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// รหัสการจองในฐานข้อมูล (BookingId)
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// รหัสอ้างอิงการจอง เช่น "ES-20261117120000"
        /// </summary>
        public string BookingRef { get; set; } = string.Empty;

        /// <summary>
        /// ชื่อ-นามสกุลของลูกค้าผู้ขอยกเลิกและรับเงินคืน
        /// </summary>
        public string GuestName { get; set; } = string.Empty;

        /// <summary>
        /// เบอร์โทรศัพท์สำหรับติดต่อกลับ
        /// </summary>
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// อีเมลสำหรับแจ้งผลการพิจารณาคืนเงิน
        /// </summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// สาขาของโรงแรมที่จองไว้ เช่น "Bangkok", "Chonburi", "Phuket"
        /// </summary>
        public string Branch { get; set; } = "Bangkok";

        /// <summary>
        /// หมายเลขห้องพักที่จอง (ถ้ามี)
        /// </summary>
        public string? RoomNo { get; set; }

        /// <summary>
        /// ประเภทห้องพักที่จอง
        /// </summary>
        public string? RoomTypeName { get; set; }

        /// <summary>
        /// จำนวนเงินที่ขอยกเลิกและขอคืน (หน่วย: บาท)
        /// </summary>
        public decimal Amount { get; set; }

        // ==========================================
        // ข้อมูลบัญชีธนาคารสำหรับโอนเงินคืน (Bank Account Details)
        // ==========================================

        /// <summary>
        /// ชื่อธนาคาร เช่น "ธนาคารกสิกรไทย", "ธนาคารไทยพาณิชย์", "ธนาคารกรุงเทพ"
        /// </summary>
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// เลขที่บัญชีธนาคารสำหรับรับเงินคืน
        /// </summary>
        public string BankAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// ชื่อบัญชีธนาคาร (ต้องตรงกับชื่อผู้จองหรือชื่อที่ระบุในคำขอ)
        /// </summary>
        public string BankAccountName { get; set; } = string.Empty;

        /// <summary>
        /// เหตุผลในการขอยกเลิกและขอคืนเงิน
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        // ==========================================
        // การกำหนดระยะเวลาตามนโยบายโรงแรม (Policy Timelines)
        // ==========================================

        /// <summary>
        /// วันที่ทำรายการจอง (ต้องขอยื่นเรื่องภายใน 7 วันหลังจากวันที่จอง)
        /// </summary>
        public DateTime BookingDate { get; set; } = DateTime.Now;

        /// <summary>
        /// วันเวลาที่ลูกค้ากดยื่นคำร้องขอคืนเงิน
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// กำหนดเวลาที่โรงแรมต้องดำเนินการคืนเงินให้เสร็จสิ้น (ภายใน 3 วันทำการ)
        /// </summary>
        public DateTime RefundDeadline { get; set; } = DateTime.Now.AddDays(3);

        // ==========================================
        // สถานะการพิจารณาโดยฝ่ายบัญชี (Processing by Accounting)
        // ==========================================

        /// <summary>
        /// สถานะคำร้อง:
        /// - "Pending" = รอฝ่ายบัญชีตรวจสอบข้อมูลและหลักฐาน
        /// - "Approved" = ฝ่ายบัญชีอนุมัติและโอนเงินคืนเรียบร้อยแล้ว
        /// - "Rejected" = ปฏิเสธคำร้อง (เช่น พ้นกำหนด 7 วัน หรือเข้าพักแล้ว)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// ชื่อพนักงานบัญชีผู้ตรวจสอบและดำเนินการ
        /// </summary>
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// วันเวลาที่ดำเนินการเสร็จสิ้น
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// บันทึกข้อความหรือเหตุผลประกอบการพิจารณาของฝ่ายบัญชี
        /// </summary>
        public string? AdminNote { get; set; }

        /// <summary>
        /// URL หรือ Path สลิปหลักฐานการโอนเงินคืนให้ลูกค้า
        /// </summary>
        public string? RefundSlipUrl { get; set; }
    }
}
