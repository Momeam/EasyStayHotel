using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// เซอร์วิส AccountingFeedbackService: ระบบสื่อสารและการส่งยอดเงินปิดกะระหว่างพนักงานต้อนรับหน้าร้านกับฝ่ายบัญชี
    /// หน้าที่:
    /// - พนักงานหน้าร้านบันทึกยอดเงินสด ยอดเงินโอน จำนวนเช็คอิน/เช็คเอาท์ ในแต่ละรอบกะการทำงาน (Shift Handover)
    /// - ฝ่ายบัญชีตรวจสอบและกดยืนยันการรับทราบยอดเงิน (Acknowledge)
    /// - บันทึกข้อมูลในไฟล์ JSON (wwwroot/data/accounting_feedbacks.json) พร้อมระบบ Thread-Safe Lock
    /// </summary>
    public static class AccountingFeedbackService
    {
        private static readonly object _lock = new object();
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string _filePath = Path.Combine(_dataDir, "accounting_feedbacks.json");

        /// <summary>
        /// ตรวจสอบการมีอยู่ของไฟล์ข้อมูล หากยังไม่มี จะสร้างโฟลเดอร์และใส่ข้อมูลตัวอย่างเริ่มต้น
        /// </summary>
        private static void EnsureFileExists()
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            if (!File.Exists(_filePath))
            {
                var initialList = new List<AccountingFeedback>
                {
                    new AccountingFeedback
                    {
                        CreatedAt = DateTime.Now.AddHours(-2),
                        StaffName = "พนักงานต้อนรับ 001",
                        Branch = "Bangkok",
                        Shift = "รอบบ่าย (Afternoon Shift)",
                        CashAmount = 3600,
                        TransferAmount = 7200,
                        TotalCheckIns = 3,
                        TotalCheckOuts = 2,
                        Category = "รายงานรอบกะ/ประจำวัน",
                        Message = "ส่งยอดเงินสดในเก๊ะเรียบร้อย 3,600 บาท มีสลิปพร้อมเพย์ 3 ใบ ยอดรวม 7,200 บาท ตรวจสอบครบถ้วนครับ",
                        IsAcknowledged = true,
                        AcknowledgedBy = "คุณสมศรี บัญชี",
                        AcknowledgedAt = DateTime.Now.AddHours(-1)
                    }
                };
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        /// <summary>
        /// ดึงรายการรายงานปิดกะและการสื่อสารทั้งหมด กรองตามสาขา และเรียงจากล่าสุดไปหาเก่าสุด
        /// </summary>
        public static List<AccountingFeedback> GetAll(string? branch = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    if (!string.IsNullOrEmpty(branch))
                    {
                        list = list.Where(f => f.Branch == branch).ToList();
                    }
                    return list.OrderByDescending(f => f.CreatedAt).ToList();
                }
                catch
                {
                    return new List<AccountingFeedback>();
                }
            }
        }

        /// <summary>
        /// เพิ่มรายการบันทึกส่งยอดเงินหรือข้อความสื่อสารใหม่จากพนักงานต้อนรับหน้าร้าน
        /// </summary>
        public static void Add(AccountingFeedback feedback)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    list.Insert(0, feedback);
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                }
                catch
                {
                    // Fail-safe
                }
            }
        }

        /// <summary>
        /// เจ้าหน้าที่ฝ่ายบัญชีกดยืนยันการรับทราบยอดเงินและข้อความ
        /// </summary>
        public static bool Acknowledge(string id, string accountingUserName)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<AccountingFeedback>>(json) ?? new List<AccountingFeedback>();
                    var item = list.FirstOrDefault(f => f.Id == id);
                    if (item != null)
                    {
                        item.IsAcknowledged = true;
                        item.AcknowledgedBy = accountingUserName;
                        item.AcknowledgedAt = DateTime.Now;
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_filePath, updatedJson);
                        return true;
                    }
                }
                catch
                {
                }
                return false;
            }
        }
    }
}
