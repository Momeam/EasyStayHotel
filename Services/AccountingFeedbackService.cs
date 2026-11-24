// นำเข้า System สำหรับการใช้งานคลาสพื้นฐาน เช่น DateTime, String
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List
using System.Collections.Generic;
// นำเข้า System.IO สำหรับการอ่านและเขียนไฟล์บนดิสก์
using System.IO;
// นำเข้า System.Linq สำหรับฟังก์ชัน Where, OrderByDescending, FirstOrDefault
using System.Linq;
// นำเข้า System.Text.Json สำหรับ Serialization ข้อมูลเป็น JSON
using System.Text.Json;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก AccountingFeedbackService สำหรับจัดการรายงานปิดกะและยอดเงินสดที่พนักงานต้อนรับส่งมอบให้ฝ่ายบัญชี
    public static class AccountingFeedbackService
    {
        // อ็อบเจกต์ล็อกเธรดเพื่อความปลอดภัยในการอ่าน-เขียนไฟล์พร้อมกัน
        private static readonly object _lock = new object();

        // พาธไดเรกทอรีสำหรับเก็บข้อมูล (wwwroot/data)
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        // พาธไฟล์ JSON สำหรับเก็บข้อมูล Feedback รายงานปิดกะ (wwwroot/data/accounting_feedbacks.json)
        private static readonly string _filePath = Path.Combine(_dataDir, "accounting_feedbacks.json");

        // เมธอดส่วนตัวสำหรับตรวจสอบความพร้อมของไฟล์และสร้างข้อมูลเริ่มต้น
        private static void EnsureFileExists()
        {
            // ตรวจสอบว่าไดเรกทอรี data มีอยู่แล้วหรือไม่
            if (!Directory.Exists(_dataDir))
            {
                // หากยังไม่มี ให้สร้างไดเรกทอรีขึ้นมา
                Directory.CreateDirectory(_dataDir);
            }

            // ตรวจสอบว่าไฟล์ accounting_feedbacks.json มีอยู่แล้วหรือไม่
            if (!File.Exists(_filePath))
            {
                // กำหนดข้อมูลตัวอย่างรายงานส่งกะของพนักงานต้อนรับเริ่มต้น 1 รายการ
                var initialList = new List<AccountingFeedback>
                {
                    new AccountingFeedback
                    {
                        // วันเวลาที่ส่ง
                        CreatedAt = DateTime.Now.AddHours(-2),
                        // ชื่อพนักงานต้อนรับ
                        StaffName = "พนักงานต้อนรับ 001",
                        // สาขา
                        Branch = "Bangkok",
                        // รอบกะ
                        Shift = "รอบบ่าย (Afternoon Shift)",
                        // ยอดเงินสดที่ส่งมอบ
                        CashAmount = 3600,
                        // ยอดเงินโอนดิจิทัล
                        TransferAmount = 7200,
                        // จำนวนรายการเช็คอินในกะ
                        TotalCheckIns = 3,
                        // จำนวนรายการเช็คเอาท์ในกะ
                        TotalCheckOuts = 2,
                        // หมวดหมู่รายงาน
                        Category = "รายงานรอบกะ/ประจำวัน",
                        // ข้อความบันทึกถึงฝ่ายบัญชี
                        Message = "ส่งยอดเงินสดในเก๊ะเรียบร้อย 3,600 บาท มีสลิปพร้อมเพย์ 3 ใบ ยอดรวม 7,200 บาท ตรวจสอบครบถ้วนครับ",
                        // สถานะการลงนามรับทราบ
                        IsAcknowledged = true,
                        // ผู้ลงนามรับทราบ
                        AcknowledgedBy = "คุณสมศรี บัญชี",
                        // เวลาที่ลงนาม
                        AcknowledgedAt = DateTime.Now.AddHours(-1)
                    }
                };
                // แปลงข้อมูลเป็น JSON สตริงแบบ Indented
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                // บันทึกไฟล์
                File.WriteAllText(_filePath, json);
            }
        }

        // เมธอดสำหรับดึงรายการ Feedback และรายงานปิดกะทั้งหมด พร้อมตัวกรองสาขา
        public static List<AccountingFeedback> GetAll(string? branch = null)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON จากไฟล์
                    var json = File.ReadAllText(_filePath);
                    // แปลง JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    // กรองตามสาขาหากมีการระบุ
                    if (!string.IsNullOrEmpty(branch))
                    {
                        list = list.Where(f => f.Branch == branch).ToList();
                    }
                    // เรียงตามเวลาจากใหม่ไปเก่า
                    return list.OrderByDescending(f => f.CreatedAt).ToList();
                }
                catch
                {
                    // ส่งกลับ List ว่างเมื่อเกิดข้อผิดพลาด
                    return new List<AccountingFeedback>();
                }
            }
        }

        // เมธอดสำหรับบันทึกรายงานปิดกะใหม่จากพนักงานต้อนรับ
        public static void Add(AccountingFeedback feedback)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON เดิม
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    // แทรกรายงานใหม่ไว้บนสุด
                    list.Insert(0, feedback);
                    // แปลงเป็น JSON สตริง
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // บันทึกลงไฟล์
                    File.WriteAllText(_filePath, updatedJson);
                }
                catch
                {
                    // ป้องกันโปรแกรมหยุดทำงานหากเกิดข้อผิดพลาด
                }
            }
        }

        // เมธอดสำหรับฝ่ายบัญชีในการลงนามรับทราบรายงานปิดกะ
        public static bool Acknowledge(string id, string accountingUserName)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่านข้อมูล JSON
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    // ค้นหารายการตาม ID
                    var item = list.FirstOrDefault(f => f.Id == id);
                    // หากพบรายการ
                    if (item != null)
                    {
                        // ปรับสถานะเป็นรับทราบแล้ว
                        item.IsAcknowledged = true;
                        // บันทึกชื่อพนักงานบัญชีผู้รับทราบ
                        item.AcknowledgedBy = accountingUserName;
                        // บันทึกวันเวลาที่รับทราบ
                        item.AcknowledgedAt = DateTime.Now;
                        // แปลงเป็น JSON
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        // เขียนทับไฟล์
                        File.WriteAllText(_filePath, updatedJson);
                        // คืนค่า true สำเร็จ
                        return true;
                    }
                }
                catch
                {
                    // จัดการข้อผิดพลาด
                }
                // หากไม่พบรายการ คืนค่า false
                return false;
            }
        }
    }
}
