using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมลที่ลงทะเบียนไว้")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสยืนยัน OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "รหัส OTP ต้องมี 6 หลัก")]
        [Display(Name = "รหัส OTP")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาตั้งรหัสผ่านใหม่")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านใหม่")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่")]
        [Display(Name = "ยืนยันรหัสผ่านใหม่")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
