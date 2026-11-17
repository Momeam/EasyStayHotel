using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

/// <summary>
/// เอนทิตี Payment: ตัวแทนตาราง payments ในฐานข้อมูล MySQL
/// หน้าที่: บันทึกข้อมูลธุรกรรมการชำระเงินของลูกค้า (ช่องทางการชำระ, ยอดเงิน, รูปภาพสลิปโอนเงิน, และสถานะการตรวจสอบ)
/// ความสัมพันธ์ (Relationships):
/// - Many-to-One กับ Booking (การชำระเงินแต่ละรายการเชื่อมโยงไปยังการจอง 1 รายการ)
/// </summary>
public partial class Payment
{
    /// <summary>
    /// รหัสประจำตัวการชำระเงิน (Primary Key, Auto Increment)
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// รหัสการจองที่ชำระเงิน (Foreign Key เชื่อมไปยังตาราง bookings)
    /// </summary>
    public int? BookingId { get; set; }

    /// <summary>
    /// วิธีหรือช่องทางการชำระเงิน เช่น "BankTransfer" (โอนผ่านธนาคาร/PromptPay), "CreditCard" (บัตรเครดิต), "Cash" (เงินสดหน้าร้าน)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// จำนวนเงินที่ชำระ (หน่วย: บาท)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// เส้นทางที่อยู่ไฟล์รูปภาพสลิปหลักฐานการโอนเงิน (Path: /uploads/slips/...)
    /// </summary>
    public string? SlipImageUrl { get; set; }

    /// <summary>
    /// วันเวลาที่ลูกค้าโอนเงินตามที่ระบุในสลิป
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// สถานะการชำระเงิน:
    /// - "Pending" = รอการตรวจสอบสลิปจากเจ้าหน้าที่
    /// - "Completed" / "Approved" = เจ้าหน้าที่ตรวจสอบและยืนยันยอดเงินเรียบร้อย
    /// - "Rejected" = สลิปไม่ถูกต้อง หรือยอดเงินไม่ตรง
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Navigation Property: ข้อมูลการจองที่เชื่อมโยงกับการชำระเงินนี้
    /// </summary>
    public virtual Booking? Booking { get; set; }
}
