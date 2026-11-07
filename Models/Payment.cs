using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public decimal Amount { get; set; }

    public string? SlipImageUrl { get; set; }

    public DateTime? TransferDate { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual Booking? Booking { get; set; }
}
