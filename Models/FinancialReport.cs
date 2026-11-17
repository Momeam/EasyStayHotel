using System;

namespace EasyStayHotel.Models
{
    /// <summary>
    /// โมเดล FinancialReport: จัดเก็บรายงานสรุปสถานะทางการเงินและรายได้ของโรงแรม
    /// หน้าที่: ใช้โดยฝ่ายบัญชี (Accounting) ในการจัดทำรายงานสรุปยอดรับชำระเงิน นำส่งให้ผู้บริหาร (Manager/Admin) ตรวจสอบและอนุมัติ
    /// การจัดเก็บข้อมูล: บันทึกลงในไฟล์ JSON (financial_reports.json) ผ่าน FinancialReportService
    /// </summary>
    public class FinancialReport
    {
        /// <summary>
        /// รหัสรายงานทางการเงิน เช่น "FR-20261117180000"
        /// </summary>
        public string ReportId { get; set; } = "FR-" + DateTime.Now.ToString("yyyyMMddHHmmss");

        /// <summary>
        /// วันเวลาที่จัดทำรายงาน
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// ชื่อหรือแผนกผู้จัดทำรายงาน เช่น "ฝ่ายบัญชีและการเงิน"
        /// </summary>
        public string CreatedBy { get; set; } = "ฝ่ายบัญชี";

        /// <summary>
        /// รอบระยะเวลาของรายงาน: "Daily" (ประจำวัน), "Weekly" (ประจำสัปดาห์), "Monthly" (ประจำเดือน)
        /// </summary>
        public string PeriodType { get; set; } = "Daily";

        /// <summary>
        /// สาขาที่จัดทำรายงาน: "ALL" (รวมทุกสาขา), "Bangkok", "Chonburi", "Phuket"
        /// </summary>
        public string Branch { get; set; } = "ALL";

        /// <summary>
        /// ป้ายกำกับรอบระยะเวลา เช่น "ประจำวันที่ 17/11/2026"
        /// </summary>
        public string PeriodLabel { get; set; } = string.Empty;

        /// <summary>
        /// ยอดรายได้รวมทั้งหมดในรอบรายงาน (หน่วย: บาท)
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// ยอดรับชำระด้วยเงินสด (Cash)
        /// </summary>
        public decimal CashAmount { get; set; }

        /// <summary>
        /// ยอดรับชำระด้วยการโอนเงิน/PromptPay (Bank Transfer)
        /// </summary>
        public decimal TransferAmount { get; set; }

        /// <summary>
        /// ยอดเงินที่ทำเรื่องขอคืนหรือคืนเงินให้ลูกค้า (Refunds)
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// จำนวนรายการจองทั้งหมดที่เกิดขึ้นในรอบเวลานี้
        /// </summary>
        public int TotalBookings { get; set; }
        
        /// <summary>
        /// สัดส่วนรายได้ของสาขากรุงเทพฯ (หน่วย: บาท)
        /// </summary>
        public decimal BangkokRevenue { get; set; }

        /// <summary>
        /// สัดส่วนรายได้ของสาขาชลบุรี-พัทยา (หน่วย: บาท)
        /// </summary>
        public decimal ChonburiRevenue { get; set; }

        /// <summary>
        /// สัดส่วนรายได้ของสาขาภูเก็ต (หน่วย: บาท)
        /// </summary>
        public decimal PhuketRevenue { get; set; }

        /// <summary>
        /// บันทึกข้อความวิเคราะห์ทางการเงินและข้อสังเกตจากฝ่ายบัญชีส่งถึงผู้บริหาร
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// สถานะของรายงาน:
        /// - "Pending" = รอผู้จัดการตรวจรับทราบ
        /// - "Acknowledged" = ผู้จัดการรับทราบยอดเรียบร้อย
        /// - "Approved" = ผู้จัดการอนุมัติปิดยอดบัญชี
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// ชื่อผู้จัดการที่ทำการตรวจรับทราบรายงาน
        /// </summary>
        public string? AcknowledgedBy { get; set; }

        /// <summary>
        /// วันเวลาที่ผู้จัดการตรวจรับทราบ
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// ความเห็นหรือข้อสั่งการเพิ่มเติมจากผู้จัดการ
        /// </summary>
        public string? ManagerComment { get; set; }
    }
}
