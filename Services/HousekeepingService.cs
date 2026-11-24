// นำเข้า System สำหรับการใช้งานคลาสพื้นฐาน เช่น DateTime, Guid, String
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List
using System.Collections.Generic;
// นำเข้า System.IO สำหรับการอ่านและเขียนไฟล์ JSON
using System.IO;
// นำเข้า System.Linq สำหรับฟังก์ชันการค้นหาและกรองข้อมูล เช่น Where, FirstOrDefault, Any, OrderBy
using System.Linq;
// นำเข้า System.Text.Json สำหรับ Serialization และ Deserialization ข้อมูล JSON
using System.Text.Json;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก HousekeepingService สำหรับจัดการคิวงานทำความสะอาดของแม่บ้าน และซิงค์ข้อมูลกับสถานะห้องพัก
    public static class HousekeepingService
    {
        // อ็อบเจกต์ล็อกเธรดเพื่อความปลอดภัยในการอ่าน-เขียนไฟล์ JSON พร้อมกัน
        private static readonly object _lock = new object();
        // พาธโฟลเดอร์สำหรับเก็บไฟล์ข้อมูล (wwwroot/data)
        private static readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
        // พาธไฟล์ JSON สำหรับบันทึกงานแม่บ้าน (wwwroot/data/housekeeping_tasks.json)
        private static readonly string _filePath = Path.Combine(_dataDir, "housekeeping_tasks.json");

        // เมธอดส่วนตัวสำหรับตรวจสอบความมีอยู่ของไฟล์และสร้างข้อมูลตั้งต้น (Seed Data)
        private static void EnsureFileExists()
        {
            // ตรวจสอบว่าโฟลเดอร์ data มีอยู่แล้วหรือไม่
            if (!Directory.Exists(_dataDir))
            {
                // หากยังไม่มี ให้สร้างไดเรกทอรี wwwroot/data
                Directory.CreateDirectory(_dataDir);
            }

            // ตรวจสอบว่าไฟล์ housekeeping_tasks.json มีอยู่แล้วหรือไม่
            if (!File.Exists(_filePath))
            {
                // กำหนดข้อมูลงานทำความสะอาดจำลองเริ่มต้น เพื่อให้ระบบมีข้อมูลทดสอบพร้อมใช้
                var initialList = new List<HousekeepingTask>
                {
                    // งานที่ 1: สถานะรอดำเนินการ (Pending) ความสำคัญด่วน (Urgent)
                    new HousekeepingTask
                    {
                        // กำหนดรหัสงานด้วยรูปแบบ HK-วันที่-เลขห้อง
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0101",
                        // รหัสห้องพัก
                        RoomId = 1,
                        // หมายเลขห้อง
                        RoomNo = "101",
                        // ชื่อประเภทห้องพัก
                        RoomTypeName = "Deluxe King",
                        // สาขา
                        Branch = "Bangkok",
                        // ชั้นของห้อง
                        Floor = 1,
                        // สถานะงานทำความสะอาด
                        Status = "Pending",
                        // ระดับความสำคัญ
                        Priority = "Urgent",
                        // ที่มาของคำขอ
                        Source = "CheckOut",
                        // ผู้ส่งคำขอ
                        RequestedBy = "พนักงานต้อนรับ 001",
                        // วันเวลาที่ส่งคำขอ
                        RequestedAt = DateTime.Now.AddMinutes(-45),
                        // ชื่อแขกที่เพิ่งเช็คเอาท์
                        GuestName = "คุณสมชาย ใจดี",
                        // หมายเหตุประกอบงาน
                        Notes = "ลูกค้าเช็คเอาท์รอบเที่ยง มีแขกใหม่จองเข้าพักรอบ 14:00 น. รบกวนทำความสะอาดด่วนครับ",
                        // รายการเช็คลิสต์เริ่มต้น
                        Checklist = new List<string>()
                    },
                    // งานที่ 2: สถานะกำลังดำเนินการ (InProgress)
                    new HousekeepingTask
                    {
                        // รหัสงาน
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0102",
                        // รหัสห้อง
                        RoomId = 2,
                        // หมายเลขห้อง
                        RoomNo = "102",
                        // ประเภทห้อง
                        RoomTypeName = "Standard Twin",
                        // สาขา
                        Branch = "Bangkok",
                        // ชั้น
                        Floor = 1,
                        // สถานะงาน
                        Status = "InProgress",
                        // ความสำคัญปกติ
                        Priority = "Normal",
                        // ที่มาคำขอ
                        Source = "CheckOut",
                        // ผู้ส่งคำขอ
                        RequestedBy = "พนักงานต้อนรับ 002",
                        // เวลาส่งคำขอ
                        RequestedAt = DateTime.Now.AddHours(-1),
                        // ชื่อแขก
                        GuestName = "คุณวิภาวรรณ สดใส",
                        // หมายเหตุ
                        Notes = "ลูกค้าเช็คเอาท์แล้ว รบกวนเติมชุดชา/กาแฟ และเปลี่ยนผ้าปูที่นอน",
                        // ชื่อแม่บ้านผู้รับงาน
                        HousekeeperName = "ป้าสมศรี แม่บ้าน",
                        // เวลาที่เริ่มทำความสะอาด
                        StartedAt = DateTime.Now.AddMinutes(-20),
                        // รายการเช็คลิสต์ที่ทำแล้ว
                        Checklist = new List<string> { "เปลี่ยนผ้าปูที่นอนและปลอกหมอน", "ทำความสะอาดห้องน้ำและสุขภัณฑ์" }
                    },
                    // งานที่ 3: สถานะทำความสะอาดเสร็จแล้ว (Completed)
                    new HousekeepingTask
                    {
                        // รหัสงาน
                        Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-0201",
                        // รหัสห้อง
                        RoomId = 3,
                        // หมายเลขห้อง
                        RoomNo = "201",
                        // ประเภทห้อง
                        RoomTypeName = "Suite Ocean",
                        // สาขา
                        Branch = "Bangkok",
                        // ชั้น
                        Floor = 2,
                        // สถานะ
                        Status = "Completed",
                        // ความสำคัญ
                        Priority = "Normal",
                        // ที่มาคำขอ
                        Source = "FrontDesk",
                        // ผู้ส่งคำขอ
                        RequestedBy = "พนักงานต้อนรับ 001",
                        // เวลาส่งคำขอ
                        RequestedAt = DateTime.Now.AddHours(-3),
                        // ชื่อแขก
                        GuestName = "คุณอนันต์ แสงทอง",
                        // หมายเหตุ
                        Notes = "ทำความสะอาดประจำวัน ตรวจเช็คกลิ่นอับ",
                        // ชื่อแม่บ้าน
                        HousekeeperName = "ป้าสมศรี แม่บ้าน",
                        // เวลาเริ่มงาน
                        StartedAt = DateTime.Now.AddHours(-2).AddMinutes(-30),
                        // เวลาเสร็จงาน
                        CompletedAt = DateTime.Now.AddHours(-1).AddMinutes(-40),
                        // หมายเหตุหลังทำความสะอาดเสร็จ
                        CompletionNotes = "ทำความสะอาดเรียบร้อย เปลี่ยนผ้าปู จัดห้องน้ำใหม่ เติม Amenities ครบถ้วน แอร์เย็นปกติ ห้องพร้อมขายต่อแล้วค่ะ",
                        // เช็คลิสต์ตรวจสอบครบ 5 ข้อ
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

                // แปลง List เป็น JSON สตริงแบบ Indented
                var json = JsonSerializer.Serialize(initialList, new JsonSerializerOptions { WriteIndented = true });
                // บันทึกไฟล์ JSON
                File.WriteAllText(_filePath, json);
            }
        }

        // เมธอดดึงรายการงานแม่บ้านทั้งหมด พร้อมตัวกรองสาขาและสถานะ
        public static List<HousekeepingTask> GetAllTasks(string? branch = null, string? status = null)
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
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    // กรองตามสาขาหากมีการระบุ
                    if (!string.IsNullOrEmpty(branch))
                    {
                        list = list.Where(t => t.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    // กรองตามสถานะหากไม่ใช่ ALL
                    if (!string.IsNullOrEmpty(status) && status != "ALL")
                    {
                        list = list.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    // เรียงลำดับงานด่วน (Urgent) ขึ้นก่อน แล้วเรียงตามเวลาที่ขอจากใหม่ไปเก่า
                    return list.OrderByDescending(t => t.Priority == "Urgent")
                               .ThenByDescending(t => t.RequestedAt)
                               .ToList();
                }
                catch
                {
                    // เกิดข้อผิดพลาดส่งกลับ List ว่าง
                    return new List<HousekeepingTask>();
                }
            }
        }

        // เมธอดค้นหางานตามรหัส Task ID
        public static HousekeepingTask? GetTaskById(string id)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่าน JSON
                    var json = File.ReadAllText(_filePath);
                    // แปลง JSON เป็น List
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();
                    // ค้นหางานตามรหัส ID
                    return list.FirstOrDefault(t => t.Id == id);
                }
                catch
                {
                    // หากเกิดข้อผิดพลาด คืนค่า null
                    return null;
                }
            }
        }

        // เมธอดสำหรับสร้างงานทำความสะอาดใหม่ลงในระบบ
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
            // ล็อกเธรดเพื่อป้องกันการเขียนทับซ้อนกัน
            lock (_lock)
            {
                // ตรวจสอบไฟล์
                EnsureFileExists();
                // อ่านข้อมูล JSON เดิม
                var json = File.ReadAllText(_filePath);
                // แปลงเป็น List
                var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                // ตรวจสอบว่ามีงานค้างของห้องนี้ในสาขานี้ที่ยังทำไม่เสร็จ (ไม่ Completed) อยู่แล้วหรือไม่
                var existingTask = list.FirstOrDefault(t => t.RoomId == roomId && t.Branch == branch && t.Status != "Completed");
                // หากมีงานค้างอยู่แล้ว ให้อัปเดตข้อมูลงานเดิมแทนการสร้างซ้ำ
                if (existingTask != null)
                {
                    // ปรับระดับความสำคัญใหม่
                    existingTask.Priority = priority;
                    // รวมหมายเหตุเพิ่มเติม
                    if (!string.IsNullOrEmpty(notes))
                    {
                        existingTask.Notes = (existingTask.Notes != null ? existingTask.Notes + " | " : "") + notes;
                    }
                    // อัปเดตชื่อผู้ขอ
                    existingTask.RequestedBy = requestedBy;
                    // อัปเดตเวลาที่ขอเป็นปัจจุบัน
                    existingTask.RequestedAt = DateTime.Now;

                    // แปลงเป็น JSON แล้วบันทึก
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่างานที่อัปเดตแล้ว
                    return existingTask;
                }

                // คำนวณชั้นห้องพักจากเลขตัวแรกของเลขห้อง (เช่น 101 -> ชั้น 1, 201 -> ชั้น 2)
                int floor = 1;
                if (!string.IsNullOrEmpty(roomNo) && char.IsDigit(roomNo[0]))
                {
                    floor = int.Parse(roomNo[0].ToString());
                }

                // สร้างอินสแตนซ์งานใหม่
                var newTask = new HousekeepingTask
                {
                    // เจนรหัสงานอัตโนมัติด้วย HK-วันที่-ตัวอักษร 4 หลัก
                    Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                    // รหัสห้องพัก
                    RoomId = roomId,
                    // หมายเลขห้องพัก
                    RoomNo = roomNo,
                    // ประเภทห้องพัก
                    RoomTypeName = roomTypeName,
                    // สาขา
                    Branch = branch,
                    // ชั้น
                    Floor = floor,
                    // สถานะเริ่มต้นเป็น Pending
                    Status = "Pending",
                    // ระดับความสำคัญ
                    Priority = priority,
                    // แหล่งที่มา
                    Source = source,
                    // ผู้ขอ
                    RequestedBy = requestedBy,
                    // เวลาที่ขอ
                    RequestedAt = DateTime.Now,
                    // ชื่อแขก
                    GuestName = guestName,
                    // หมายเหตุ
                    Notes = notes,
                    // รายการเช็คลิสต์เริ่มต้น
                    Checklist = new List<string>()
                };

                // แทรกงานใหม่ไว้ที่ตำแหน่งบนสุดของรายการ
                list.Insert(0, newTask);
                // แปลงข้อมูลเป็น JSON
                var saveJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                // บันทึกไฟล์ JSON
                File.WriteAllText(_filePath, saveJson);

                // ส่งคืนงานที่สร้างใหม่
                return newTask;
            }
        }

        // เมธอดเมื่อแม่บ้านกดเริ่มงานทำความสะอาด (Start Cleaning)
        public static bool StartCleaning(string taskId, string housekeeperName)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่านไฟล์ JSON
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    // ค้นหางานตาม Task ID
                    var task = list.FirstOrDefault(t => t.Id == taskId);
                    // หากไม่พบงาน คืนค่า false
                    if (task == null) return false;

                    // ปรับสถานะเป็น InProgress (กำลังทำความสะอาด)
                    task.Status = "InProgress";
                    // บันทึกชื่อแม่บ้านผู้รับผิดชอบ
                    task.HousekeeperName = housekeeperName;
                    // บันทึกเวลาที่เริ่มงาน
                    task.StartedAt = DateTime.Now;

                    // แปลงเป็น JSON แล้วบันทึก
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true สำเร็จ
                    return true;
                }
                catch
                {
                    // เกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }

        // เมธอดเมื่อแม่บ้านตรวจเช็คลิสต์เสร็จสิ้นและกดยืนยันส่งงาน (Complete Cleaning)
        public static bool CompleteCleaning(
            string taskId,
            string housekeeperName,
            string? completionNotes = null,
            List<string>? checklist = null)
        {
            // ล็อกเธรดเพื่อความปลอดภัย
            lock (_lock)
            {
                try
                {
                    // ตรวจสอบไฟล์
                    EnsureFileExists();
                    // อ่านไฟล์ JSON
                    var json = File.ReadAllText(_filePath);
                    // แปลงเป็น List
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    // ค้นหางานตาม Task ID
                    var task = list.FirstOrDefault(t => t.Id == taskId);
                    // หากไม่พบงาน คืนค่า false
                    if (task == null) return false;

                    // ปรับสถานะงานเป็น Completed (ทำความสะอาดเสร็จสิ้น)
                    task.Status = "Completed";
                    // บันทึกชื่อแม่บ้านผู้ส่งงาน
                    task.HousekeeperName = string.IsNullOrWhiteSpace(housekeeperName) ? (task.HousekeeperName ?? "แม่บ้าน") : housekeeperName;
                    // บันทึกวันเวลาที่ส่งงานสำเร็จ
                    task.CompletedAt = DateTime.Now;
                    // บันทึกหมายเหตุการส่งงาน (หากไม่ได้กรอก ให้ใส่ข้อความมาตรฐาน)
                    task.CompletionNotes = string.IsNullOrWhiteSpace(completionNotes)
                        ? "ทำความสะอาดเรียบร้อย ตรวจเช็คอุปกรณ์และสุขภัณฑ์ครบถ้วน พร้อมส่งมอบห้องให้นำไปขายต่อ"
                        : completionNotes;

                    // ตรวจสอบเช็คลิสต์ที่ส่งมา
                    if (checklist != null && checklist.Any())
                    {
                        // บันทึกเช็คลิสต์ที่ผู้ใช้ติ๊กเลือก
                        task.Checklist = checklist;
                    }
                    // หากไม่มีการส่งเช็คลิสต์มา ให้กำหนดเช็คลิสต์มาตรฐาน 4 ข้อ
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

                    // แปลงข้อมูลเป็น JSON
                    var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    // บันทึกไฟล์
                    File.WriteAllText(_filePath, updatedJson);
                    // คืนค่า true สำเร็จ
                    return true;
                }
                catch
                {
                    // เกิดข้อผิดพลาด คืนค่า false
                    return false;
                }
            }
        }

        // เมธอดสำหรับซิงค์ห้องพักสถานะ Cleaning ใน MySQL เข้าสู่ระบบงานทำความสะอาดอัตโนมัติ
        public static void SyncWithRooms(List<Room> rooms, string branch, string staffName = "ระบบอัตโนมัติ")
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
                    var list = JsonSerializer.Deserialize<List<HousekeepingTask>>(json) ?? new List<HousekeepingTask>();

                    // ตัวแปรติดตามว่ามีการเพิ่มงานใหม่หรือไม่
                    bool hasChanges = false;
                    // วนลูปตรวจสอบทุกห้องพักในสาขานั้นที่มีสถานะเป็น Cleaning
                    foreach (var room in rooms.Where(r => r.Status == "Cleaning" && r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase)))
                    {
                        // ตรวจสอบว่ามีงานค้างของห้องนี้ในระบบที่ยังไม่ Completed หรือยัง
                        bool exists = list.Any(t => t.RoomId == room.RoomId && t.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase) && t.Status != "Completed");
                        // หากยังไม่มีงานค้าง ให้สร้างงานทำความสะอาดใหม่ขึ้นมารองรับทันที
                        if (!exists)
                        {
                            // คำนวณชั้นจากเลขห้อง
                            int floor = 1;
                            if (!string.IsNullOrEmpty(room.RoomNo) && char.IsDigit(room.RoomNo[0]))
                            {
                                floor = int.Parse(room.RoomNo[0].ToString());
                            }

                            // สร้าง Task ใหม่
                            var newTask = new HousekeepingTask
                            {
                                // รหัสงาน
                                Id = "HK-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                                // รหัสห้อง
                                RoomId = room.RoomId,
                                // หมายเลขห้อง
                                RoomNo = room.RoomNo,
                                // ชื่อประเภทห้อง
                                RoomTypeName = room.RoomType?.TypeName ?? "Standard",
                                // สาขา
                                Branch = branch,
                                // ชั้น
                                Floor = floor,
                                // สถานะ
                                Status = "Pending",
                                // ความสำคัญ
                                Priority = "Normal",
                                // แหล่งที่มา
                                Source = "FrontDesk",
                                // ผู้ส่ง
                                RequestedBy = staffName,
                                // เวลา
                                RequestedAt = DateTime.Now,
                                // หมายเหตุ
                                Notes = "สถานะห้องพักถูกตั้งเป็น 'รอทำความสะอาด' กรุณาเข้าตรวจและทำความสะอาดเพื่อเตรียมห้องขายต่อ",
                                // เช็คลิสต์
                                Checklist = new List<string>()
                            };
                            // ใส่ไว้บนสุด
                            list.Insert(0, newTask);
                            // มีการเปลี่ยนแปลงข้อมูล
                            hasChanges = true;
                        }
                    }

                    // หากมีการเปลี่ยนแปลงให้บันทึกไฟล์
                    if (hasChanges)
                    {
                        var updatedJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(_filePath, updatedJson);
                    }
                }
                catch
                {
                    // กรณีเกิดข้อผิดพลาด ให้ทำงานต่อไปโดยไม่ให้ระบบพัง
                }
            }
        }
    }
}
