using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class CustomerCheckoutViewModel
    {
        public int RoomId { get; set; }

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int Guests { get; set; }

        // จำนวนห้องพักหลักที่ต้องการจองพร้อมกัน (ค่าเริ่มต้น 1 ห้อง)
        public int RoomsCount { get; set; } = 1;

        // รายการประเภทห้องพักเพิ่มเติม (กรณีลูกค้ากด "+ จองห้องพักเพิ่ม" ในสาขาเดียวกัน)
        public System.Collections.Generic.List<int> ExtraRoomTypeIds { get; set; } = new System.Collections.Generic.List<int>();

        // จำนวนห้องของแต่ละรายการห้องพักเพิ่มเติม
        public System.Collections.Generic.List<int> ExtraRoomCounts { get; set; } = new System.Collections.Generic.List<int>();

        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        public int ExtraBedCount { get; set; } = 0;

        public int BreakfastCount { get; set; } = 0;

        public int TransferCount { get; set; } = 0;

        public string? PromoCode { get; set; }

        public decimal TotalPrice { get; set; }

        public IFormFile? PaymentSlip { get; set; }
    }
}
