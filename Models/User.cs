using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

/// <summary>
/// เอนทิตี User: ตัวแทนตาราง users ในฐานข้อมูล MySQL
/// หน้าที่: จัดเก็บข้อมูลผู้ใช้งานระบบทุกประเภท (ลูกค้า, พนักงานต้อนรับ, ผู้ดูแลระบบ, ฝ่ายบัญชี, แม่บ้าน)
/// ความสัมพันธ์ (Relationships):
/// - One-to-Many กับ Booking (ลูกค้า 1 คน สามารถมีประวัติการจองห้องพักได้หลายครั้ง)
/// </summary>
public partial class User
{
    /// <summary>
    /// รหัสประจำตัวผู้ใช้งาน (Primary Key, Auto Increment)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// ที่อยู่อีเมล (ใช้เป็น Username สำหรับเข้าสู่ระบบ และรับอีเมลแจ้งเตือน / รหัส OTP) มี Index แบบ Unique
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// รหัสผ่านสำหรับเข้าสู่ระบบ
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// ชื่อและนามสกุลจริงของผู้ใช้งาน
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// เบอร์โทรศัพท์สำหรับติดต่อ
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// เลขบัตรประจำตัวประชาชน หรือเลขหนังสือเดินทาง (ใช้สำหรับการยืนยันตัวตนตอนเช็คอิน)
    /// </summary>
    public string? IdCardOrPassport { get; set; }

    /// <summary>
    /// ระดับสิทธิ์หรือตำแหน่งงานของผู้ใช้ (Role-Based Access Control)
    /// ค่าที่เป็นไปได้:
    /// - "Customer" = ลูกค้าทั่วไป
    /// - "Staff" = พนักงานต้อนรับ (Front Desk)
    /// - "Admin" / "SuperAdmin" = ผู้ดูแลระบบ / ผู้จัดการ
    /// - "Accounting" = เจ้าหน้าที่ฝ่ายบัญชีและการเงิน
    /// - "Housekeeper" / "Maid" = พนักงานแม่บ้านทำความสะอาด
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// สาขาที่ผู้ใช้ประจำการอยู่ (สำหรับพนักงานและผู้ดูแลระบบ เช่น "Bangkok", "Chonburi", "Phuket")
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// วันเดือนปีเกิด (ใช้สำหรับระบบสิทธิพิเศษและโปรโมชั่นส่วนลดต้อนรับเดือนเกิด)
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Navigation Property: ประวัติรายการจองทั้งหมดของผู้ใช้งานคนนี้
    /// </summary>
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
