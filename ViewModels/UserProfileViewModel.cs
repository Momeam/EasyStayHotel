using System;
using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.Models.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล UserProfileViewModel: สำหรับหน้าจอโปรไฟล์และแก้ไขข้อมูลส่วนตัวของผู้ใช้ (Profile.cshtml)
    /// หน้าที่: แสดงข้อมูลส่วนตัวของผู้ใช้ และรองรับการแก้ไขชื่อ เบอร์โทรศัพท์ เลขบัตรประชาชน วันเกิด รวมถึงการเปลี่ยนรหัสผ่าน
    /// </summary>
    public class UserProfileViewModel
    {
        /// <summary>
        /// รหัสประจำตัวผู้ใช้ (UserId)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// ชื่อและนามสกุลจริง
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(100, ErrorMessage = "ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// อีเมลของผู้ใช้ (ใช้แสดงผลอย่างเดียว ไม่อนุญาตให้แก้ไขโดยตรงเพื่อความปลอดภัย)
        /// </summary>
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// เบอร์โทรศัพท์สำหรับติดต่อ
        /// </summary>
        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [RegularExpression(@"^[0-9\-\+\s]{9,15}$", ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง (เช่น 0812345678)")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// เลขบัตรประจำตัวประชาชนหรือหนังสือเดินทาง
        /// </summary>
        [Display(Name = "เลขบัตรประชาชน / หนังสือเดินทาง")]
        [StringLength(20, ErrorMessage = "เลขบัตรประชาชนหรือหนังสือเดินทางต้องไม่เกิน 20 ตัวอักษร")]
        public string? IdCardOrPassport { get; set; }

        /// <summary>
        /// วันเดือนปีเกิด (ใช้สำหรับสิทธิพิเศษส่วนลดวันเกิด)
        /// </summary>
        [Display(Name = "วันเดือนปีเกิด")]
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>
        /// ระดับสิทธิ์ของผู้ใช้ (เช่น Customer, Staff, Admin)
        /// </summary>
        [Display(Name = "ระดับผู้ใช้งาน")]
        public string? Role { get; set; }

        /// <summary>
        /// สาขาประจำการ (สำหรับพนักงานและผู้ดูแลระบบ)
        /// </summary>
        [Display(Name = "สาขาประจำ")]
        public string? Branch { get; set; }

        // ==========================================
        // ส่วนการเปลี่ยนรหัสผ่าน (Password Change Section)
        // (กรอกเฉพาะเมื่อผู้ใช้ต้องการเปลี่ยนรหัสผ่านใหม่)
        // ==========================================

        /// <summary>
        /// รหัสผ่านปัจจุบัน (ต้องกรอกเพื่อยืนยันตัวตนก่อนเปลี่ยนรหัสใหม่)
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านปัจจุบัน")]
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// รหัสผ่านใหม่ที่ต้องการเปลี่ยน (ความยาวอย่างน้อย 6 ตัวอักษร)
        /// </summary>
        [MinLength(6, ErrorMessage = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่านใหม่")]
        public string? NewPassword { get; set; }

        /// <summary>
        /// ยืนยันรหัสผ่านใหม่ (ต้องตรงกับ NewPassword)
        /// </summary>
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่")]
        [Display(Name = "ยืนยันรหัสผ่านใหม่")]
        public string? ConfirmNewPassword { get; set; }
    }
}
