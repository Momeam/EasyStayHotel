using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

public partial class Roomtype
{
    public int RoomTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public int Capacity { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
