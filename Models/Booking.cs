using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

/// <summary>
/// เอนทิตี Booking: ตัวแทนตาราง bookings ในฐานข้อมูล MySQL
/// หน้าที่: จัดเก็บข้อมูลการทำรายการจองห้องพักของลูกค้าหรือแขก Walk-in
/// ความสัมพันธ์ (Relationships):
/// - Many-to-One กับ User (การจอง 1 รายการผูกกับลูกค้า 1 บัญชี หรือเป็น null กรณี Walk-in)
/// - Many-to-One กับ Room (การจอง 1 รายการระบุห้องพัก 1 ห้อง)
/// - One-to-Many กับ Payment (การจอง 1 รายการอาจมีประวัติการชำระเงินหรือสลิปแนบ)
/// </summary>
public partial class Booking
{
    /// <summary>
    /// รหัสประจำตัวการจองในฐานข้อมูล (Primary Key, Auto Increment)
    /// </summary>
    public int BookingId { get; set; }

    /// <summary>
    /// รหัสอ้างอิงการจองสำหรับแสดงให้ลูกค้า เช่น "ES-20261117120000" (ความยาวไม่เกิน 20 ตัวอักษร)
    /// </summary>
    public string BookingRef { get; set; } = null!;

    /// <summary>
    /// รหัสผู้ใช้ที่เป็นเจ้าของการจอง (Foreign Key เชื่อมไปยังตาราง users, เป็น null ได้กรณี Walk-in)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// ชื่อ-นามสกุลของผู้เข้าพักจริง (สำหรับกรณี Walk-in หรือจองให้บุคคลอื่น)
    /// </summary>
    public string? GuestName { get; set; }

    /// <summary>
    /// รหัสห้องพักที่ถูกจอง (Foreign Key เชื่อมไปยังตาราง rooms)
    /// </summary>
    public int? RoomId { get; set; }

    /// <summary>
    /// วันที่กำหนดเช็คอิน (Check-in Date)
    /// </summary>
    public DateOnly CheckInDate { get; set; }

    /// <summary>
    /// วันที่กำหนดเช็คเอาท์ (Check-out Date)
    /// </summary>
    public DateOnly CheckOutDate { get; set; }

    /// <summary>
    /// ยอดราคาสุทธิของการจอง (หน่วย: บาท) คำนวณหลังหักส่วนลดโปรโมชั่นและรวมบริการเสริมแล้ว
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// สถานะปัจจุบันของการจอง (Booking Status Lifecycle):
    /// - "Pending" = รอการตรวจสอบยอดชำระเงินหรือตรวจสอบสลิป
    /// - "Confirmed" = ยืนยันการจองเรียบร้อย รอวันเข้าพัก
    /// - "CheckedIn" = แขกเดินทางมาถึงและเช็คอินเข้าพักในห้องแล้ว
    /// - "CheckedOut" = แขกเช็คเอาท์ออกจากโรงแรมเรียบร้อยแล้ว
    /// - "Cancelled" = ยกเลิกการจอง
    /// </summary>
    public string? BookingStatus { get; set; }

    /// <summary>
    /// Navigation Property: รายการบันทึกการชำระเงินทั้งหมดที่เกี่ยวข้องกับการจองนี้
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Navigation Property: ข้อมูลห้องพักที่ถูกจอง (เชื่อมโยงกับตาราง rooms)
    /// </summary>
    public virtual Room? Room { get; set; }

    /// <summary>
    /// Navigation Property: ข้อมูลบัญชีผู้ใช้งานที่ทำรายการจอง (เชื่อมโยงกับตาราง users)
    /// </summary>
    public virtual User? User { get; set; }
}
