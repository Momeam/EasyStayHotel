using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล WalkinViewModel: สำหรับพนักงานต้อนรับใช้ทำรายการจองและเปิดห้องพักให้แขกที่มาติดต่อหน้าร้านโดยตรง (Walk-in Booking)
    /// หน้าที่: บันทึกข้อมูลแขก วันที่เข้าพัก ห้องพักที่เลือก บริการเสริม (อาหารเช้า, เตียงเสริม, เช็คเอาท์เลท) และช่องทางรับชำระเงิน
    /// </summary>
    public class WalkinViewModel
    {
        // -------------------------------------------------------------
        // 1. ข้อมูลผู้เข้าพัก (Guest Identification)
        // -------------------------------------------------------------

        /// <summary>
        /// ชื่อ-นามสกุลของผู้เข้าพัก
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = null!;
        
        /// <summary>
        /// อีเมลของผู้เข้าพัก (ระบุหรือไม่ก็ได้)
        /// </summary>
        public string? Email { get; set; }
        
        /// <summary>
        /// เบอร์โทรศัพท์สำหรับติดต่อ
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = null!;
        
        /// <summary>
        /// เลขบัตรประจำตัวประชาชน หรือหนังสือเดินทาง (จำเป็นสำหรับการลงทะเบียนเข้าพักตามกฎหมายโรงแรม)
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเลขบัตรประชาชน หรือ พาสปอร์ต")]
        public string IdCardOrPassport { get; set; } = null!;

        // -------------------------------------------------------------
        // 2. รายละเอียดการจองห้องพัก (Booking Details)
        // -------------------------------------------------------------

        /// <summary>
        /// วันที่เช็คอินเข้าพัก (ค่าเริ่มต้นคือ วันนี้)
        /// </summary>
        [Required]
        public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        
        /// <summary>
        /// วันที่เช็คเอาท์ออกจากโรงแรม (ค่าเริ่มต้นคือ วันพรุ่งนี้)
        /// </summary>
        [Required]
        public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        
        /// <summary>
        /// รหัสห้องพักที่เลือกเข้าพัก (RoomId)
        /// </summary>
        [Required(ErrorMessage = "กรุณาเลือกห้องพัก")]
        public int RoomId { get; set; }

        /// <summary>
        /// รหัสประเภทห้องพัก (RoomTypeId)
        /// </summary>
        public int? RoomTypeId { get; set; }
        
        /// <summary>
        /// จำนวนผู้เข้าพัก (ท่าน)
        /// </summary>
        [Required]
        public int NumberOfGuests { get; set; } = 2;

        // -------------------------------------------------------------
        // 3. บริการเสริมและช่องทางชำระเงิน (Add-ons & Payment)
        // -------------------------------------------------------------

        /// <summary>
        /// รับบริการอาหารเช้า (+250 บาท/ท่าน/วัน)
        /// </summary>
        public bool AddBreakfast { get; set; }

        /// <summary>
        /// ขอเตียงเสริม (+500 บาท/คืน)
        /// </summary>
        public bool AddExtraBed { get; set; }

        /// <summary>
        /// บริการรถรับ-ส่งสนามบิน (+600 บาท)
        /// </summary>
        public bool AddAirportTransfer { get; set; }

        /// <summary>
        /// ขอเช็คเอาท์ล่วงเวลา (Late Check-out ถึง 15:00 น. +300 บาท)
        /// </summary>
        public bool AddLateCheckout { get; set; }

        /// <summary>
        /// วิธีการชำระเงิน: "Cash" (เงินสดหน้าร้าน), "PromptPay" (โอนเงินผ่าน QR Code), "CreditCard" (บัตรเครดิต)
        /// </summary>
        public string PaymentMethod { get; set; } = "Cash";

        /// <summary>
        /// ราคารวมสุทธิทั้งหมดของการจองนี้ (บาท)
        /// </summary>
        public decimal TotalPrice { get; set; }
    }
}
