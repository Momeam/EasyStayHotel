using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(100, ErrorMessage = "ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [StringLength(100, ErrorMessage = "อีเมลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [RegularExpression(@"^[0-9\-\+\s]{9,15}$", ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง (เช่น 0812345678)")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "เลขบัตรประชาชน / หนังสือเดินทาง")]
        [StringLength(20, ErrorMessage = "เลขบัตรประชาชนหรือหนังสือเดินทางต้องไม่เกิน 20 ตัวอักษร")]
        public string? IdCardOrPassport { get; set; }

        [Display(Name = "วันเดือนปีเกิด")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "กรุณาตั้งรหัสผ่าน")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านที่ตั้งไว้")]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // รหัส OTP สำหรับยืนยันอีเมล
        [Display(Name = "รหัส OTP")]
        public string? OtpCode { get; set; }
    }

    public class SendOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
