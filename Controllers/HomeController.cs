using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyStayHotel.Models;

namespace EasyStayHotel.Controllers;

public class HomeController : Controller
{
    private readonly EasystaydbContext _context;

    public HomeController(EasystaydbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        List<Roomtype> roomTypes;
        try
        {
            roomTypes = _context.Roomtypes
                .Include(rt => rt.Rooms)
                .OrderBy(rt => rt.BasePrice)
                .ToList();
        }
        catch
        {
            roomTypes = new List<Roomtype>();
        }

        if (!roomTypes.Any())
        {
            roomTypes = new List<Roomtype>
            {
                new Roomtype
                {
                    RoomTypeId = 1,
                    TypeName = "Standard Room (สแตนดาร์ด)",
                    Description = "ห้องพักมาตรฐานตกแต่งสไตล์โมเดิร์นอบอุ่น สะอาดปลอดภัย เตียงนอนนุ่มสบาย พร้อมสมาร์ททีวีและอินเทอร์เน็ตความเร็วสูง",
                    BasePrice = 1200,
                    Capacity = 2,
                    Rooms = new List<Room>
                    {
                        new Room { RoomId = 101, RoomNo = "101", Branch = "Bangkok", Status = "Available" },
                        new Room { RoomId = 102, RoomNo = "102", Branch = "Chonburi", Status = "Available" },
                        new Room { RoomId = 103, RoomNo = "103", Branch = "Phuket", Status = "Available" }
                    }
                },
                new Roomtype
                {
                    RoomTypeId = 2,
                    TypeName = "Deluxe Room (ดีลักซ์)",
                    Description = "ห้องพักดีไซน์โมเดิร์นหรูหรา พื้นที่กว้างขวางพร้อมมุมโซฟาพักผ่อน ระเบียงส่วนตัว และสิ่งอำนวยความสะดวกระดับพรีเมียมครบครัน",
                    BasePrice = 2200,
                    Capacity = 2,
                    Rooms = new List<Room>
                    {
                        new Room { RoomId = 201, RoomNo = "201", Branch = "Bangkok", Status = "Available" },
                        new Room { RoomId = 202, RoomNo = "202", Branch = "Chonburi", Status = "Available" },
                        new Room { RoomId = 203, RoomNo = "203", Branch = "Phuket", Status = "Available" }
                    }
                },
                new Roomtype
                {
                    RoomTypeId = 3,
                    TypeName = "Executive Suite (ห้องสวีท)",
                    Description = "ห้องสวีทสุดหรูพื้นที่กว้างขวาง แยกโซนห้องนอนและห้องนั่งเล่นชัดเจน พร้อมอ่างอาบน้ำจากุซซี่และระเบียงชมวิวแบบพาโนรามา",
                    BasePrice = 3800,
                    Capacity = 4,
                    Rooms = new List<Room>
                    {
                        new Room { RoomId = 301, RoomNo = "301", Branch = "Bangkok", Status = "Available" },
                        new Room { RoomId = 302, RoomNo = "302", Branch = "Chonburi", Status = "Available" },
                        new Room { RoomId = 303, RoomNo = "303", Branch = "Phuket", Status = "Available" }
                    }
                }
            };
        }

        return View(roomTypes);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
