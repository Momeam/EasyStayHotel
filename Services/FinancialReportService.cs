// นำเข้า System สำหรับฟังก์ชันพื้นฐาน เช่น DateTime, String
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List
using System.Collections.Generic;
// นำเข้า System.IO สำหรับอ่านและเขียนไฟล์บนดิสก์
using System.IO;
// นำเข้า System.Linq สำหรับการค้นหาและกรองข้อมูลด้วย LINQ
using System.Linq;
// นำเข้า System.Text.Json สำหรับ Serialization ข้อมูล JSON
using System.Text.Json;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก FinancialReportService สำหรับบันทึกและจัดการรายงานการเงินที่ฝ่ายบัญชีส่งมอบให้ผู้จัดการ (Admin)
    public static class FinancialReportService
    {
        // อ็อบเจกต์ล็อกเธรดเพื่อความปลอดภัยในการเข้าถึงไฟล์พร้อมกัน
        private static readonly object _lock = new object();

        // พาธไดเรกทอรีสำหรับเก็บข้อมูล (wwwroot/data)
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        // พาธไฟล์ JSON สำหรับเก็บรายงานการเงิน (wwwroot/data/financial_reports.json)
        private static readonly string _filePath = Path.Combine(_dataDir, "financial_reports.json");

        // เมธอดส่วนตัวสำหรับตรวจสอบและสร้างไฟล์ข้อมูลเริ่มต้นหากยังไม่มี
        private static void EnsureFileExists()
        {
            // ตรวจสอบว่าไดเรกทอรี data มีอยู่แล้วหรือไม่
            if (!Directory.Exists(_dataDir))
            {
                // หากยังไม่มี ให้สร้างไดเรกทอรีขึ้นมา
                Directory.CreateDirectory(_dataDir);
            }

            // ตรวจสอบว่าไฟล์ financial_reports.json มีอยู่แล้วหรือไม่
            if (!File.Exists(_filePath))
            {
                // กำหนดข้อมูลตัวอย่างรายงานการเงินเริ่มต้นที่ได้รับการอนุมัติแล้ว 1 รายการ
                var initialList = new List<FinancialReport>
                {
                    new FinancialReport
                    {
                        // รหัสรายงาน
                        ReportId = "FR-" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "-01",
                        // วันเวลาที่สร้าง
                        CreatedAt = DateTime.Now.AddDays(-1).Date.AddHours(18),
                        // ผู้จัดทำ
                        CreatedBy = "ฝ่ายบัญชีและการเงิน (Accounting Dept.)",
                        // ประเภทช่วงเวลา
                        PeriodType = "Daily",
                        // สาขา
                        Branch = "ALL",
                        // ป้ายกำกับช่วงเวลา
                        PeriodLabel = $"ประจำวันที่ {DateTime.Now.AddDays(-1):dd/MM/yyyy}",
                        // รายรับรวม
                        TotalRevenue = 32800,
                        // ยอดเงินสด
                        CashAmount = 8400,
                        // ยอดเงินโอน
                        TransferAmount = 24400,
                        // ยอดคืนเงิน
                        RefundAmount = 0,
                        // จำนวนการจอง
                        TotalBookings = 12,
                        // รายรับกรุงเทพฯ
                        BangkokRevenue = 15200,
                        // รายรับชลบุรี
                        ChonburiRevenue = 9600,
                        // รายรับภูเก็ต
                        PhuketRevenue = 8000,
                        // หมายเหตุจากบัญชี
                        Notes = "สรุปยอดรับชำระประจำวันทุกสาขาเรียบร้อย สาขากรุงเทพฯ มีอัตราการเข้าพักสูงสุด และไม่มีรายการขอคืนเงินที่ค้างอยู่ครับ",
                        // สถานะการอนุมัติ
                        Status = "Approved",
                        // ผู้จัดการที่ลงนาม
                        AcknowledgedBy = "ผู้จัดการทั่วไป (General Manager)",
                        // เวลาที่ลงนาม
                        AcknowledgedAt = DateTime.Now.AddDays(-1).Date.AddHours(19).AddMinutes(30),
                        // ข้อคิดเห็นตอบกลับจากผู้จัดการ
                        ManagerComment = "รับทราบยอดเงินครบถ้วน จัดสรรรายได้เข้าบัญชีหลักเรียบร้อยดี"
                    }
                };
                // แปลงเป็น JSON สตริงแบบมีจัดย่อหน้า
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                // บันทึกลงไฟล์
                File.WriteAllText(_filePath, json);
            }
        }

        // เมธอดดึงรายการรายงานสรุปการเงินทั้งหมด พร้อมรองรับการกรองตามสาขา
        public static List<FinancialReport> GetAll(string? branch = null)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบความพร้อมของไฟล์
                    EnsureFileExists();
                    // อ่านข้อความ JSON จากไฟล์
                    var json = File.ReadAllText(_filePath);
                    // แปลง JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    // กรองตามสาขาหากระบุ และไม่ใช่ ALL
                    if (!string.IsNullOrEmpty(branch) && branch != "ALL")
                    {
                        list = list.Where(r => r.Branch == branch || r.Branch == "ALL").ToList();
                    }
                    // เรียงตามเวลาที่สร้าง จากใหม่ไปเก่า
                    return list.OrderByDescending(r => r.CreatedAt).ToList();
                }
                catch
                {
                    // คืนค่า List ว่างเมื่อเกิดข้อผิดพลาด
                    return new List<FinancialReport>();
                }
            }
        }

        // เมธอดค้นหารายงานการเงินตาม ReportId
        public static FinancialReport? GetById(string reportId)
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
                    // แปลง JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    // ค้นหารายงานที่ตรงกับ ReportId
                    return list.FirstOrDefault(r => r.ReportId == reportId);
                }
                catch
                {
                    // คืนค่า null เมื่อเกิดข้อผิดพลาด
                    return null;
                }
            }
        }

        // เมธอดสำหรับบันทึกรายงานการเงินใหม่ที่ส่งมาจากฝ่ายบัญชี
        public static bool Create(FinancialReport report)
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
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    // แทรกรายงานใหม่ไว้ตำแหน่งแรกสุด
                    list.Insert(0, report);
                    // แปลงกลับเป็น JSON
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // เขียนทับไฟล์
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true สำเร็จ
                    return true;
                }
                catch
                {
                    // คืนค่า false เมื่อเกิดข้อผิดพลาด
                    return false;
                }
            }
        }

        // เมธอดสำหรับผู้จัดการ (Admin) ในการลงนามรับทราบหรืออนุมัติรายงานการเงิน
        public static bool Acknowledge(string reportId, string adminName, string? managerComment = null, bool approve = true)
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
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    // ค้นหารายงานที่ตรงกับ ReportId
                    var report = list.FirstOrDefault(r => r.ReportId == reportId);
                    // หากพบรายงาน
                    if (report != null)
                    {
                        // ปรับสถานะเป็น Approved หรือ Acknowledged ตามพารามิเตอร์ approve
                        report.Status = approve ? "Approved" : "Acknowledged";
                        // บันทึกชื่อผู้จัดการที่ลงนาม
                        report.AcknowledgedBy = adminName;
                        // บันทึกวันเวลาที่ลงนาม
                        report.AcknowledgedAt = DateTime.Now;
                        // บันทึกข้อคิดเห็นตอบกลับจากผู้จัดการ (ถ้ามี)
                        if (!string.IsNullOrWhiteSpace(managerComment))
                        {
                            report.ManagerComment = managerComment.Trim();
                        }
                        // แปลงข้อมูลเป็น JSON
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        // บันทึกลงไฟล์
                        File.WriteAllText(_filePath, updatedJson);
                        // คืนค่า true สำเร็จ
                        return true;
                    }
                    // หากไม่พบรายงาน คืนค่า false
                    return false;
                }
                catch
                {
                    // เกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }
    }
}
