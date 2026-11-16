using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class WalkinViewModel
    {
        // 1. ข้อมูลผู้เข้าพัก
        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = null!;
        
        public string? Email { get; set; }
        
        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = null!;
        
        [Required(ErrorMessage = "กรุณาระบุเลขบัตรประชาชน หรือ พาสปอร์ต")]
        public string IdCardOrPassport { get; set; } = null!;

        // 2. รายละเอียดการจอง
        [Required]
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        
        [Required]
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        
        [Required(ErrorMessage = "กรุณาเลือกห้องพัก")]
        public int RoomId { get; set; }

        public int? RoomTypeId { get; set; }
        
        [Required]
        public int NumberOfGuests { get; set; } = 2;

        // 3. บริการเสริมและค่าใช้จ่าย
        public bool AddBreakfast { get; set; }
        public bool AddExtraBed { get; set; }
        public bool AddAirportTransfer { get; set; }
        public bool AddLateCheckout { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal TotalPrice { get; set; }
    }
}