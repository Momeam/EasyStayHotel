// นำเข้า System สำหรับชนิดข้อมูลและคลาสพื้นฐาน
using System;
// นำเข้า System.Collections.Generic สำหรับการใช้งาน List
using System.Collections.Generic;

// กำหนดเนมสเปซ EasyStayHotel.Services
namespace EasyStayHotel.Services
{
    // คลาสสแตติก RoomPreviewHelper สำหรับช่วยดึงข้อมูลรูปภาพ ขนาดห้อง สิ่งอำนวยความสะดวก และแปลงชื่อสาขาเป็นภาษาไทย
    public static class RoomPreviewHelper
    {
        // ฟังก์ชันแปลงชื่อสาขาภาษาอังกฤษเป็นชื่อภาษาไทยที่ถูกต้อง
        public static string GetBranchNameTh(string? branch)
        {
            return branch switch
            {
                // สาขากรุงเทพฯ
                "Bangkok" => "กรุงเทพมหานคร",
                // สาขาชลบุรี-พัทยา
                "Chonburi" => "ชลบุรี (พัทยา)",
                // สาขาภูเก็ต
                "Phuket" => "ภูเก็ต",
                // กรณีอื่นๆ หรือค่าว่าง ให้แสดงว่าทุกสาขา
                _ => string.IsNullOrEmpty(branch) ? "ทุกสาขา" : branch
            };
        }

        // ฟังก์ชันดึงรูปภาพหน้าปกหลักของประเภทห้องพัก
        public static string GetMainImage(string? typeName, int roomTypeId = 0)
        {
            // ดึงแกลเลอรีรูปภาพทั้งหมดของห้องประเภทนั้น
            var gallery = GetGalleryImages(typeName, roomTypeId);
            // คืนค่ารูปแรกในแกลเลอรีเป็นรูปปก
            return gallery[0];
        }

        // ฟังก์ชันดึงแกลเลอรีภาพหลายมุมมองตามประเภทห้องพัก
        public static List<string> GetGalleryImages(string? typeName, int roomTypeId = 0)
        {
            // แปลงชื่อประเภทห้องเป็นตัวพิมพ์เล็กเพื่อตรวจสอบ
            string name = (typeName ?? "").ToLower();

            // ตรวจสอบกรณีเป็นห้องระดับ Suite หรือ Executive
            if (name.Contains("suite") || name.Contains("สวีท") || name.Contains("vip") || name.Contains("總統") || roomTypeId == 3)
            {
                // คืนค่าชุดภาพห้องสวีทระดับพรีเมียม
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1000&q=80"
                };
            }

            // ตรวจสอบกรณีเป็นห้อง Deluxe หรือ Superior
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || name.Contains("superior") || roomTypeId == 2)
            {
                // คืนค่าชุดภาพห้องดีลักซ์
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?auto=format&fit=crop&w=1000&q=80"
                };
            }

            // ตรวจสอบกรณีเป็นห้อง Family หรือ Villa
            if (name.Contains("family") || name.Contains("ครอบครัว") || name.Contains("villa") || name.Contains("วิลล่า"))
            {
                // คืนค่าชุดภาพห้องสำหรับครอบครัว
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1540518614846-7ede433c457?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584132967334-10e028bd69f7?auto=format&fit=crop&w=1000&q=80"
                };
            }

            // คืนค่าชุดภาพห้องพักมาตรฐาน (Standard)
            return new List<string>
            {
                "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=1000&q=80",
                "https://images.unsplash.com/photo-1595526114035-0d45ed16cfbf?auto=format&fit=crop&w=1000&q=80",
                "https://images.unsplash.com/photo-1600566752355-35792bedcfea?auto=format&fit=crop&w=1000&q=80"
            };
        }

        // ฟังก์ชันดึงขนาดพื้นที่ใช้สอยของห้องพัก (ตารางเมตร)
        public static string GetRoomSize(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3) return "65 ตร.ม.";
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2) return "42 ตร.ม.";
            if (name.Contains("family") || name.Contains("ครอบครัว")) return "55 ตร.ม.";
            return "30 ตร.ม.";
        }

        // ฟังก์ชันดึงประเภทเตียงนอนของแต่ละห้องพัก
        public static string GetBedType(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
                return "1 เตียงคิงไซส์ใหญ่พิเศษ + ห้องนั่งเล่นแยกส่วน";
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2)
                return "1 เตียงคิงไซส์ (King Size) พร้อมโซฟาพักผ่อน";
            if (name.Contains("family") || name.Contains("ครอบครัว"))
                return "2 เตียงควีนไซส์ สำหรับครอบครัว";
            return "1 เตียงควีนไซส์ หรือ 2 เตียงเดี่ยว (Twin)";
        }

        // ฟังก์ชันดึงป้ายไฮไลท์และคลาสสีสไตล์ Bootstrap Badge สำหรับการ์ดห้องพัก
        public static (string Label, string BadgeClass) GetHighlightBadge(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
                return ("ลักชัวรีพรีเมียม", "bg-warning text-dark");
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2)
                return ("ยอดนิยมอันดับ 1", "bg-danger text-white");
            return ("คุ้มค่าราคาดีที่สุด", "bg-primary text-white");
        }

        // ฟังก์ชันดึงรายละเอียดคำบรรยายห้องพัก
        public static string GetDefaultDescription(string? typeName, string? dbDescription, int roomTypeId = 0)
        {
            // หากมีคำบรรยายในฐานข้อมูลให้ใช้จากฐานข้อมูลเป็นหลัก
            if (!string.IsNullOrWhiteSpace(dbDescription))
            {
                return dbDescription;
            }

            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
                return "ห้องสวีทสุดหรูพื้นที่กว้างขวาง แยกโซนห้องนอนและห้องนั่งเล่นชัดเจน พร้อมอ่างอาบน้ำจากุซซี่และระเบียงชมวิวแบบพาโนรามา";
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2)
                return "ห้องพักดีไซน์โมเดิร์นหรูหรา พื้นที่กว้างขวางพร้อมมุมโซฟาพักผ่อน ระเบียงส่วนตัว และสิ่งอำนวยความสะดวกระดับพรีเมียมครบครัน";
            return "ห้องพักมาตรฐานตกแต่งสไตล์โมเดิร์นอบอุ่น สะอาดปลอดภัย เตียงนอนนุ่มสบาย พร้อมสมาร์ททีวีและอินเทอร์เน็ตความเร็วสูง";
        }

        // ฟังก์ชันดึงรายการไอคอนและชื่อสิ่งอำนวยความสะดวก (Amenities) ประจำห้องพัก
        public static List<(string Icon, string Text)> GetAmenities(string? typeName, int roomTypeId = 0)
        {
            // รายการสิ่งอำนวยความสะดวกพื้นฐานสำหรับทุกประเภทห้อง
            var list = new List<(string Icon, string Text)>
            {
                ("bi-wifi", "ฟรี Wi-Fi ความเร็วสูง"),
                ("bi-cup-hot-fill", "รวมบุฟเฟต์อาหารเช้า"),
                ("bi-tv", "Smart TV 55\" (Netflix/YouTube)"),
                ("bi-snow", "เครื่องปรับอากาศอัจฉริยะ"),
                ("bi-shield-check", "ตู้นิรภัย & คีย์การ์ดส่วนตัว")
            };

            string name = (typeName ?? "").ToLower();
            // สิ่งอำนวยความสะดวกพิเศษสำหรับห้อง Deluxe ขึ้นไป
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || name.Contains("suite") || name.Contains("สวีท") || roomTypeId >= 2)
            {
                list.Add(("bi-sunrise", "ระเบียงชมวิวส่วนตัว"));
                list.Add(("bi-cup-straw", "มินิบาร์ & เครื่องชงกาแฟสด"));
            }
            // สิ่งอำนวยความสะดวกพิเศษสำหรับห้อง Suite
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
            {
                list.Add(("bi-water", "อ่างอาบน้ำ Bathtub หรู"));
                list.Add(("bi-stars", "สิทธิ์เข้าใช้ Executive Lounge"));
            }

            return list;
        }
    }
}
