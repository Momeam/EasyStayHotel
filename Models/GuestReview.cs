using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models
{
    public class GuestReview
    {
        public string ReviewId { get; set; } = Guid.NewGuid().ToString("N");

        public int BookingId { get; set; }

        public string BookingRef { get; set; } = string.Empty;

        public string GuestName { get; set; } = string.Empty;

        public string? GuestEmail { get; set; }

        public string Branch { get; set; } = "Bangkok";

        public string RoomTypeName { get; set; } = "Standard";

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public int CleanlinessRating { get; set; } = 5;

        public int ServiceRating { get; set; } = 5;

        public int LocationRating { get; set; } = 5;

        public string? Title { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsVerifiedStay { get; set; } = true;

        public string? StaffReply { get; set; }

        public DateTime? StaffRepliedAt { get; set; }
    }
}
