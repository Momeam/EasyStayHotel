using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล ForgotPasswordViewModel: สำหรับหน้าจอกู้คืนรหัสผ่านผ่าน OTP (ForgotPassword.cshtml)
    /// หน้าที่: รับอีเมล รหัส OTP 6 หลัก และรหัสผ่านใหม่ที่ผู้ใช้ต้องการตั้ง
    /// การตรวจสอบข้อมูล (Validation): ตรวจสอบความถูกต้องของอีเมล ความยาว OTP 6 หลัก และความตรงกันของรหัสผ่านใหม่
    /// </summary>
    public class ForgotPasswordViewModel
    {
        /// <summary>
        /// อีเมลที่ลงทะเบียนไว้กับระบบ
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกอีเมลที่ลงทะเบียนไว้")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// รหัสยืนยันตัวตนแบบใช้ครั้งเดียว 6 หลัก (One-Time Password) ที่ส่งไปยังอีเมล
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกรหัสยืนยัน OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "รหัส OTP ต้องมี 6 หลัก")]
        [Display(Name = "รหัส OTP")]
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>
        /// รหัสผ่านใหม่ที่ต้องการตั้ง (อย่างน้อย 6 ตัวอักษร)
        /// </summary>
        [Required(ErrorMessage = "กรุณาตั้งรหัสผ่านใหม่")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านใหม่")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// ยืนยันรหัสผ่านใหม่ (ต้องตรงกับ NewPassword ทุกประการ)
        /// </summary>
        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่")]
        [Display(Name = "ยืนยันรหัสผ่านใหม่")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// โมเดลคำขอส่งรหัส OTP สำหรับกู้คืนรหัสผ่านผ่าน AJAX Request
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// อีเมลเป้าหมายที่จะส่ง OTP กู้คืนรหัสผ่าน
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
