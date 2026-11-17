using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล RegisterViewModel: สำหรับหน้าจอลงทะเบียนสมัครสมาชิกใหม่ (Register.cshtml)
    /// หน้าที่: รับและตรวจสอบความถูกต้องของข้อมูลผู้สมัคร พร้อมทั้งรองรับการยืนยันตัวตนด้วยรหัส OTP ทางอีเมล
    /// Data Annotations:
    /// - [Required] บังคับกรอก
    /// - [EmailAddress] รูปแบบอีเมลถูกต้อง
    /// - [RegularExpression] ตรวจสอบรูปแบบเบอร์โทรศัพท์ (9-15 หลัก)
    /// - [Compare] ตรวจสอบการพิมพ์รหัสผ่านซ้ำให้ตรงกัน
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// ชื่อ-นามสกุลจริงของผู้สมัคร
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(100, ErrorMessage = "ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// ที่อยู่อีเมลสำหรับเข้าสู่ระบบและรับรหัส OTP
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [StringLength(100, ErrorMessage = "อีเมลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// เบอร์โทรศัพท์เคลื่อนที่สำหรับติดต่อ
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [RegularExpression(@"^[0-9\-\+\s]{9,15}$", ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง (เช่น 0812345678)")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// เลขประจำตัวประชาชน 13 หลัก หรือเลขที่หนังสือเดินทาง
        /// </summary>
        [Display(Name = "เลขบัตรประชาชน / หนังสือเดินทาง")]
        [StringLength(20, ErrorMessage = "เลขบัตรประชาชนหรือหนังสือเดินทางต้องไม่เกิน 20 ตัวอักษร")]
        public string? IdCardOrPassport { get; set; }

        /// <summary>
        /// วันเดือนปีเกิด (ใช้สำหรับการมอบส่วนลดโปรโมชั่นวันเกิด 10%)
        /// </summary>
        [Display(Name = "วันเดือนปีเกิด")]
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>
        /// รหัสผ่านสำหรับเข้าสู่ระบบ (ความยาวอย่างน้อย 6 ตัวอักษร)
        /// </summary>
        [Required(ErrorMessage = "กรุณาตั้งรหัสผ่าน")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// ยืนยันรหัสผ่านอีกครั้ง (ต้องตรงกับ Password)
        /// </summary>
        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านที่ตั้งไว้")]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// รหัส OTP 6 หลักที่ลูกค้าได้รับทางอีเมล เพื่อยืนยันความถูกต้องของอีเมล
        /// </summary>
        [Display(Name = "รหัส OTP")]
        public string? OtpCode { get; set; }
    }

    /// <summary>
    /// โมเดลคำขอส่งรหัส OTP สมัครสมาชิกผ่านทาง AJAX
    /// </summary>
    public class SendOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// โมเดลคำขอตรวจสอบความถูกต้องของรหัส OTP ทาง AJAX
    /// </summary>
    public class VerifyOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
