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

        public string Status { get; set; } = "Pending";

        public string Priority { get; set; } = "Normal";

        public string Source { get; set; } = "CheckOut";

        public string RequestedBy { get; set; } = "พนักงานต้อนรับ";

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public string? GuestName { get; set; }

        public string? Notes { get; set; }

        public string? HousekeeperName { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? CompletionNotes { get; set; }

        public List<string> Checklist { get; set; } = new List<string>();
    }
}
