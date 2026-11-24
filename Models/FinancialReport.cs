using System;

namespace EasyStayHotel.Models
{
    public class FinancialReport
    {
        public string ReportId { get; set; } = "FR-" + DateTime.Now.ToString("yyyyMMddHHmmss");

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; } = "ฝ่ายบัญชี";

        public string PeriodType { get; set; } = "Daily";

        public string Branch { get; set; } = "ALL";

        public string PeriodLabel { get; set; } = string.Empty;

        public decimal TotalRevenue { get; set; }

        public decimal CashAmount { get; set; }

        public decimal TransferAmount { get; set; }

        public decimal RefundAmount { get; set; }

        public int TotalBookings { get; set; }

        public decimal BangkokRevenue { get; set; }

        public decimal ChonburiRevenue { get; set; }

        public decimal PhuketRevenue { get; set; }

        public string? Notes { get; set; }

        public string Status { get; set; } = "Pending";

        public string? AcknowledgedBy { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        public string? ManagerComment { get; set; }
    }
}
