namespace EasyStayHotel.Models;

/// <summary>
/// โมเดล ErrorViewModel: ใช้สำหรับส่งข้อมูลข้อผิดพลาดไปยังหน้า View แจ้ง Error (/Home/Error)
/// หน้าที่: จัดเก็บ RequestId เพื่อช่วยในการ Trace หรือค้นหาข้อผิดพลาดใน Log ของระบบ
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// รหัสอ้างอิงคำขอ HTTP (HTTP Request Trace Identifier)
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Property คำนวณ (Computed Property): ตรวจสอบว่ามีค่า RequestId หรือไม่ เพื่อใช้แสดงผลบนหน้าจอ
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
