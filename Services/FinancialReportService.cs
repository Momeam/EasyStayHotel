using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// เซอร์วิส FinancialReportService: จัดการรายงานสรุปทางการเงิน รายได้ และการปิดรอบบัญชีของโรงแรม (Financial & Revenue Reporting)
    /// หน้าที่:
    /// - จัดเก็บประวัติรายงานการเงินที่ฝ่ายบัญชีสร้างขึ้น (รายวัน, รายสัปดาห์, รายเดือน)
    /// - นำเสนอข้อมูลให้ผู้จัดการ/ผู้บริหาร (Admin/Manager) ตรวจรับทราบ (Acknowledge) หรืออนุมัติปิดยอด (Approve)
    /// - จัดเก็บในรูปแบบไฟล์ JSON (wwwroot/data/financial_reports.json) เพื่อความยืดหยุ่นและการทำงานแบบไม่ขึ้นกับโครงสร้างตารางหลัก
    /// </summary>
    public static class FinancialReportService
    {
        private static readonly object _lock = new object();
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string _filePath = Path.Combine(_dataDir, "financial_reports.json");

        /// <summary>
        /// ตรวจสอบการมีอยู่ของไฟล์ข้อมูล หากยังไม่มี จะสร้างโฟลเดอร์และใส่ข้อมูลรายงานเริ่มต้น
        /// </summary>
        private static void EnsureFileExists()
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            if (!File.Exists(_filePath))
            {
                var initialList = new List<FinancialReport>
                {
                    new FinancialReport
                    {
                        ReportId = "FR-" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "-01",
                        CreatedAt = DateTime.Now.AddDays(-1).Date.AddHours(18),
                        CreatedBy = "ฝ่ายบัญชีและการเงิน (Accounting Dept.)",
                        PeriodType = "Daily",
                        Branch = "ALL",
                        PeriodLabel = $"ประจำวันที่ {DateTime.Now.AddDays(-1):dd/MM/yyyy}",
                        TotalRevenue = 32800,
                        CashAmount = 8400,
                        TransferAmount = 24400,
                        RefundAmount = 0,
                        TotalBookings = 12,
                        BangkokRevenue = 15200,
                        ChonburiRevenue = 9600,
                        PhuketRevenue = 8000,
                        Notes = "สรุปยอดรับชำระประจำวันทุกสาขาเรียบร้อย สาขากรุงเทพฯ มีอัตราการเข้าพักสูงสุด และไม่มีรายการขอคืนเงินที่ค้างอยู่ครับ",
                        Status = "Approved",
                        AcknowledgedBy = "ผู้จัดการทั่วไป (General Manager)",
                        AcknowledgedAt = DateTime.Now.AddDays(-1).Date.AddHours(19).AddMinutes(30),
                        ManagerComment = "รับทราบยอดเงินครบถ้วน จัดสรรรายได้เข้าบัญชีหลักเรียบร้อยดี"
                    }
                };
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        /// <summary>
        /// ดึงรายการรายงานทางการเงินทั้งหมด กรองตามสาขา และเรียงจากรายงานล่าสุดไปหาเก่าสุด
        /// </summary>
        public static List<FinancialReport> GetAll(string? branch = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    if (!string.IsNullOrEmpty(branch) && branch != "ALL")
                    {
                        list = list.Where(r => r.Branch == branch || r.Branch == "ALL").ToList();
                    }
                    return list.OrderByDescending(r => r.CreatedAt).ToList();
                }
                catch
                {
                    return new List<FinancialReport>();
                }
            }
        }

        /// <summary>
        /// ค้นหารายงานทางการเงินจาก ReportId
        /// </summary>
        public static FinancialReport? GetById(string reportId)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    return list.FirstOrDefault(r => r.ReportId == reportId);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// บันทึกรายงานทางการเงินฉบับใหม่ที่ฝ่ายบัญชีสร้างขึ้น
        /// </summary>
        public static bool Create(FinancialReport report)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    list.Insert(0, report);
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// ผู้จัดการ/ผู้บริหารตรวจรับทราบหรืออนุมัติรายงานทางการเงิน พร้อมบันทึกความเห็น
        /// </summary>
        public static bool Acknowledge(string reportId, string adminName, string? managerComment = null, bool approve = true)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<FinancialReport>>(json) ?? new List<FinancialReport>();
                    var report = list.FirstOrDefault(r => r.ReportId == reportId);
                    if (report != null)
                    {
                        report.Status = approve ? "Approved" : "Acknowledged";
                        report.AcknowledgedBy = adminName;
                        report.AcknowledgedAt = DateTime.Now;
                        if (!string.IsNullOrWhiteSpace(managerComment))
                        {
                            report.ManagerComment = managerComment.Trim();
                        }
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_filePath, updatedJson);
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
