using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    public static class HousekeepingService
    {
        private static readonly object _lock = new object();
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        private static readonly string _filePath = Path.Combine(_dataDir, "housekeeping_tasks.json");

        private static void EnsureFileExists()
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            if (!File.Exists(_filePath))
            {
                var initialList = new List<HousekeepingTask>
                {
                    new HousekeepingTask
                    {
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0101",
                        RoomId = 1,
                        RoomNo = "101",
                        RoomTypeName = "Deluxe King",
                        Branch = "Bangkok",
                        Floor = 1,
                        Status = "Pending",
                        Priority = "Urgent",
                        Source = "CheckOut",
                        RequestedBy = "พนักงานต้อนรับ 001",
                        RequestedAt = DateTime.Now.AddMinutes(-45),
                        GuestName = "คุณสมชาย ใจดี",
                        Notes = "ลูกค้าเช็คเอาท์รอบเที่ยง มีแขกใหม่จองเข้าพักรอบ 14:00 น. รบกวนทำความสะอาดด่วนครับ",
                        Checklist = new List<string>()
                    },
                    new HousekeepingTask
                    {
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0102",
                        RoomId = 2,
                        RoomNo = "102",
                        RoomTypeName = "Standard Twin",
                        Branch = "Bangkok",
                        Floor = 1,
                        Status = "InProgress",
                        Priority = "Normal",
                        Source = "CheckOut",
                        RequestedBy = "พนักงานต้อนรับ 002",
                        RequestedAt = DateTime.Now.AddHours(-1),
                        GuestName = "คุณวิภาวรรณ สดใส",
                        Notes = "ลูกค้าเช็คเอาท์แล้ว รบกวนเติมชุดชา/กาแฟ และเปลี่ยนผ้าปูที่นอน",
                        HousekeeperName = "ป้าสมศรี แม่บ้าน",
                        StartedAt = DateTime.Now.AddMinutes(-20),
                        Checklist = new List<string> { "เปลี่ยนผ้าปูที่นอนและปลอกหมอน", "ทำความสะอาดห้องน้ำและสุขภัณฑ์" }
                    },
                    new HousekeepingTask
                    {
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0201",
                        RoomId = 3,
                        RoomNo = "201",
                        RoomTypeName = "Suite Ocean",
                        Branch = "Bangkok",
                        Floor = 2,
                        Status = "Completed",
                        Priority = "Normal",
                        Source = "FrontDesk",
                        RequestedBy = "พนักงานต้อนรับ 001",
                        RequestedAt = DateTime.Now.AddHours(-3),
                        GuestName = "คุณอนันต์ แสงทอง",
                        Notes = "ทำความสะอาดประจำวัน ตรวจเช็คกลิ่นอับ",
                        HousekeeperName = "ป้าสมศรี แม่บ้าน",
                        StartedAt = DateTime.Now.AddHours(-2).AddMinutes(-30),
                        CompletedAt = DateTime.Now.AddHours(-1).AddMinutes(-40),
                        CompletionNotes = "ทำความสะอาดเรียบร้อย เปลี่ยนผ้าปู จัดห้องน้ำใหม่ เติม Amenities ครบถ้วน แอร์เย็นปกติ ห้องพร้อมขายต่อแล้วค่ะ",
                        Checklist = new List<string>
                        {
                            "เปลี่ยนผ้าปูที่นอนและปลอกหมอน",
                            "ทำความสะอาดห้องน้ำและสุขภัณฑ์",
                            "เติมของใช้ Amenities ครบชุด",
                            "ดูดฝุ่นและถูพื้นฆ่าเชื้อ",
                            "ตรวจสอบระบบไฟและเครื่องปรับอากาศ"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public static List<HousekeepingTask> GetAllTasks(string? branch = null, string? status = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    if (!string.IsNullOrEmpty(branch))
                    {
                        list = list.Where(t => t.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (!string.IsNullOrEmpty(status) && status != "ALL")
                    {
                        list = list.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    return list.OrderByDescending(t => t.Priority == "Urgent")
                               .ThenByDescending(t => t.RequestedAt)
                               .ToList();
                }
                catch
                {
                    return new List<HousekeepingTask>();
                }
            }
        }

        public static HousekeepingTask? GetTaskById(string id)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();
                    return list.FirstOrDefault(t => t.Id == id);
                }
                catch
                {
                    return null;
                }
            }
        }

        public static HousekeepingTask CreateTask(
            int roomId,
            string roomNo,
            string roomTypeName,
            string branch,
            string requestedBy,
            string priority = "Normal",
            string? notes = null,
            string? guestName = null,
            string source = "FrontDesk")
        {
            lock (_lock)
            {
                EnsureFileExists();
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                var existingTask = list.FirstOrDefault(t => t.RoomId == roomId && t.Branch == branch && t.Status != "Completed");
                if (existingTask != null)
                {
                    existingTask.Priority = priority;
                    if (!string.IsNullOrEmpty(notes))
                    {
                        existingTask.Notes = (existingTask.Notes != null ? existingTask.Notes + " | " : "") + notes;
                    }
                    existingTask.RequestedBy = requestedBy;
                    existingTask.RequestedAt = DateTime.Now;

                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    return existingTask;
                }

                int floor = 1;
                if (!string.IsNullOrEmpty(roomNo) && char.IsDigit(roomNo[0]))
                {
                    floor = int.Parse(roomNo[0].ToString());
                }

                var newTask = new HousekeepingTask
                {
                    Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                    RoomId = roomId,
                    RoomNo = roomNo,
                    RoomTypeName = roomTypeName,
                    Branch = branch,
                    Floor = floor,
                    Status = "Pending",
                    Priority = priority,
                    Source = source,
                    RequestedBy = requestedBy,
                    RequestedAt = DateTime.Now,
                    GuestName = guestName,
                    Notes = notes,
                    Checklist = new List<string>()
                };

                list.Insert(0, newTask);
                var saveJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, saveJson);

                return newTask;
            }
        }

        public static bool StartCleaning(string taskId, string housekeeperName)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    var task = list.FirstOrDefault(t => t.Id == taskId);
                    if (task == null) return false;

                    task.Status = "InProgress";
                    task.HousekeeperName = housekeeperName;
                    task.StartedAt = DateTime.Now;

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

        public static bool CompleteCleaning(
            string taskId,
            string housekeeperName,
            string? completionNotes = null,
            List<string>? checklist = null)
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    var task = list.FirstOrDefault(t => t.Id == taskId);
                    if (task == null) return false;

                    task.Status = "Completed";
                    task.HousekeeperName = string.IsNullOrWhiteSpace(housekeeperName) ? (task.HousekeeperName ?? "แม่บ้าน") : housekeeperName;
                    task.CompletedAt = DateTime.Now;
                    task.CompletionNotes = string.IsNullOrWhiteSpace(completionNotes)
                        ? "ทำความสะอาดเรียบร้อย ตรวจเช็คอุปกรณ์และสุขภัณฑ์ครบถ้วน พร้อมส่งมอบห้องให้นำไปขายต่อ"
                        : completionNotes;

                    if (checklist != null && checklist.Any())
                    {
                        task.Checklist = checklist;
                    }
                    else if (task.Checklist == null || !task.Checklist.Any())
                    {
                        task.Checklist = new List<string>
                        {
                            "เปลี่ยนผ้าปูที่นอนและปลอกหมอน",
                            "ทำความสะอาดห้องน้ำและสุขภัณฑ์",
                            "เติมของใช้ Amenities ครบชุด",
                            "ตรวจสอบระบบไฟและเครื่องปรับอากาศ"
                        };
                    }

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

        public static void SyncWithRooms(List<Room> rooms, string branch, string staffName = "ระบบอัตโนมัติ")
        {
            lock (_lock)
            {
                try
                {
                    EnsureFileExists();
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    bool hasChanges = false;
                    foreach (var room in rooms.Where(r => r.Status == "Cleaning" && r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)))
                    {
                        bool exists = list.Any(t => t.RoomId == room.RoomId && t.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase) && t.Status != "Completed");
                        if (!exists)
                        {
                            int floor = 1;
                            if (!string.IsNullOrEmpty(room.RoomNo) && char.IsDigit(room.RoomNo[0]))
                            {
                                floor = int.Parse(room.RoomNo[0].ToString());
                            }

                            var newTask = new HousekeepingTask
                            {
                                Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                                RoomId = room.RoomId,
                                RoomNo = room.RoomNo,
                                RoomTypeName = room.RoomType?.TypeName ?? "Standard",
                                Branch = branch,
                                Floor = floor,
                                Status = "Pending",
                                Priority = "Normal",
                                Source = "FrontDesk",
                                RequestedBy = staffName,
                                RequestedAt = DateTime.Now,
                                Notes = "สถานะห้องพักถูกตั้งเป็น 'รอทำความสะอาด' กรุณาเข้าตรวจและทำความสะอาดเพื่อเตรียมห้องขายต่อ",
                                Checklist = new List<string>()
                            };
                            list.Insert(0, newTask);
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_filePath, updatedJson);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
