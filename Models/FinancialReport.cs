using System;

namespace EasyStayHotel.Models
{
    public class FinancialReport
    {
        public string ReportId { get; set; } = "FR-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "ฝ่ายบัญชี";
        public string PeriodType { get; set; } = "Daily"; // "Daily", "Weekly", "Monthly"
        public string Branch { get; set; } = "ALL"; // "ALL", "Bangkok", "Chonburi", "Phuket"
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal CashAmount { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public int TotalBookings { get; set; }
        
        // สัดส่วนรายได้แยกตามสาขา
        public decimal BangkokRevenue { get; set; }
        public decimal ChonburiRevenue { get; set; }
        public decimal PhuketRevenue { get; set; }

        public string? Notes { get; set; } // บันทึก/ข้อความวิเคราะห์จากฝ่ายบัญชีถึงผู้จัดการ
        public string Status { get; set; } = "Pending"; // "Pending", "Acknowledged", "Approved"
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? ManagerComment { get; set; }
    }
}
