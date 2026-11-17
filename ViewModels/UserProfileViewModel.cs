using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(100, ErrorMessage = "ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

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

        [Display(Name = "ระดับผู้ใช้งาน")]
        public string? Role { get; set; }

        [Display(Name = "สาขาประจำ")]
        public string? Branch { get; set; }

        // --- ส่วนการเปลี่ยนรหัสผ่าน (ถ้าต้องการเปลี่ยน ให้ระบุ) ---
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านปัจจุบัน")]
        public string? CurrentPassword { get; set; }

        [MinLength(6, ErrorMessage = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านใหม่")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่")]
        [Display(Name = "ยืนยันรหัสผ่านใหม่")]
        public string? ConfirmNewPassword { get; set; }
    }
}
