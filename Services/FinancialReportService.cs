using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    public static class FinancialReportService
    {
        private static readonly object _lock = new object();

        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string _filePath = Path.Combine(_dataDir, "financial_reports.json");

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
