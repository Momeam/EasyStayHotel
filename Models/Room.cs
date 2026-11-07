using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomNo { get; set; } = null!;

    public int? RoomTypeId { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Roomtype? RoomType { get; set; }
}
