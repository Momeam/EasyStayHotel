using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class CustomerCheckoutViewModel
    {
        // 1. ข้อมูลห้องพักและการจอง (ดึงมาจากระบบ)
        public int RoomId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }
        public int Guests { get; set; }

        // 2. ข้อมูลลูกค้า (ให้ลูกค้ากรอก)
        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "กรุณาระบุอีเมล")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        // 3. บริการเสริม (Add-ons) - เก็บเป็นจำนวน (Quantity)
        public int ExtraBedCount { get; set; } = 0;
        public int BreakfastCount { get; set; } = 0;
        public int TransferCount { get; set; } = 0;

        // 4. การชำระเงินและโปรโมชั่น
        public string? PromoCode { get; set; }
        public decimal TotalPrice { get; set; }
        public IFormFile? PaymentSlip { get; set; }
    }
}