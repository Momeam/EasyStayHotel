using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingRef { get; set; } = null!;

    public int? UserId { get; set; }

    public string? GuestName {get; set;}

    public int? RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public decimal TotalPrice { get; set; }

    public string? BookingStatus { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room? Room { get; set; }

    public virtual User? User { get; set; }
}
