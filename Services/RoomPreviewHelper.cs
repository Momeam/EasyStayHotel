using System;
using System.Collections.Generic;

namespace EasyStayHotel.Services
{
    public static class RoomPreviewHelper
    {
        public static string GetBranchNameTh(string? branch)
        {
            return branch switch
            {
                "Bangkok" => "กรุงเทพมหานคร",
                "Chonburi" => "ชลบุรี (พัทยา)",
                "Phuket" => "ภูเก็ต",
                _ => string.IsNullOrEmpty(branch) ? "ทุกสาขา" : branch
            };
        }

        public static string GetMainImage(string? typeName, int roomTypeId = 0)
        {
            var gallery = GetGalleryImages(typeName, roomTypeId);
            return gallery[0];
        }

        public static List<string> GetGalleryImages(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();

            if (name.Contains("suite") || name.Contains("สวีท") || name.Contains("vip") || name.Contains("總統") || roomTypeId == 3)
            {
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1000&q=80"
                };
            }

            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || name.Contains("superior") || roomTypeId == 2)
            {
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?auto=format&fit=crop&w=1000&q=80"
                };
            }

            if (name.Contains("family") || name.Contains("ครอบครัว") || name.Contains("villa") || name.Contains("วิลล่า"))
            {
                return new List<string>
                {
                    "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1540518614846-7ede433c457?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584132967334-10e028bd69f7?auto=format&fit=crop&w=1000&q=80"
                };
            }

            // Default / Standard Room
            return new List<string>
            {
                "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=1000&q=80",
                "https://images.unsplash.com/photo-1595526114035-0d45ed16cfbf?auto=format&fit=crop&w=1000&q=80",
                "https://images.unsplash.com/photo-1600566752355-35792bedcfea?auto=format&fit=crop&w=1000&q=80"
            };
        }

        public static string GetRoomSize(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3) return "65 ตร.ม.";
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2) return "42 ตร.ม.";
            if (name.Contains("family") || name.Contains("ครอบครัว")) return "55 ตร.ม.";
            return "30 ตร.ม.";
        }

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

        public static (string Label, string BadgeClass) GetHighlightBadge(string? typeName, int roomTypeId = 0)
        {
            string name = (typeName ?? "").ToLower();
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
                return ("✨ ลักชัวรีพรีเมียม", "bg-warning text-dark");
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || roomTypeId == 2)
                return ("🔥 ยอดนิยมอันดับ 1", "bg-danger text-white");
            return ("💎 คุ้มค่าราคาดีที่สุด", "bg-primary text-white");
        }

        public static string GetDefaultDescription(string? typeName, string? dbDescription, int roomTypeId = 0)
        {
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

        public static List<(string Icon, string Text)> GetAmenities(string? typeName, int roomTypeId = 0)
        {
            var list = new List<(string Icon, string Text)>
            {
                ("bi-wifi", "ฟรี Wi-Fi ความเร็วสูง"),
                ("bi-cup-hot-fill", "รวมบุฟเฟต์อาหารเช้า"),
                ("bi-tv", "Smart TV 55\" (Netflix/YouTube)"),
                ("bi-snow", "เครื่องปรับอากาศอัจฉริยะ"),
                ("bi-shield-check", "ตู้นิรภัย & คีย์การ์ดส่วนตัว")
            };

            string name = (typeName ?? "").ToLower();
            if (name.Contains("deluxe") || name.Contains("ดีลักซ์") || name.Contains("suite") || name.Contains("สวีท") || roomTypeId >= 2)
            {
                list.Add(("bi-sunrise", "ระเบียงชมวิวส่วนตัว"));
                list.Add(("bi-cup-straw", "มินิบาร์ & เครื่องชงกาแฟสด"));
            }
            if (name.Contains("suite") || name.Contains("สวีท") || roomTypeId == 3)
            {
                list.Add(("bi-water", "อ่างอาบน้ำ Bathtub หรู"));
                list.Add(("bi-stars", "สิทธิ์เข้าใช้ Executive Lounge"));
            }

            return list;
        }
    }
}
