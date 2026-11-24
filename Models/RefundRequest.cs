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

        public decimal Amount { get; set; }

        public string BankName { get; set; } = string.Empty;

        public string BankAccountNumber { get; set; } = string.Empty;

        public string BankAccountName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime RefundDeadline { get; set; } = DateTime.Now.AddDays(3);

        public string Status { get; set; } = "Pending";

        public string? ProcessedBy { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? AdminNote { get; set; }

        public string? RefundSlipUrl { get; set; }
    }
}
