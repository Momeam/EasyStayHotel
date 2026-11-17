using System;

namespace EasyStayHotel.Models
{
    /// <summary>
    /// โมเดล AccountingFeedback: จัดเก็บข้อความสื่อสารและการส่งยอดเงินระหว่างพนักงานต้อนรับหน้าร้านกับฝ่ายบัญชี
    /// หน้าที่: พนักงานต้อนรับใช้แจ้งยอดปิดกะ (Shift Handover), แจ้งสลิปที่มีปัญหา, หรือข้อสังเกตเกี่ยวกับธุรกรรมหน้าร้าน
    /// การจัดเก็บข้อมูล: บันทึกลงในไฟล์ JSON (accounting_feedbacks.json) ผ่าน AccountingFeedbackService
    /// </summary>
    public class AccountingFeedback
    {
        /// <summary>
        /// รหัสประจำตัวข้อความ (GUID 32 ตัวอักษร)
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// วันเวลาที่พนักงานส่งข้อความ
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// ชื่อพนักงานผู้ส่งข้อความ (เช่น "พนักงานต้อนรับ 001")
        /// </summary>
        public string StaffName { get; set; } = string.Empty;

        /// <summary>
        /// สาขาที่ปฏิบัติหน้าที่ เช่น "Bangkok", "Chonburi", "Phuket"
        /// </summary>
        public string Branch { get; set; } = "Bangkok";

        /// <summary>
        /// รอบกะการทำงาน เช่น "รอบเช้า (Morning Shift)", "รอบบ่าย (Afternoon Shift)", "รอบค่ำ/ปิดกะ (Night Shift)"
        /// </summary>
        public string Shift { get; set; } = "รอบค่ำ/ปิดกะ (Night Shift)";

        /// <summary>
        /// ยอดรวมเงินสดที่รับเข้าในรอบกะ (หน่วย: บาท)
        /// </summary>
        public decimal CashAmount { get; set; }

        /// <summary>
        /// ยอดรวมเงินโอน/PromptPay ในรอบกะ (หน่วย: บาท)
        /// </summary>
        public decimal TransferAmount { get; set; }

        /// <summary>
        /// จำนวนแขกที่เช็คอินในรอบกะ
        /// </summary>
        public int TotalCheckIns { get; set; }

        /// <summary>
        /// จำนวนแขกที่เช็คเอาท์ในรอบกะ
        /// </summary>
        public int TotalCheckOuts { get; set; }

        /// <summary>
        /// หมวดหมู่ของข้อความ: "รายงานรอบกะ/ประจำวัน", "แจ้งเรื่องคืนเงิน", "ข้อผิดพลาดทางการเงิน", "ข้อเสนอแนะทั่วไป"
        /// </summary>
        public string Category { get; set; } = "รายงานรอบกะ/ประจำวัน";

        /// <summary>
        /// รายละเอียดข้อความหรือหมายเหตุที่แจ้งฝ่ายบัญชี
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// สถานะว่าฝ่ายบัญชีกดรับทราบข้อความแล้วหรือไม่ (true/false)
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// ชื่อเจ้าหน้าที่บัญชีผู้กดรับทราบ
        /// </summary>
        public string? AcknowledgedBy { get; set; }

        /// <summary>
        /// วันเวลาที่ฝ่ายบัญชีกดรับทราบ
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }
    }
}
