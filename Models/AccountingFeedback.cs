using System;

namespace EasyStayHotel.Models
{
    public class AccountingFeedback
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string StaffName { get; set; } = string.Empty;

        public string Branch { get; set; } = "Bangkok";

        public string Shift { get; set; } = "รอบค่ำ/ปิดกะ (Night Shift)";

        public decimal CashAmount { get; set; }

        public decimal TransferAmount { get; set; }

        public int TotalCheckIns { get; set; }

        public int TotalCheckOuts { get; set; }

        public string Category { get; set; } = "รายงานรอบกะ/ประจำวัน";

        public string Message { get; set; } = string.Empty;

        public bool IsAcknowledged { get; set; }

        public string? AcknowledgedBy { get; set; }

        public DateTime? AcknowledgedAt { get; set; }
    }
}
