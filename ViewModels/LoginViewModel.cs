using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล LoginViewModel: สำหรับหน้าจอเข้าสู่ระบบ (Login.cshtml)
    /// หน้าที่: รับและตรวจสอบข้อมูลบัญชีผู้ใช้ (อีเมลและรหัสผ่าน) เพื่อทำกระบวนการยืนยันตัวตน (Authentication)
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// อีเมลของผู้ใช้งาน (Username)
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// รหัสผ่านสำหรับเข้าสู่ระบบ
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// ตัวเลือกการจดจำการเข้าสู่ระบบ
        /// </summary>
        [Display(Name = "จดจำการเข้าสู่ระบบ")]
        public bool RememberMe { get; set; } = false;
    }
}
