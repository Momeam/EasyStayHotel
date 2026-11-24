using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class WalkinViewModel
    {
        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = null!;

        public string? Email { get; set; }

        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "กรุณาระบุเลขบัตรประชาชน หรือ พาสปอร์ต")]
        public string IdCardOrPassport { get; set; } = null!;

        [Required]
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required]
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required(ErrorMessage = "กรุณาเลือกห้องพัก")]
        public int RoomId { get; set; }

        public int? RoomTypeId { get; set; }

        [Required]
        public int NumberOfGuests { get; set; } = 2;

        public bool AddBreakfast { get; set; }

        public bool AddExtraBed { get; set; }

        public bool AddAirportTransfer { get; set; }

        public bool AddLateCheckout { get; set; }

        public string PaymentMethod { get; set; } = "Cash";

        public decimal TotalPrice { get; set; }
    }
}
