using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.ViewModels
{
    /// <summary>
    /// วิวม็อดเดล RefundRequestViewModel: สำหรับฟอร์มยื่นคำร้องขอยกเลิกการจองและขอคืนเงินของลูกค้า
    /// หน้าที่: รับข้อมูลบัญชีธนาคาร เหตุผลในการขอยกเลิก และเบอร์โทรศัพท์ติดต่อ เพื่อส่งต่อให้ฝ่ายบัญชีดำเนินการโอนเงินคืน
    /// เงื่อนไข: ตามนโยบายโรงแรม ลูกค้าต้องยื่นเรื่องภายใน 7 วันนับจากวันที่ทำการจอง
    /// </summary>
    public class RefundRequestViewModel
    {
        /// <summary>
        /// รหัสการจองที่ต้องการขอยกเลิกและขอคืนเงิน
        /// </summary>
        [Required]
        public int BookingId { get; set; }

        /// <summary>
        /// ชื่อธนาคารสำหรับรับเงินคืน เช่น ธนาคารกสิกรไทย, ธนาคารไทยพาณิชย์
        /// </summary>
        [Required(ErrorMessage = "กรุณาเลือกธนาคารสำหรับรับเงินคืน")]
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// เลขที่บัญชีธนาคารสำหรับรับเงินคืน (ตรวจสอบว่าประกอบด้วยตัวเลขและขีดเท่านั้น)
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเลขที่บัญชีธนาคาร")]
        [RegularExpression(@"^[0-9\-]+$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลขเท่านั้น")]
        public string BankAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// ชื่อ-นามสกุลของเจ้าของบัญชีผู้รับเงิน (ต้องตรงกับชื่อผู้จอง)
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุชื่อเจ้าของบัญชีผู้รับเงิน")]
        public string BankAccountName { get; set; } = string.Empty;

        /// <summary>
        /// เบอร์โทรศัพท์สำหรับติดต่อสอบถามข้อมูลเพิ่มเติม
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์สำหรับติดต่อ")]
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// เหตุผลหลักในการขอยกเลิกและขอคืนเงิน
        /// </summary>
        [Required(ErrorMessage = "กรุณาระบุเหตุผลในการขอคืนเงิน")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// รายละเอียดหรือเหตุผลเพิ่มเติม (กรณีเลือกเหตุผลอื่นๆ)
        /// </summary>
        public string? OtherReason { get; set; }
    }
}
