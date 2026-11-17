using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

/// <summary>
/// เอนทิตี Room: ตัวแทนตาราง rooms ในฐานข้อมูล MySQL
/// หน้าที่: จัดเก็บข้อมูลห้องพักแต่ละห้องของโรงแรม (เช่น หมายเลขห้อง, ประเภทห้อง, สถานะห้อง, และสาขา)
/// ความสัมพันธ์ (Relationships):
/// - Many-to-One กับ Roomtype (ห้องพักหลายห้องสังกัดประเภทห้องพักเดียวกัน)
/// - One-to-Many กับ Booking (ห้องพัก 1 ห้องสามารถมีประวัติการจองได้หลายรายการ)
/// </summary>
public partial class Room
{
    /// <summary>
    /// รหัสประจำตัวห้องพัก (Primary Key, Auto Increment)
    /// </summary>
    public int RoomId { get; set; }

    /// <summary>
    /// หมายเลขห้องพัก เช่น "101", "201", "305"
    /// </summary>
    public string RoomNo { get; set; } = null!;

    /// <summary>
    /// รหัสประเภทห้องพัก (Foreign Key เชื่อมไปยังตาราง roomtypes)
    /// </summary>
    public int? RoomTypeId { get; set; }

    /// <summary>
    /// สถานะห้องพักในปัจจุบัน เช่น "Available" (ว่างพร้อมขาย), "Booked" (จองแล้ว), 
    /// "Occupied" (มีแขกเข้าพักอยู่), "Cleaning" (กำลังทำความสะอาด), "Maintenance" (ปิดซ่อมบำรุง)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// สาขาของโรงแรมที่ห้องพักนี้ตั้งอยู่ เช่น "Bangkok", "Chonburi", "Phuket"
    /// ค่าเริ่มต้นคือ "Bangkok"
    /// </summary>
    public string Branch { get; set; } = "Bangkok";

    /// <summary>
    /// Navigation Property: รายการประวัติการจองทั้งหมดที่เกี่ยวข้องกับห้องพักนี้
    /// </summary>
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>
    /// Navigation Property: ข้อมูลประเภทห้องพัก (ดึงข้อมูลรายละเอียดและราคาจากตาราง roomtypes)
    /// </summary>
    public virtual Roomtype? RoomType { get; set; }
}
