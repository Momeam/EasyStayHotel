using System;
using System.Collections.Generic;

namespace EasyStayHotel.Models;

/// <summary>
/// เอนทิตี Roomtype: ตัวแทนตาราง roomtypes ในฐานข้อมูล MySQL
/// หน้าที่: จัดเก็บหมวดหมู่หรือประเภทห้องพักของโรงแรม (เช่น Standard, Deluxe, Suite)
/// ความสัมพันธ์ (Relationships):
/// - One-to-Many กับ Room (ประเภทห้องพัก 1 ประเภท มีห้องพักสังกัดได้หลายห้อง)
/// </summary>
public partial class Roomtype
{
    /// <summary>
    /// รหัสประเภทห้องพัก (Primary Key, Auto Increment)
    /// </summary>
    public int RoomTypeId { get; set; }

    /// <summary>
    /// ชื่อประเภทห้องพัก เช่น "Standard Room", "Deluxe Room", "Executive Suite"
    /// </summary>
    public string TypeName { get; set; } = null!;

    /// <summary>
    /// คำอธิบายรายละเอียดของประเภทห้องพัก เช่น สิ่งอำนวยความสะดวก ขนาดห้อง และลักษณะการตกแต่ง
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ราคาพื้นฐานต่อคืน (หน่วย: บาท) ความละเอียดทศนิยม 2 ตำแหน่ง
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// จำนวนผู้เข้าพักสูงสุดที่ห้องประเภทนี้รองรับได้ (เช่น 2 ท่าน, 4 ท่าน)
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Navigation Property: รายชื่อห้องพักทั้งหมดที่สังกัดประเภทนี้
    /// </summary>
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
