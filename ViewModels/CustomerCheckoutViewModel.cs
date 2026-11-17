using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล CustomerCheckoutViewModel: สำหรับหน้าจอ Checkout และชำระเงินของลูกค้า (Checkout.cshtml)
    /// หน้าที่: รวบรวมข้อมูลสรุปห้องพักที่เลือก ข้อมูลผู้เข้าพัก บริการเสริม (Add-ons) โค้ดโปรโมชั่น และไฟล์สลิปหลักฐานการชำระเงิน
    /// คำถามที่อาจารย์มักถาม:
    /// - ViewModel ต่างจาก Model อย่างไร?
    ///   -> Model เป็นตัวแทนตารางในฐานข้อมูล ส่วน ViewModel ออกแบบมาเฉพาะเพื่อรองรับข้อมูลที่หน้า View ต้องการนำเสนอและรับค่าจากฟอร์ม
    /// </summary>
    public class CustomerCheckoutViewModel
    {
        // -------------------------------------------------------------
        // 1. ข้อมูลห้องพักและการจอง (ส่งมาจากหน้ารายละเอียดห้องพัก)
        // -------------------------------------------------------------

        /// <summary>
        /// รหัสห้องพักที่เลือกจอง
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// ชื่อประเภทห้องพัก เช่น "Deluxe Room (ดีลักซ์)"
        /// </summary>
        public string RoomTypeName { get; set; } = string.Empty;

        /// <summary>
        /// ราคาห้องพักต่อคืน (หน่วย: บาท)
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// วันที่เช็คอินเข้าพัก
        /// </summary>
        public DateTime CheckInDate { get; set; }

        /// <summary>
        /// วันที่เช็คเอาท์ออกจากโรงแรม
        /// </summary>
        public DateTime CheckOutDate { get; set; }

        /// <summary>
        /// จำนวนคืนที่เข้าพัก (คำนวณจาก CheckOutDate - CheckInDate)
        /// </summary>
        public int Nights { get; set; }

        /// <summary>
        /// จำนวนผู้เข้าพัก (ท่าน)
        /// </summary>
        public int Guests { get; set; }

        // -------------------------------------------------------------
        // 2. ข้อมูลผู้เข้าพัก (รับค่าจากฟอร์มที่ลูกค้ากรอก)
        // -------------------------------------------------------------

        /// <summary>
        /// ชื่อ-นามสกุลของผู้เข้าพัก
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        public string GuestName { get; set; } = string.Empty;
        
        /// <summary>
        /// อีเมลสำหรับรับใบยืนยันการจองและแจ้งเตือน
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// เบอร์โทรศัพท์สำหรับติดต่อฉุกเฉิน
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        // -------------------------------------------------------------
        // 3. บริการเสริม (Add-on Services) - คิดค่าบริการตามจำนวน
        // -------------------------------------------------------------

        /// <summary>
        /// จำนวนเตียงเสริม (+500 บาท/คืน)
        /// </summary>
        public int ExtraBedCount { get; set; } = 0;

        /// <summary>
        /// จำนวนชุดอาหารเช้าบุฟเฟต์ (+250 บาท/ท่าน/วัน)
        /// </summary>
        public int BreakfastCount { get; set; } = 0;

        /// <summary>
        /// บริการรถรับ-ส่งสนามบิน (+600 บาท/เที่ยว)
        /// </summary>
        public int TransferCount { get; set; } = 0;

        // -------------------------------------------------------------
        // 4. การคำนวณราคา โปรโมชั่น และหลักฐานการชำระเงิน
        // -------------------------------------------------------------

        /// <summary>
        /// รหัสคูปองส่วนลดที่ลูกค้ากรอก (เช่น "EARLYBIRD", "LONGSTAY", "BIRTHDAY10", "GROUP15", "WEEKDAY5")
        /// </summary>
        public string? PromoCode { get; set; }

        /// <summary>
        /// ยอดราคาสุทธิที่ต้องชำระ (บาท)
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// ไฟล์รูปภาพสลิปหลักฐานการโอนเงิน (รับผ่าน Multipart Form)
        /// </summary>
        public IFormFile? PaymentSlip { get; set; }
    }
}
