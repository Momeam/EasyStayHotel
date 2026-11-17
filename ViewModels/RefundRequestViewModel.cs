using System.ComponentModel.DataAnnotations;

namespace EasyStayHotel.ViewModels
{
    public class RefundRequestViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกธนาคารสำหรับรับเงินคืน")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเลขที่บัญชีธนาคาร")]
        [RegularExpression(@"^[0-9\-]+$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลขเท่านั้น")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุชื่อเจ้าของบัญชีผู้รับเงิน")]
        public string BankAccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์สำหรับติดต่อ")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเหตุผลในการขอคืนเงิน")]
        public string Reason { get; set; } = string.Empty;

        public string? OtherReason { get; set; }
    }
}
