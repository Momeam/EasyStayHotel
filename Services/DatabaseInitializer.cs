using System;
using System.Linq;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// เซอร์วิส DatabaseInitializer: สำหรับเตรียมข้อมูลเริ่มต้นและ Seed Data ของฐานข้อมูล (Database Seeder)
    /// หน้าที่:
    /// 1. ตรวจสอบและสร้างห้องพักจำลองสำหรับทุกสาขา (Bangkok, Chonburi, Phuket)
    /// 2. ตรวจสอบและสร้างบัญชีผู้ใช้งานทดสอบสำหรับทุก Role (Admin, Staff ทุกสาขา, Accounting, Housekeeper)
    /// 3. จัดเตรียมรายการจองตัวอย่างเพื่อความพร้อมในการสาธิต (Demo-ready)
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// ตรวจสอบและเตรียมห้องพักสถานะว่าง (Available) ให้แก่ทุกสาขาของโรงแรม
        /// หากสาขาใดไม่มีห้องพัก จะสร้างห้องพักมาตรฐาน (Standard, Deluxe, Suite) ให้อัตโนมัติ
        /// </summary>
        public static void EnsureBranchRooms(EasystaydbContext context)
        {
            try
            {
                var branches = new[] { "Chonburi", "Phuket" };
                bool changed = false;

                foreach (var branch in branches)
                {
                    // นับจำนวนห้องที่ว่างพร้อมขายในสาขานั้น
                    int availableCount = context.Rooms.Count(r => r.Branch == branch && r.Status == "Available");
                    if (availableCount == 0)
                    {
                        var existingRooms = context.Rooms.Where(r => r.Branch == branch).ToList();
                        if (existingRooms.Any())
                        {
                            // หากมีห้องพักอยู่แล้วแต่สถานะไม่ใช่ Available ให้ปรับเป็น Available
                            foreach (var room in existingRooms)
                            {
                                room.Status = "Available";
                            }
                            changed = true;
                        }
                        else
                        {
                            // หากยังไม่มีห้องพักในสาขานั้นเลย ให้สร้างห้องพักใหม่ประเภทละ 3 ห้อง
                            var types = context.Roomtypes.Where(rt => rt.RoomTypeId >= 1 && rt.RoomTypeId <= 3).ToList();
                            if (types.Any())
                            {
                                foreach (var type in types)
                                {
                                    string prefix = type.RoomTypeId switch
                                    {
                                        1 => "10",
                                        2 => "20",
                                        3 => "30",
                                        _ => "40"
                                    };

                                    for (int i = 1; i <= 3; i++)
                                    {
                                        context.Rooms.Add(new Room
                                        {
                                            RoomNo = $"{prefix}{i}",
                                            RoomTypeId = type.RoomTypeId,
                                            Status = "Available",
                                            Branch = branch
                                        });
                                    }
                                }
                                changed = true;
                            }
                        }
                    }
                }

                if (changed)
                {
                    context.SaveChanges();
                }
            }
            catch
            {
                // กลไก Fail-safe: หากฐานข้อมูลยังไม่พร้อมตอนรันระบบ จะไม่ทำให้แอปพลิเคชันพัง (Crash)
            }
        }

        /// <summary>
        /// ตรวจสอบและสร้างบัญชีผู้ใช้งานระบบเริ่มต้น (Default User Accounts) สำหรับทุกบทบาท
        /// </summary>
        public static void EnsureHousekeeperUser(EasystaydbContext context)
        {
            try
            {
                // 1. ตรวจสอบและสร้างบัญชีผู้ดูแลระบบ (Admin)
                bool adminExists = context.Users.Any(u => u.Email == "admin@easystay.com");
                if (!adminExists)
                {
                    context.Users.Add(new User
                    {
                        FullName = "ผู้จัดการฝ่ายบริหาร (Admin)",
                        Email = "admin@easystay.com",
                        Phone = "02-123-4500",
                        Password = "1234",
                        Role = "Admin",
                        Branch = "Bangkok",
                        DateOfBirth = new DateOnly(1985, 1, 1)
                    });
                }

                // 2. ตรวจสอบและสร้างบัญชีแม่บ้านประจำโรงแรม (Housekeeper)
                bool housekeeperExists = context.Users.Any(u => u.Role == "Housekeeper" || u.Role == "Maid" || u.Email == "housekeeper@easystay.com");
                if (!housekeeperExists)
                {
                    var housekeeperUser = new User
                    {
                        FullName = "คุณสมศรี แม่บ้านประจำ",
                        Email = "housekeeper@easystay.com",
                        Phone = "0891234567",
                        Password = "1234",
                        Role = "Housekeeper",
                        Branch = "Bangkok",
                        DateOfBirth = new DateOnly(1988, 5, 15)
                    };
                    context.Users.Add(housekeeperUser);
                }

                // 3. ตรวจสอบและสร้างบัญชีฝ่ายบัญชีและการเงิน (Accounting)
                bool accUserExists = context.Users.Any(u => u.Role == "Accounting" || u.Email == "account@easystay.com");
                if (!accUserExists)
                {
                    context.Users.Add(new User
                    {
                        FullName = "พนักงานฝ่ายบัญชีและการเงิน",
                        Email = "account@easystay.com",
                        Phone = "02-123-4568",
                        Password = "1234",
                        Role = "Accounting",
                        Branch = "Bangkok",
                        DateOfBirth = new DateOnly(1992, 8, 10)
                    });
                }

                // 4. ตรวจสอบและสร้างบัญชีพนักงานต้อนรับประจำแต่ละสาขา (Front Desk Staff)
                var staffAccounts = new[]
                {
                    new { Email = "staff_puket@easystay.com", Name = "พนักงานต้อนรับ ภูเก็ต (001)", Branch = "Phuket", Phone = "076-123-456" },
                    new { Email = "staff_phuket@easystay.com", Name = "พนักงานต้อนรับ ภูเก็ต (002)", Branch = "Phuket", Phone = "076-123-457" },
                    new { Email = "staff_chonburi@easystay.com", Name = "พนักงานต้อนรับ ชลบุรี-พัทยา", Branch = "Chonburi", Phone = "038-123-456" },
                    new { Email = "staff_bangkok@easystay.com", Name = "พนักงานต้อนรับ กรุงเทพฯ", Branch = "Bangkok", Phone = "02-123-4567" },
                    new { Email = "staff@easystay.com", Name = "พนักงานต้อนรับ 001", Branch = "Bangkok", Phone = "081-234-5678" }
                };

                foreach (var acc in staffAccounts)
                {
                    var existingStaff = context.Users.FirstOrDefault(u => u.Email.ToLower() == acc.Email.ToLower());
                    if (existingStaff == null)
                    {
                        context.Users.Add(new User
                        {
                            FullName = acc.Name,
                            Email = acc.Email,
                            Phone = acc.Phone,
                            Password = "1234",
                            Role = "Staff",
                            Branch = acc.Branch,
                            DateOfBirth = new DateOnly(1995, 3, 20)
                        });
                    }
                    else
                    {
                        // อัปเดตข้อมูลสาขาและสิทธิ์ให้ถูกต้องเสมอ
                        existingStaff.Branch = acc.Branch;
                        existingStaff.Role = "Staff";
                        existingStaff.FullName = acc.Name;
                    }
                }

                context.SaveChanges();

                // 5. เตรียมข้อมูลการจองตัวอย่างสำหรับสาขาภูเก็ตและชลบุรี เพื่อให้พร้อมทดสอบระบบหน้าร้านทันที
                EnsureSampleBranchBookings(context);
            }
            catch
            {
                // Fallback
            }
        }

        /// <summary>
        /// เตรียมข้อมูลการจองตัวอย่าง (Sample Bookings) เช่น แขกพักอยู่จริง (In-House) และแขกกำลังจะเข้าพัก (Arrivals)
        /// </summary>
        private static void EnsureSampleBranchBookings(EasystaydbContext context)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var testBranches = new[] { "Phuket", "Chonburi" };

                foreach (var branch in testBranches)
                {
                    bool hasBookings = context.Bookings.Any(b => b.Room != null && b.Room.Branch == branch);
                    if (!hasBookings)
                    {
                        var branchRooms = context.Rooms.Where(r => r.Branch == branch).ToList();
                        if (branchRooms.Count >= 2)
                        {
                            // 1. แขกที่พักอยู่ในโรงแรมปัจจุบัน (In-House) พร้อมให้ทดสอบการเช็คเอาท์
                            var room1 = branchRooms[0];
                            room1.Status = "Occupied";
                            var inHouseBooking = new Booking
                            {
                                BookingRef = $"BK-{branch.Substring(0, 2).ToUpper()}-{DateTime.Now:yyyyMMdd}01",
                                RoomId = room1.RoomId,
                                CheckInDate = today.AddDays(-1),
                                CheckOutDate = today,
                                TotalPrice = 2800,
                                BookingStatus = "CheckIn",
                                GuestName = branch == "Phuket" ? "คุณณัฐพล เที่ยวทะเลใต้" : "คุณพงศกร เที่ยวพัทยาชลบุรี"
                            };
                            inHouseBooking.Payments.Add(new Payment
                            {
                                Amount = 2800,
                                PaymentStatus = "Paid",
                                TransferDate = DateTime.Now.AddDays(-1)
                            });
                            context.Bookings.Add(inHouseBooking);

                            // 2. แขกที่จะเช็คอินในวันนี้ (Arrival) พร้อมให้ทดสอบการรับเช็คอิน
                            var room2 = branchRooms[1];
                            room2.Status = "Available";
                            var arrivalBooking = new Booking
                            {
                                BookingRef = $"BK-{branch.Substring(0, 2).ToUpper()}-{DateTime.Now:yyyyMMdd}02",
                                RoomId = room2.RoomId,
                                CheckInDate = today,
                                CheckOutDate = today.AddDays(2),
                                TotalPrice = 3600,
                                BookingStatus = "Confirmed",
                                GuestName = branch == "Phuket" ? "คุณกานดา หาดป่าตอง" : "คุณวรัญญา บางแสนสายลม"
                            };
                            arrivalBooking.Payments.Add(new Payment
                            {
                                Amount = 3600,
                                PaymentStatus = "Paid",
                                TransferDate = DateTime.Now
                            });
                            context.Bookings.Add(arrivalBooking);

                            context.SaveChanges();
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
        }
    }
}
