// ============================================================================
// Easy Stay Hotel & Resort - Complete Trilingual I18n Engine (ไทย / English / 中文)
// ครอบคลุมทุกหน้าฝั่งลูกค้า โมดอล ป้ายสถานะ ข้อความไดนามิก รีวิว และข้อมูลจากฐานข้อมูล
// ============================================================================

(function () {
    const STORAGE_KEY = 'easystay_lang';
    const SUPPORTED_LANGS = {
        th: { code: 'TH', label: 'ไทย', htmlLang: 'th' },
        en: { code: 'EN', label: 'English', htmlLang: 'en' },
        zh: { code: '中文', label: '中文', htmlLang: 'zh-CN' }
    };

    // 1. คลังคำศัพท์แปลตรงตัว (Exact Dictionary: Thai -> English & Simplified Chinese)
    const EXACT_DICT = {
        // --- Navbar & User Menu & Footer ---
        "หน้าแรก": { en: "Home", zh: "首页" },
        "พรีวิวห้องพัก": { en: "Room Previews", zh: "客房预览" },
        "โปรโมชั่น": { en: "Promotions", zh: "优惠活动" },
        "ตรวจสอบการจอง": { en: "Check Booking", zh: "查询预订" },
        "เข้าสู่ระบบ": { en: "Sign In", zh: "登录" },
        "สมัครสมาชิก": { en: "Register", zh: "注册会员" },
        "ออกจากระบบ": { en: "Sign Out", zh: "退出登录" },
        "ออกจากระบบ (Logout)": { en: "Sign Out (Logout)", zh: "退出登录" },
        "ข้อมูลส่วนตัว / โปรไฟล์": { en: "My Profile", zh: "个人资料" },
        "ประวัติการจองของฉัน": { en: "My Booking History", zh: "我的预订记录" },
        "ดูประวัติการจองของฉัน": { en: "View My Bookings", zh: "查看我的预订" },
        "แอดมิน": { en: "Admin", zh: "管理员" },
        "พนักงาน": { en: "Staff", zh: "员工系统" },
        "ระบบพนักงาน": { en: "Staff Console", zh: "前台员工系统" },
        "ระบบบัญชี": { en: "Accounting", zh: "财务系统" },
        "แดชบอร์ดแม่บ้าน": { en: "Housekeeping", zh: "客房部看板" },
        "ระบบแอดมิน (Admin Console)": { en: "Admin Console", zh: "管理员控制台" },
        "ระบบพนักงาน (Staff Console)": { en: "Staff Console", zh: "前台员工系统" },
        "ระบบบัญชี (Accounting Console)": { en: "Accounting Console", zh: "财务管理系统" },
        "แดชบอร์ดแม่บ้าน (Housekeeping)": { en: "Housekeeping Console", zh: "客房清洁看板" },
        "เครือโรงแรมและรีสอร์ทชั้นนำในประเทศไทย มอบประสบการณ์การพักผ่อนที่เหนือระดับ ในทำเลศักยภาพทั้งกรุงเทพมหานคร ชลบุรี (พัทยา) และภูเก็ต": {
            en: "Thailand's premier hotel & resort chain, delivering an extraordinary stay across prime locations in Bangkok, Chonburi (Pattaya), and Phuket.",
            zh: "泰国领先的酒店及度假村集团，在曼谷、春武里（芭堤雅）和普吉岛核心地段为您提供卓越的度假体验。"
        },
        "สาขาที่ให้บริการ (Our Branches)": { en: "Our Branches", zh: "旗下分店 (Our Branches)" },
        "สาขากรุงเทพมหานคร (Bangkok City Center)": { en: "Bangkok City Center Branch", zh: "曼谷市中心分店 (Bangkok)" },
        "สาขาชลบุรี - พัทยา (Pattaya Beachfront)": { en: "Chonburi - Pattaya Beachfront Branch", zh: "春武里 - 芭堤雅海滨分店 (Pattaya)" },
        "สาขาภูเก็ต (Phuket Andaman Resort)": { en: "Phuket Andaman Resort Branch", zh: "普吉岛安达曼度假村分店 (Phuket)" },
        "เมนูลัด & ติดต่อเรา": { en: "Quick Links & Contact Us", zh: "快捷菜单与联系我们" },
        "ดูพรีวิวห้องพักทั้งหมด": { en: "View All Room Previews", zh: "查看所有客房预览" },
        "ตรวจสอบสถานะการจอง": { en: "Check Booking Status", zh: "查询预订状态" },
        "โทร: 02-123-4567 (ตลอด 24 ชม.)": { en: "Tel: 02-123-4567 (24 Hours)", zh: "电话：02-123-4567（24小时服务）" },
        "อีเมล: contact@easystayhotel.com": { en: "Email: contact@easystayhotel.com", zh: "邮箱：contact@easystayhotel.com" },
        "นโยบายความเป็นส่วนตัว": { en: "Privacy Policy", zh: "隐私政策" },

        // --- Hero & Search Form ---
        "เครือโรงแรมและรีสอร์ทมาตรฐาน 5 ดาว": { en: "5-Star Luxury Hotels & Resorts", zh: "五星级标准豪华酒店与度假村" },
        "ค้นหาโรงแรมและรีสอร์ทที่ดีที่สุดในเมืองไทย": { en: "Find the Best Hotels & Resorts in Thailand", zh: "探索泰国最优质的豪华酒店与度假村" },
        "ชมพรีวิวห้องพักสุดหรูทั้ง 3 สาขา (กรุงเทพฯ • พัทยา • ภูเก็ต) รับประกันราคาดีที่สุดและส่วนลดพิเศษเมื่อจองตรงกับ Easy Stay": {
            en: "Explore luxury rooms across 3 branches (Bangkok • Pattaya • Phuket). Best price guaranteed with exclusive discounts when booking direct.",
            zh: "浏览三大分店（曼谷 • 芭堤雅 • 普吉岛）豪华客房，官方直订享最优价格保证与专属折扣。"
        },
        "เลือกสาขา / จังหวัด": { en: "Branch / Location", zh: "选择分店 / 城市" },
        "สาขา / จังหวัด": { en: "Branch / Location", zh: "分店 / 城市" },
        "กรุงเทพมหานคร (Bangkok)": { en: "Bangkok", zh: "曼谷 (Bangkok)" },
        "ชลบุรี - พัทยา (Chonburi)": { en: "Chonburi - Pattaya", zh: "春武里 - 芭堤雅 (Chonburi)" },
        "ภูเก็ต (Phuket)": { en: "Phuket", zh: "普吉岛 (Phuket)" },
        "วันที่ Check-in": { en: "Check-in Date", zh: "入住日期 (Check-in)" },
        "วันที่ Check-out": { en: "Check-out Date", zh: "退房日期 (Check-out)" },
        "วันเช็คอิน": { en: "Check-in Date", zh: "入住日期" },
        "วันเช็คเอาท์": { en: "Check-out Date", zh: "退房日期" },
        "เช็คอิน": { en: "Check-in", zh: "入住日期" },
        "เช็คเอาท์": { en: "Check-out", zh: "退房日期" },
        "เช็คอิน:": { en: "Check-in:", zh: "入住：" },
        "เช็คเอาท์:": { en: "Check-out:", zh: "退房：" },
        "| เช็คอิน:": { en: "| Check-in:", zh: "| 入住：" },
        "| เช็คเอาท์:": { en: "| Check-out:", zh: "| 退房：" },
        "ระยะเวลา": { en: "Duration", zh: "入住时长" },
        "จำนวนห้อง": { en: "Rooms", zh: "房间数" },
        "จำนวนแขก": { en: "Guests", zh: "住客人数" },
        "ค้นหาห้องว่าง": { en: "Search Rooms", zh: "搜索空房" },
        "อัปเดตการค้นหา": { en: "Update Search", zh: "更新搜索" },
        "กลับหน้าแรก": { en: "Back to Home", zh: "返回首页" },
        "กลับสู่หน้าหลัก": { en: "Back to Home", zh: "返回首页" },
        "ย้อนกลับ": { en: "Back", zh: "返回" },
        "ปิดหน้าต่าง": { en: "Close", zh: "关闭窗口" },
        "ยกเลิก": { en: "Cancel", zh: "取消" },
        "บันทึกข้อมูล": { en: "Save Changes", zh: "保存资料" },

        // --- Room Previews, Branches & Room Types ---
        "พรีวิวห้องพักสุดหรูของเรา": { en: "Our Luxury Room Previews", zh: "精选豪华客房预览" },
        "เลือกชมตัวอย่างห้องพัก เปรียบเทียบสิ่งอำนวยความสะดวก และตรวจสอบห้องว่างในแต่ละสาขาได้ทันที": {
            en: "Browse room galleries, compare amenities, and check real-time availability across all branches.",
            zh: "浏览客房实景、对比配套设施，并即时查看各分店空房情况。"
        },
        "ทุกสาขา": { en: "All Branches", zh: "所有分店" },
        "กรุงเทพฯ": { en: "Bangkok", zh: "曼谷" },
        "กรุงเทพมหานคร": { en: "Bangkok", zh: "曼谷" },
        "ชลบุรี": { en: "Chonburi", zh: "春武里" },
        "ชลบุรี (พัทยา)": { en: "Chonburi (Pattaya)", zh: "春武里 (芭堤雅)" },
        "ภูเก็ต": { en: "Phuket", zh: "普吉岛" },
        "สาขากรุงเทพมหานคร": { en: "Bangkok Branch", zh: "曼谷分店" },
        "สาขาชลบุรี (พัทยา)": { en: "Chonburi (Pattaya) Branch", zh: "春武里(芭堤雅)分店" },
        "สาขาภูเก็ต": { en: "Phuket Branch", zh: "普吉岛分店" },
        "สาขากรุงเทพมหานคร (Bangkok)": { en: "Bangkok Branch", zh: "曼谷分店 (Bangkok)" },
        "สาขาชลบุรี - พัทยา (Chonburi)": { en: "Chonburi - Pattaya Branch", zh: "春武里 - 芭堤雅分店 (Chonburi)" },
        "สาขาภูเก็ต (Phuket)": { en: "Phuket Branch", zh: "普吉岛分店 (Phuket)" },
        "คุ้มค่าราคาดีที่สุด": { en: "Best Value", zh: "超值精选" },
        "ยอดนิยมอันดับ 1": { en: "#1 Most Popular", zh: "人气首选 #1" },
        "ลักชัวรีพรีเมียม": { en: "Luxury Premium", zh: "尊享奢华" },
        "เต็มชั่วคราว": { en: "Sold Out", zh: "暂时客满" },
        "เต็มในสาขานี้": { en: "Full at this branch", zh: "该分店已满" },
        "ห้องพักเต็ม": { en: "Fully Booked", zh: "客房已满" },
        "ห้องว่างในแต่ละสาขา (คลิกเพื่อจอง):": { en: "Available by Branch (Click to book):", zh: "各分店空房（点击直接预订）：" },
        "ราคาเริ่มต้น": { en: "Starting from", zh: "起价" },
        "รวมอาหารเช้า": { en: "Breakfast Included", zh: "含早餐" },
        "รวมภาษีและค่าบริการ": { en: "Taxes & fees included", zh: "已含税费和服务费" },
        "/ คืน": { en: "/ Night", zh: "/ 晚" },
        "ดูรายละเอียด": { en: "View Details", zh: "查看详情" },
        "พรีวิวห้อง": { en: "Preview Room", zh: "预览客房" },
        "จองเลย": { en: "Book Now", zh: "立即预订" },
        "จองห้องพักนี้ทันที": { en: "Book This Room Now", zh: "立即预订此房型" },
        "เลือกห้องนี้": { en: "Select Room", zh: "选择此房型" },
        "เลือกสาขา": { en: "Select Branch", zh: "选择分店" },
        "เลือกสาขาที่ต้องการจอง": { en: "Select Branch to Book", zh: "选择预订分店" },
        "ขนาดห้องพัก": { en: "Room Size", zh: "客房面积" },
        "จำนวนผู้เข้าพัก": { en: "Occupancy", zh: "可住人数" },
        "ราคาต่อคืน": { en: "Price / Night", zh: "每晚价格" },
        "รายละเอียดห้องพัก": { en: "Room Description", zh: "客房详情" },
        "รูปแบบเตียง:": { en: "Bed Type:", zh: "床型：" },
        "สิ่งอำนวยความสะดวกภายในห้องพัก": { en: "Room Amenities & Facilities", zh: "客房配套设施" },
        "สิ่งอำนวยความสะดวกครบครัน:": { en: "Full Room Amenities:", zh: "完善的配套设施：" },
        "เลือกสาขา (พร้อมจำนวนห้องว่าง)": { en: "Select Branch (with availability)", zh: "选择分店（含空房数量）" },
        "ดำเนินการค้นหาและจองห้องนี้": { en: "Search & Book This Room", zh: "搜索并预订此客房" },
        "มุมห้องนอนหลัก (Master Bedroom)": { en: "Master Bedroom View", zh: "主卧室视角 (Master Bedroom)" },
        "มุมพักผ่อนและบรรยากาศภายในห้อง": { en: "Living & Relaxation Area", zh: "休闲区与室内氛围" },
        "ห้องน้ำหรูและสิ่งอำนวยความสะดวก": { en: "Luxury Bathroom & Amenities", zh: "豪华卫浴与配套设施" },

        // --- Room Names, Database Descriptions, Bed Types & Amenities ---
        "Standard": { en: "Standard Room", zh: "标准客房 (Standard)" },
        "Deluxe": { en: "Deluxe Room", zh: "豪华客房 (Deluxe)" },
        "Suite": { en: "Executive Suite", zh: "豪华套房 (Suite)" },
        "Standard Room (สแตนดาร์ด)": { en: "Standard Room", zh: "标准客房 (Standard Room)" },
        "Deluxe Room (ดีลักซ์)": { en: "Deluxe Room", zh: "豪华客房 (Deluxe Room)" },
        "Executive Suite (ห้องสวีท)": { en: "Executive Suite", zh: "行政套房 (Executive Suite)" },
        "ห้องพักมาตรฐาน (Easy Stay Room)": { en: "Standard Room (Easy Stay Room)", zh: "标准客房 (Easy Stay Room)" },
        "ห้องพักมาตรฐาน": { en: "Standard Room", zh: "标准客房" },
        "ห้องขนาดใหญ่พร้อมระเบียง": { en: "Spacious luxury room with private balcony", zh: "带私人阳台的宽敞豪华客房" },
        "ห้องพักสุดหรูวิวทะเล": { en: "Luxury suite with panoramic ocean view", zh: "全景海景奢华套房" },
        "ห้องพักมาตรฐานสะดวกสบาย": { en: "Comfortable modern standard room", zh: "舒适现代标准客房" },
        "30 ตร.ม.": { en: "30 sq.m.", zh: "30 平方米" },
        "42 ตร.ม.": { en: "42 sq.m.", zh: "42 平方米" },
        "55 ตร.ม.": { en: "55 sq.m.", zh: "55 平方米" },
        "65 ตร.ม.": { en: "65 sq.m.", zh: "65 平方米" },
        "1 เตียงควีนไซส์ หรือ 2 เตียงเดี่ยว (Twin)": { en: "1 Queen Bed or 2 Twin Beds", zh: "1张大床 (Queen) 或 2张单人床 (Twin)" },
        "1 เตียงคิงไซส์ (King Size) พร้อมโซฟาพักผ่อน": { en: "1 King Size Bed with Lounge Sofa", zh: "1张特大床 (King Size) 配休闲沙发" },
        "1 เตียงคิงไซส์ใหญ่พิเศษ + ห้องนั่งเล่นแยกส่วน": { en: "1 Super King Bed + Separate Living Room", zh: "1张超大特大床 + 独立客厅" },
        "2 เตียงควีนไซส์ สำหรับครอบครัว": { en: "2 Queen Beds for Family", zh: "2张双人大床（适合家庭）" },
        "ห้องพักมาตรฐานตกแต่งสไตล์โมเดิร์นอบอุ่น สะอาดปลอดภัย เตียงนอนนุ่มสบาย พร้อมสมาร์ททีวีและอินเทอร์เน็ตความเร็วสูง": {
            en: "Warm modern standard room, clean and secure, featuring a plush bed, Smart TV, and high-speed Wi-Fi.",
            zh: "现代温馨标准客房，干净安全，配备舒适床铺、智能电视及高速无线网络。"
        },
        "ห้องพักดีไซน์โมเดิร์นหรูหรา พื้นที่กว้างขวางพร้อมมุมโซฟาพักผ่อน ระเบียงส่วนตัว และสิ่งอำนวยความสะดวกระดับพรีเมียมครบครัน": {
            en: "Spacious modern luxury room with a cozy sofa corner, private balcony, and full premium amenities.",
            zh: "现代奢华设计客房，空间宽敞，配有休闲沙发区、私人阳台及全套尊贵设施。"
        },
        "ห้องสวีทสุดหรูพื้นที่กว้างขวาง แยกโซนห้องนอนและห้องนั่งเล่นชัดเจน พร้อมอ่างอาบน้ำจากุซซี่และระเบียงชมวิวแบบพาโนรามา": {
            en: "Expansive luxury suite with separate bedroom and living room, Jacuzzi bathtub, and panoramic view balcony.",
            zh: "宽敞豪华行政套房，卧室与客厅独立分区，配有按摩浴缸及全景私人阳台。"
        },
        "ฟรี Wi-Fi ความเร็วสูง": { en: "Free High-Speed Wi-Fi", zh: "免费高速 Wi-Fi" },
        "รวมบุฟเฟต์อาหารเช้า": { en: "Breakfast Buffet Included", zh: "含自助早餐" },
        "Smart TV 55\" (Netflix/YouTube)": { en: "55\" Smart TV (Netflix/YouTube)", zh: "55寸智能电视 (Netflix/YouTube)" },
        "เครื่องปรับอากาศอัจฉริยะ": { en: "Smart Air Conditioning", zh: "智能恒温空调" },
        "ตู้นิรภัย & คีย์การ์ดส่วนตัว": { en: "In-room Safe & Keycard Access", zh: "私人保险箱与门禁卡" },
        "ระเบียงชมวิวส่วนตัว": { en: "Private Scenic Balcony", zh: "私人观景阳台" },
        "มินิบาร์ & เครื่องชงกาแฟสด": { en: "Mini-bar & Espresso Machine", zh: "迷你吧与现磨咖啡机" },
        "อ่างอาบน้ำ Bathtub หรู": { en: "Luxury Bathtub", zh: "豪华独立浴缸" },
        "สิทธิ์เข้าใช้ Executive Lounge": { en: "Executive Lounge Access", zh: "行政酒廊尊享礼遇" },

        // --- Destinations & Promotions ---
        "เลือกพักผ่อนใน 3 จุดหมายปลายทางยอดนิยม": { en: "Stay Across 3 Iconic Destinations", zh: "畅享三大热门旅游目的地" },
        "คลิกเลือกสาขาเพื่อค้นหาห้องพักว่างในจังหวัดที่คุณต้องการเดินทาง": {
            en: "Click a destination below to explore available rooms for your upcoming trip.",
            zh: "点击下方分店，即刻探索您旅行目的地的空余客房。"
        },
        "ใจกลางย่านธุรกิจ เดินทางสะดวกใกล้รถไฟฟ้า BTS": {
            en: "Prime central business district location with easy access to BTS Skytrain.",
            zh: "位于曼谷核心商业区，毗邻 BTS 轻轨站，出行便捷。"
        },
        "ติดชายหาดพัทยา พร้อมสระว่ายน้ำวิวทะเลสุดโรแมนติก": {
            en: "Direct Pattaya beachfront access with a romantic ocean-view infinity pool.",
            zh: "毗邻芭堤雅海滩，配备浪漫海景无边泳池。"
        },
        "รีสอร์ทหรูริมทะเลอันดามัน ดื่มด่ำธรรมชาติและหาดทรายขาว": {
            en: "Luxury Andaman seaside resort surrounded by pristine white-sand beaches.",
            zh: "安达曼海畔奢华度假村，尽享自然风光与洁白沙滩。"
        },
        "ข้อเสนอพิเศษและโปรโมชั่นยอดนิยม": { en: "Special Offers & Popular Promotions", zh: "热门特惠与专属促销活动" },
        "รับส่วนลดอัตโนมัติทันทีเมื่อเข้าเงื่อนไข หรือคัดลอกรหัสคูปองไปใช้ในขั้นตอนชำระเงิน": {
            en: "Enjoy automatic discounts when eligible, or copy coupon codes to apply at checkout.",
            zh: "符合条件即可自动享受折扣，或复制优惠码在结账时使用。"
        },
        "Early Bird จองล่วงหน้า 30 วัน": { en: "Early Bird (30 Days Advance)", zh: "早鸟优惠（提前30天预订）" },
        "ส่วนลดพิเศษ Early Bird 15%": { en: "Special 15% Early Bird Discount", zh: "享早鸟专属 85折（立减15%）" },
        "วางแผนการเดินทางล่วงหน้าอย่างน้อย 30 วัน ใช้โค้ดด้านล่างเพื่อรับส่วนลดทันที 15%": {
            en: "Plan your trip at least 30 days in advance and get 15% off instantly.",
            zh: "提前至少30天规划您的行程，使用下方优惠码即享 15% 折扣。"
        },
        "พักยาวคุ้มกว่า & วันธรรมดาน่าพัก": { en: "Long Stay & Weekday Deals", zh: "连住优惠 & 工作日特惠" },
        "ลดอัตโนมัติ 10% (คืนที่ 3) + ลดวันธรรมดา 5%": { en: "Auto 10% Off (3rd Night) + 5% Off Weekdays", zh: "第3晚自动减10% + 工作日额外减5%" },
        "เข้าพัก 3 คืนขึ้นไป หรือเช็คอินวันจันทร์-พฤหัสบดี ระบบคำนวณส่วนลดให้อัตโนมัติทันทีโดยไม่ต้องใช้โค้ด": {
            en: "Stay 3+ nights or check in Mon-Thu for automatic discounts applied directly at checkout.",
            zh: "连住3晚及以上或周一至周四入住，系统将在结账时自动计算折扣。"
        },
        "ลดอัตโนมัติในหน้าชำระเงิน": { en: "Auto-applied at Checkout", zh: "结账时自动减免" },
        "ฉลองเดือนเกิด & จองแบบกลุ่ม": { en: "Birthday Month & Group Booking", zh: "生日月礼遇 & 团队预订优惠" },
        "ส่วนลดเดือนเกิด 10% / จอง 2 ห้องลด 15%": { en: "10% Off Birthday Month / 15% Off 2+ Rooms", zh: "生日月立减10% / 预订2间起立减15%" },
        "สมาชิกที่เข้าพักในเดือนเกิดรับส่วนลดอัตโนมัติ 10% หรือใช้โค้ดเมื่อจองตั้งแต่ 2 ห้องขึ้นไป": {
            en: "Members staying in their birthday month get 10% off automatically, or 15% off when booking 2+ rooms.",
            zh: "会员在生日当月入住自动享9折，或预订2间及以上客房使用优惠码享85折。"
        },
        "โค้ด:": { en: "Code:", zh: "优惠码：" },
        "โค้ดกลุ่ม:": { en: "Group Code:", zh: "团队码：" },
        "(คลิกคัดลอก)": { en: "(Click to copy)", zh: "（点击复制）" },

        // --- Guest Reviews Section (including JSON Review Items) ---
        "เสียงตอบรับจากผู้เข้าพักจริง (Verified Guest Reviews)": { en: "Verified Guest Reviews", zh: "真实住客评价 (Verified Guest Reviews)" },
        "ความประทับใจระดับ 5 ดาวจากลูกค้าของเรา": { en: "5-Star Experiences from Our Guests", zh: "来自住客的五星级好评体验" },
        "สัมผัสประสบการณ์การพักผ่อนเหนือระดับที่ Easy Stay Hotel & Resort ทั้ง 3 สาขา ด้วยคะแนนความพึงพอใจเฉลี่ย 4.9 เต็ม 5 จากผู้เข้าพักจริง": {
            en: "Experience exceptional hospitality across all 3 Easy Stay branches, rated 4.9/5 by verified guests.",
            zh: "在 Easy Stay 三大分店体验卓越住宿服务，真实住客平均满意度高达 4.9 / 5 分。"
        },
        "ข้อความตอบกลับจากทางโรงแรม:": { en: "Response from Hotel Management:", zh: "酒店官方回复：" },
        "เข้าพักจริง": { en: "Verified Stay", zh: "真实入住" },
        "ประทับใจการเข้าพัก": { en: "Wonderful Stay", zh: "非常满意的入住体验" },
        "ห้องพักสวยมาก เตียงนอนสบาย การเดินทางสะดวก": { en: "Beautiful room, comfortable bed, very convenient location", zh: "房间非常漂亮，床铺舒适，交通十分便利" },
        "ประทับใจการบริการของพนักงานต้อนรับมากครับ ให้คำแนะนำดีเยี่ยม ห้องพักสะอาด กลิ่นหอม วิวเมืองกรุงเทพฯ สวยงาม แนะนำเลยครับ": {
            en: "Very impressed with the front desk service! Great recommendations, spotless room, pleasant scent, and a lovely Bangkok city view. Highly recommended!",
            zh: "前台接待服务令人印象深刻！提供了很棒的建议，房间干净清香，曼谷城市景观优美，强烈推荐！"
        },
        "ขอบพระคุณคุณธนภัทรเป็นอย่างยิ่งครับ ทาง Easy Stay สาขากรุงเทพฯ ยินดีที่ได้ให้บริการและหวังว่าจะได้ต้อนรับท่านอีกครั้งครับ": {
            en: "Thank you very much, Khun Thanapat! Easy Stay Bangkok is delighted to serve you and looks forward to welcoming you again.",
            zh: "非常感谢您的好评！Easy Stay 曼谷分店很高兴为您服务，期待您的再次光临。"
        },
        "วิวทะเลพัทยาสวยสะกดใจ อ่างจากุซซี่ดีมาก": { en: "Breathtaking Pattaya sea view and amazing Jacuzzi", zh: "令人惊叹的芭堤雅海景，按摩浴缸非常棒" },
        "พาครอบครัวมาพักผ่อนที่สาขาชลบุรี-พัทยา ห้องสวีทกว้างขวาง อ่างจากุซซี่แช่สบายมาก อาหารเช้าอร่อยหลากหลาย คุ้มค่าราคามากค่ะ": {
            en: "Brought my family to the Pattaya branch. The suite is spacious, the Jacuzzi is super relaxing, and the breakfast buffet has great variety. Great value!",
            zh: "带家人来芭堤雅分店度假，套房非常宽敞，按摩浴缸很舒适，自助早餐丰富美味，物超所值！"
        },
        "ขอบพระคุณคุณศิริพรและครอบครัวครับ ทางเราดีใจเป็นอย่างยิ่งที่ท่านได้รับความสุขในการเข้าพักครับ": {
            en: "Thank you very much, Khun Siriporn and family! We are delighted that you enjoyed your stay with us.",
            zh: "非常感谢您和家人的光临！我们很高兴您在这里度过了愉快的假期。"
        },
        "บรรยากาศรีสอร์ทภูเก็ตเงียบสงบ พนักงานดูแลดุจคนในครอบครัว": {
            en: "Peaceful Phuket resort atmosphere, staff treated us like family",
            zh: "普吉岛度假村环境宁静优美，员工服务贴心周到"
        },
        "ประทับใจระบบการเช็คอินที่รวดเร็ว ไม่ต้องรอนาน ห้องพักสะอาดสะอ้าน แม่บ้านทำความสะอาดเรียบร้อยมาก มีโอกาสจะกลับมาพักอีกแน่นอนค่ะ": {
            en: "Impressed by the fast check-in without waiting. The room was spotless and housekeeping did an excellent job. Will definitely stay again!",
            zh: "快速办理入住令人印象深刻，无需久等。客房一尘不染，客房清洁非常到位，下次一定会再来！"
        },
        "ห้องพักมาตรฐานแต่สิ่งอำนวยความสะดวกครบครัน": {
            en: "Standard room with complete premium facilities",
            zh: "标准客房但设施一应俱全"
        },
        "คุ้มราคามากครับสำหรับห้อง Standard แอร์เย็นฉ่ำ ไวไฟเร็วมาก เหมาะสำหรับคนมาทำงานหรือมาเที่ยวในเมือง ใกล้สถานีรถไฟฟ้า": {
            en: "Great value for the Standard room! Cool A/C, fast Wi-Fi, perfect for business or city travel near the BTS station.",
            zh: "标准间性价比极高！空调凉爽，Wi-Fi 极快，非常适合商务出差或城市旅游，紧邻轻轨站。"
        },
        "ระเบียงกว้าง รับลมทะเลชิลมาก": { en: "Spacious balcony with a relaxing ocean breeze", zh: "阳台宽敞，海风惬意宜人" },
        "จองตอนมีโปรโมชั่น Early Bird ลด 15% คุ้มค่ามาก ห้องพักสวยเหมือนในรูป เตียงนุ่มหลับสบาย แนะนำห้องดีลักซ์เลยค่ะ": {
            en: "Booked with the 15% Early Bird discount—great value! The room looks just like the photos and the bed is super comfortable. Highly recommend the Deluxe room!",
            zh: "享受了早鸟85折优惠，非常划算！房间和照片一样漂亮，床铺柔软舒适，强烈推荐豪华客房！"
        },
        "ยอดเยี่ยม เยี่ยมยอด": { en: "Excellent & Outstanding!", zh: "非常棒，极佳体验！" },
        "สุดสุดสุดสุดสุดสุดสุด": { en: "Absolutely amazing experience!", zh: "非常棒的入住体验！" },

        // --- Search Page ---
        "ผลการค้นหา: พบประเภทห้องว่าง": { en: "Search Results: Found", zh: "搜索结果：找到" },
        "รายการ": { en: "room type(s)", zh: "种可用房型" },
        "กำลังดูสาขา:": { en: "Viewing Branch:", zh: "当前分店：" },
        "เลือกดูและจองห้องพักสาขาอื่น:": { en: "Explore & Book Other Branches:", zh: "切换查看并预订其他分店：" },
        "คลิกที่สาขาด้านล่างเพื่อสลับไปดูห้องว่างในจังหวัดที่คุณต้องการได้ทันที": {
            en: "Click any branch below to instantly switch and check room availability in that city.",
            zh: "点击下方分店按钮，即可立即切换查看对应城市的空房。"
        },
        "ตัวกรองการค้นหา": { en: "Search Filters", zh: "搜索筛选" },
        "ล้างทั้งหมด": { en: "Reset All", zh: "重置全部" },
        "ล้างตัวกรองทั้งหมด": { en: "Reset All Filters", zh: "清除所有筛选" },
        "ช่วงราคา (บาท / คืน)": { en: "Price Range (THB / Night)", zh: "价格区间 (泰铢 / 晚)" },
        "ถึง": { en: "to", zh: "至" },
        "ประเภทห้องพัก": { en: "Room Types", zh: "客房类型" },
        "ไม่มีประเภทห้องว่าง": { en: "No available room types", zh: "暂无空余房型" },
        "นำตัวกรองไปใช้": { en: "Apply Filters", zh: "应用筛选" },
        "ห้องพักประเภทที่คุณเลือกจากหน้าพรีวิว": { en: "Selected Room Type from Preview", zh: "您在预览页选择的房型" },
        "จองตามประเภทห้องพัก • พนักงานต้อนรับจะจัดสรรหมายเลขห้องพักให้ ณ วันเช็คอิน": {
            en: "Book by room type • Room number will be assigned by front desk upon check-in.",
            zh: "按房型预订 • 前台将在您办理入住时为您分配具体房间号。"
        },
        "จองประเภทนี้ในสาขาอื่น:": { en: "Book this room type at another branch:", zh: "在其他分店预订此房型：" },
        "ไม่พบห้องพักที่ตรงกับตัวกรองของคุณ": { en: "No rooms match your filter criteria", zh: "未找到符合筛选条件的客房" },
        "ลองปรับช่วงราคาหรือเลือกประเภทห้องพักเพิ่มเติม": { en: "Try adjusting the price range or selecting more room types", zh: "请尝试调整价格区间或选择更多房型" },
        "คุณสามารถเลือกดูห้องว่างและจองในสาขาอื่นของเราได้ทันที:": {
            en: "You can check availability and book at our other branches immediately:",
            zh: "您可以立即查看并预订我们其他分店的空房："
        },
        "กลับไปดูพรีวิวห้องพักทุกสาขา": { en: "Back to All Room Previews", zh: "返回查看所有分店客房预览" },

        // --- Checkout Page ---
        "ขั้นตอนการจองและเลือกบริการเสริม": { en: "Booking Details & Add-on Services", zh: "预订确认与附加服务" },
        "ตรวจสอบรายละเอียดห้องพัก เลือกบริการเสริม และชำระเงินผ่าน PromptPay": {
            en: "Review your room details, select optional add-ons, and complete payment via PromptPay.",
            zh: "请核对客房详情、选择附加服务，并通过 PromptPay 完成支付。"
        },
        "ข้อมูลห้องพักและการเข้าพัก": { en: "Room & Stay Information", zh: "客房与入住信息" },
        "จัดสรรหมายเลขห้อง ณ วันเช็คอิน": { en: "Room assigned at check-in", zh: "入住时分配房间号" },
        "จัดสรรห้องตอนเช็คอิน": { en: "Assigned at check-in", zh: "入住时分配房间" },
        "ยกเลิกฟรีล่วงหน้า 3 วันก่อนเข้าพัก": { en: "Free cancellation up to 3 days before check-in", zh: "入住前3天可免费取消" },
        "เข้าพัก": { en: "Stay", zh: "入住" },
        ", แขก": { en: ", Guests:", zh: "，住客" },
        "• ฟรี Wi-Fi และอาหารเช้า": { en: "• Free Wi-Fi & Breakfast", zh: "• 含免费 Wi-Fi 及早餐" },
        "วันที่เช็คอิน (Check-in 14:00 น.) *": { en: "Check-in Date (from 14:00) *", zh: "入住日期 (14:00 起) *" },
        "วันที่เช็คเอาท์ (Check-out 12:00 น.) *": { en: "Check-out Date (until 12:00) *", zh: "退房日期 (12:00 前) *" },
        "ข้อมูลผู้เข้าพัก": { en: "Guest Information", zh: "住客信息" },
        "ชื่อ-นามสกุล *": { en: "Full Name *", zh: "姓名 *" },
        "อีเมล (สำหรับรับใบยืนยันและเช็คสิทธิ์เดือนเกิด) *": {
            en: "Email Address (for booking confirmation & birthday privilege) *",
            zh: "电子邮箱（用于接收确认单及生日月优惠验证）*"
        },
        "เบอร์โทรศัพท์ *": { en: "Phone Number *", zh: "手机号码 *" },
        "บริการเสริมพิเศษ (Add-on Services)": { en: "Special Add-on Services", zh: "特色附加服务 (Add-on Services)" },
        "เลือกบริการเสริมเพื่อยกระดับความสะดวกสบายในการพักผ่อนของคุณ": {
            en: "Enhance your stay with our optional comfort and convenience packages.",
            zh: "选择额外定制服务，全面提升您的入住舒适体验。"
        },
        "เตียงเสริม (Extra Bed)": { en: "Extra Bed", zh: "加床服务 (Extra Bed)" },
        "สูงสุด 1 เตียงต่อห้องพัก (฿800 / คืน)": { en: "Max 1 extra bed per room (฿800 / night)", zh: "每间客房最多加1张床 (฿800 / 晚)" },
        "อาหารเช้าเพิ่มเติม (Breakfast Buffet)": { en: "Additional Breakfast Buffet", zh: "额外自助早餐 (Breakfast Buffet)" },
        "บุฟเฟต์นานาชาติ ณ ห้องอาหารของโรงแรม (฿350 / คน / วัน)": {
            en: "International buffet at the hotel restaurant (฿350 / person / day)",
            zh: "酒店餐厅国际自助早餐 (฿350 / 人 / 天)"
        },
        "รถรับส่งสนามบิน (Airport Transfer)": { en: "Airport Transfer Service", zh: "机场专车接送 (Airport Transfer)" },
        "รถตู้พรีเมียม VIP เที่ยวเดียวหรือไปกลับ (฿1,200 / เที่ยว)": {
            en: "VIP luxury van transfer, one-way or round-trip (฿1,200 / trip)",
            zh: "豪华 VIP 专车接送，单程或往返 (฿1,200 / 趟)"
        },
        "ช่องทางการชำระเงินที่ปลอดภัย": { en: "Secure Payment Method", zh: "安全支付方式" },
        "PromptPay (พร้อมเพย์)": { en: "PromptPay QR Payment", zh: "PromptPay 扫码支付" },
        "สแกน QR Code เพื่อชำระยอดรวมด้วยแอปธนาคารของคุณ": {
            en: "Scan the QR Code with your mobile banking app to pay the total amount.",
            zh: "请使用您的手机银行 App 扫描二维码支付总额。"
        },
        "อัปโหลดหลักฐานสลิปการโอนเงิน *": { en: "Upload Payment Slip / Receipt *", zh: "上传转账凭证 / 付款截图 *" },
        "สรุปค่าใช้จ่ายทั้งหมด": { en: "Payment Summary", zh: "费用明细汇总" },
        "ค่าห้องพัก": { en: "Room Charge", zh: "客房费用" },
        "ส่วนลดโปรโมชั่น": { en: "Promotion Discount", zh: "促销优惠减免" },
        "โปรโมชั่นที่ใช้:": { en: "Applied Promotion:", zh: "已享优惠：" },
        "ยอดรวมสุทธิชำระ": { en: "Grand Total", zh: "应付总金额" },
        "โค้ดส่วนลด / คูปองโปรโมชั่น": { en: "Promo Code / Discount Coupon", zh: "优惠码 / 促销礼券" },
        "ใช้คูปอง": { en: "Apply", zh: "使用优惠码" },
        "คูปองแนะนำ:": { en: "Suggested Coupons:", zh: "推荐优惠码：" },
        "EARLYBIRD (-15%)": { en: "EARLYBIRD (-15%)", zh: "EARLYBIRD (立减15%)" },
        "LONGSTAY (-10% คืนที่ 3)": { en: "LONGSTAY (-10% 3rd Night)", zh: "LONGSTAY (第3晚减10%)" },
        "GROUP15 (-15% กลุ่ม)": { en: "GROUP15 (-15% Group)", zh: "GROUP15 (团队减15%)" },
        "BIRTHDAY10 (-10% เดือนเกิด)": { en: "BIRTHDAY10 (-10% Birthday)", zh: "BIRTHDAY10 (生日月减10%)" },
        "WEEKDAY5 (-5% วันธรรมดา)": { en: "WEEKDAY5 (-5% Weekday)", zh: "WEEKDAY5 (工作日减5%)" },
        "สิทธิพิเศษโปรโมชั่น Easy Stay:": { en: "Easy Stay Promotion Privileges:", zh: "Easy Stay 专属优惠礼遇：" },
        "พักยาวคุ้มกว่า:": { en: "Long Stay:", zh: "连住优惠：" },
        "ต้อนรับเดือนเกิด:": { en: "Birthday Month:", zh: "生日月礼遇：" },
        "เข้าพักแบบกลุ่ม:": { en: "Group Booking:", zh: "团队预订：" },
        "วันธรรมดาน่าพัก:": { en: "Weekday Special:", zh: "工作日特惠：" },
        "จองล่วงหน้า > 30 วัน ลด 15% ทันที (อัตโนมัติ / โค้ด": {
            en: "Book > 30 days in advance for 15% off (Auto / Code",
            zh: "提前30天以上预订立减15%（自动 / 代码"
        },
        "จอง 3 คืนขึ้นไป ลดราคาคืนที่ 3 ลง 10% (อัตโนมัติ / โค้ด": {
            en: "Stay 3+ nights for 10% off the 3rd night (Auto / Code",
            zh: "连住3晚及以上，第3晚减10%（自动 / 代码"
        },
        "พักในเดือนเกิดของตนเอง ลด 10% (อัตโนมัติ / โค้ด": {
            en: "Stay during your birthday month for 10% off (Auto / Code",
            zh: "在生日当月入住享9折优惠（自动 / 代码"
        },
        "จองตั้งแต่ 2 ห้องขึ้นไป รับส่วนลด 15% ทุกห้อง (กรอกคูปอง": {
            en: "Book 2+ rooms for 15% off all rooms (Use code",
            zh: "预订2间及以上客房，全部享85折（输入优惠码"
        },
        "พัก จันทร์ - พฤหัสฯ ห้อง Deluxe ขึ้นไป ลดเพิ่ม 5% (อัตโนมัติ / โค้ด": {
            en: "Stay Mon-Thu in Deluxe+ rooms for extra 5% off (Auto / Code",
            zh: "周一至周四入住豪华房及以上额外减5%（自动 / 代码"
        },
        "ยืนยันและจองห้องพักตอนนี้": { en: "Confirm & Complete Booking", zh: "立即确认并提交预订" },
        "คุณจะได้รับหมายเลขการจองทันทีหลังจากส่งหลักฐานการชำระเงิน": {
            en: "You will receive your booking reference immediately after submitting payment proof.",
            zh: "提交付款凭证后，您将立即获得预订参考编号。"
        },
        "บริการเสริม: รถรับส่ง": { en: "Add-on: Airport Transfer", zh: "附加服务：机场接送" },

        // --- Booking Success, MyBookings, Refund Modal & Review Modal ---
        "การจองสำเร็จ!": { en: "Booking Successful!", zh: "预订成功！" },
        "ขอบคุณสำหรับการจองห้องพักกับ Easy Stay ระบบได้รับหลักฐานการชำระเงินของคุณแล้ว": {
            en: "Thank you for booking with Easy Stay. We have received your payment slip.",
            zh: "感谢您预订 Easy Stay 酒店，系统已收到您的付款凭证。"
        },
        "หมายเลขการจอง (Booking Reference)": { en: "Booking Reference", zh: "预订编号 (Booking Reference)" },
        "หมายเหตุ:": { en: "Note:", zh: "备注：" },
        "หมายเลขห้องพักจริงจะถูกจัดสรรโดยพนักงานต้อนรับจากห้องที่ว่างตามประเภทห้องที่ท่านจอง เมื่อท่านเดินทางมาถึงและทำการเช็คอิน ณ โรงแรม": {
            en: "Your specific room number will be assigned by our front desk from available rooms matching your booked room type upon arrival and check-in.",
            zh: "当您抵达酒店办理入住时，前台接待人员将根据您预订的房型为您分配具体房间号。"
        },
        "พนักงานของเรากำลังตรวจสอบรายการชำระเงิน คุณสามารถใช้หมายเลขการจองนี้เพื่อติดตามสถานะได้ตลอดเวลา": {
            en: "Our staff is verifying your payment. You can use this booking reference to track your status anytime.",
            zh: "工作人员正在核实您的付款，您可以随时使用此预订编号查询最新状态。"
        },
        "พิมพ์ / ดาวน์โหลดเอกสารการจอง (Booking Voucher)": {
            en: "Print / Download Booking Voucher",
            zh: "打印 / 下载预订确认单 (Booking Voucher)"
        },
        "ตรวจสอบสถานะการจองห้องพัก": { en: "Check Room Booking Status", zh: "查询客房预订状态" },
        "แสดงรายการจองทั้งหมดของบัญชีคุณ หรือค้นหาด้วยหมายเลขการจอง (Booking Reference)": {
            en: "Showing all bookings under your account, or search by Booking Reference.",
            zh: "显示您账户下的所有预订，或通过预订编号搜索。"
        },
        "กรอกหมายเลขการจอง (เช่น ES-...) หรืออีเมล/เบอร์โทรศัพท์ที่ใช้จอง เพื่อดูรายละเอียดและสถานะล่าสุด": {
            en: "Enter your Booking Reference (e.g. ES-...) or email/phone number used during booking.",
            zh: "请输入预订编号（如 ES-...）或预订时使用的邮箱/手机号以查看最新状态。"
        },
        "จองห้องพักเพิ่ม": { en: "Book Another Room", zh: "继续预订客房" },
        "ชื่อผู้จอง / อีเมล / เบอร์โทรศัพท์": { en: "Guest Name / Email / Phone Number", zh: "住客姓名 / 邮箱 / 手机号" },
        "ค้นหา": { en: "Search", zh: "搜索" },
        "หมายเลขการจอง:": { en: "Booking Ref:", zh: "预订编号：" },
        "ผู้เข้าพัก:": { en: "Guest:", zh: "住客：" },
        "ยอดชำระสุทธิ": { en: "Total Paid", zh: "实付总额" },
        "เอกสารการจอง (Voucher)": { en: "Booking Voucher", zh: "预订确认单 (Voucher)" },
        "ดูหลักฐานสลิปโอนเงิน": { en: "View Payment Slip", zh: "查看付款凭证" },
        "รอพนักงานตรวจสอบ": { en: "Pending Verification", zh: "待员工核实" },
        "ยืนยันการจองแล้ว": { en: "Booking Confirmed", zh: "预订已确认" },
        "ยื่นขอคืนเงิน (รอตรวจสอบ)": { en: "Refund Requested", zh: "已申请退款（审核中）" },
        "คืนเงินเรียบร้อยแล้ว": { en: "Refunded", zh: "已成功退款" },
        "เช็คอินเข้าพักแล้ว": { en: "Checked In", zh: "已办理入住" },
        "เข้าพักเสร็จสมบูรณ์": { en: "Completed", zh: "已退房完成" },
        "ยกเลิกการจอง": { en: "Cancelled", zh: "已取消预订" },
        "ชำระเงินเรียบร้อย": { en: "Payment Verified", zh: "已支付完成" },
        "คืนเงินแล้ว": { en: "Refunded", zh: "已退款" },
        "รอตรวจสอบสลิปโอนเงิน": { en: "Verifying Payment Slip", zh: "待核实付款凭证" },
        "หลักฐานไม่ถูกต้อง": { en: "Invalid Payment Proof", zh: "凭证无效" },
        "รอฝ่ายบัญชีตรวจสอบ": { en: "Pending Accounting Review", zh: "待财务部审核" },
        "คืนเงินภายใน 3 วันทำการ": { en: "Refund within 3 business days", zh: "3个工作日内退还" },
        "โอนเงินคืนสำเร็จ": { en: "Refund Transferred", zh: "已成功退款" },
        "โอนคืนสำเร็จ": { en: "Refund Completed", zh: "退款成功" },
        "ผ่านบัญชีธนาคารแล้ว": { en: "Transferred via bank account", zh: "已通过银行账户退还" },
        "ปฏิเสธการคืนเงิน": { en: "Refund Rejected", zh: "退款申请被拒" },
        "เกิน 7 วันหลังจอง (หมดเขตขอคืนเงิน)": { en: "Past 7-day refund window", zh: "已超过预订后7天退款期限" },
        "เขียนรีวิว & ให้คะแนน": { en: "Write a Review & Rate", zh: "撰写评价与评分" },
        "คำขอคืนเงินส่งถึงฝ่ายบัญชีแล้ว (รอดำเนินการ)": {
            en: "Refund request submitted to Accounting (Pending)",
            zh: "退款申请已提交至财务部（处理中）"
        },
        "ฝ่ายบัญชีโอนเงินคืนเข้าบัญชีธนาคารเรียบร้อยแล้ว": {
            en: "Accounting has transferred the refund to your bank account",
            zh: "财务部已将退款转入您的银行账户"
        },
        "คำขอคืนเงินถูกปฏิเสธโดยฝ่ายบัญชี": {
            en: "Refund request was declined by Accounting",
            zh: "退款申请已被财务部拒绝"
        },
        "ยื่นคำขอคืนเงินค่าห้องพัก": { en: "Request Room Booking Refund", zh: "申请客房预订退款" },
        "รหัสอ้างอิง:": { en: "Booking Ref:", zh: "预订编号：" },
        "ห้องพัก:": { en: "Room:", zh: "客房：" },
        "วันที่จอง:": { en: "Booking Date:", zh: "预订日期：" },
        "ยอดเงินที่ขอคืนสุทธิ:": { en: "Total Refund Amount:", zh: "申请退款总额：" },
        "เงื่อนไขและกำหนดเวลาการคืนเงิน:": { en: "Refund Terms & Timeline:", zh: "退款条件与处理时限：" },
        "กรอกข้อมูลบัญชีธนาคารสำหรับรับเงินคืน": { en: "Bank Account Details for Refund", zh: "填写接收退款的银行账户信息" },
        "เลือกธนาคารผู้รับเงิน": { en: "Select Bank", zh: "选择收款银行" },
        "-- กรุณาเลือกธนาคาร --": { en: "-- Please Select Bank --", zh: "-- 请选择银行 --" },
        "ธนาคารกสิกรไทย (KBANK)": { en: "Kasikornbank (KBANK)", zh: "开泰银行 (KBANK)" },
        "ธนาคารไทยพาณิชย์ (SCB)": { en: "Siam Commercial Bank (SCB)", zh: "汇商银行 (SCB)" },
        "ธนาคารกรุงเทพ (BBL)": { en: "Bangkok Bank (BBL)", zh: "盘谷银行 (BBL)" },
        "ธนาคารกรุงไทย (KTB)": { en: "Krungthai Bank (KTB)", zh: "泰京银行 (KTB)" },
        "ธนาคารทหารไทยธนชาต (TTB)": { en: "TMBThanachart Bank (TTB)", zh: "军人ธนชาต银行 (TTB)" },
        "ธนาคารกรุงศรีอยุธยา (BAY)": { en: "Bank of Ayudhya (Krungsri / BAY)", zh: "大城银行 (BAY)" },
        "ธนาคารออมสิน (GSB)": { en: "Government Savings Bank (GSB)", zh: "政府储蓄银行 (GSB)" },
        "พร้อมเพย์ (PromptPay)": { en: "PromptPay", zh: "PromptPay" },
        "เลขที่บัญชีธนาคาร / เบอร์พร้อมเพย์": { en: "Bank Account / PromptPay Number", zh: "银行账号 / PromptPay 号码" },
        "ชื่อเจ้าของบัญชี (ตรงกับชื่อผู้รับเงิน)": { en: "Account Holder Name", zh: "账户持有人姓名" },
        "เบอร์โทรศัพท์ติดต่อ": { en: "Contact Phone Number", zh: "联系电话" },
        "เหตุผลในการขอคืนเงิน": { en: "Reason for Refund", zh: "申请退款原因" },
        "เปลี่ยนแปลงแผนการเดินทางกะทันหัน": { en: "Sudden change of travel plans", zh: "临时更改出行计划" },
        "จองวันเข้าพักผิด / ต้องการจองใหม่": { en: "Booked wrong dates / Want to rebook", zh: "选错入住日期 / 需重新预订" },
        "มีเหตุจำเป็นส่วนตัวไม่สามารถเดินทางได้": { en: "Personal emergency / Unable to travel", zh: "个人紧急情况无法出行" },
        "จองห้องพักผิดประเภท / ผิดสาขา": { en: "Booked wrong room type / wrong branch", zh: "选错房型 / 选错分店" },
        "อื่นๆ": { en: "Other", zh: "其他原因" },
        "ระบุรายละเอียดเหตุผล": { en: "Specify Reason Details", zh: "请说明具体原因" },
        "ยืนยันยื่นขอคืนเงิน": { en: "Submit Refund Request", zh: "确认提交退款申请" },
        "ให้คะแนนและรีวิวการเข้าพัก": { en: "Rate & Review Your Stay", zh: "评价您的入住体验" },
        "ความพึงพอใจโดยรวมในการเข้าพัก": { en: "Overall Stay Satisfaction", zh: "总体入住满意度" },
        "5 ดาว (ดีเยี่ยม ประทับใจมาก)": { en: "5 Stars (Excellent / Very Impressed)", zh: "5 星（极佳，非常满意）" },
        "4 ดาว (ดีมาก น่าพอใจ)": { en: "4 Stars (Very Good)", zh: "4 星（很好，比较满意）" },
        "3 ดาว (ปานกลาง)": { en: "3 Stars (Average)", zh: "3 星（一般）" },
        "2 ดาว (พอใช้)": { en: "2 Stars (Fair)", zh: "2 星（有待改进）" },
        "1 ดาว (ควรปรับปรุง)": { en: "1 Star (Needs Improvement)", zh: "1 星（不满意）" },
        "ความสะอาด": { en: "Cleanliness", zh: "清洁卫生" },
        "การบริการ": { en: "Service", zh: "服务质量" },
        "ทำเลที่ตั้ง": { en: "Location", zh: "地理位置" },
        "5/5 (ดีเยี่ยม)": { en: "5/5 (Excellent)", zh: "5/5（极佳）" },
        "4/5 (ดี)": { en: "4/5 (Good)", zh: "4/5（良好）" },
        "3/5 (ปานกลาง)": { en: "3/5 (Average)", zh: "3/5（一般）" },
        "หัวข้อรีวิวสั้นๆ": { en: "Review Title", zh: "评价标题" },
        "ข้อความรีวิวและความประทับใจ": { en: "Your Review & Feedback", zh: "评价内容与入住感受" },
        "ส่งรีวิวของคุณ": { en: "Submit Review", zh: "提交评价" },
        "ไม่พบข้อมูลการจองที่ตรงกับคำค้นหา": { en: "No bookings found matching your search", zh: "未找到符合搜索条件的预订" },
        "กรุณาตรวจสอบหมายเลขการจอง (Booking Reference) หรืออีเมล/เบอร์โทรศัพท์อีกครั้ง": {
            en: "Please double-check your Booking Reference, email, or phone number.",
            zh: "请再次核对您的预订编号、电子邮箱或电话号码。"
        },
        "คุณยังไม่มีประวัติการจองห้องพัก": { en: "You have no booking history yet", zh: "您暂无客房预订记录" },
        "เริ่มต้นค้นหาและชมพรีวิวห้องพักสุดหรูจากทั้ง 3 สาขาของเราได้เลย": {
            en: "Start exploring and booking luxury rooms across our 3 branches now.",
            zh: "立即探索并预订我们三大分店的豪华客房。"
        },
        "กรอกหมายเลขการจองเพื่อตรวจสอบสถานะ": { en: "Enter Booking Reference to Check Status", zh: "输入预订编号查询状态" },
        "หรือเข้าสู่ระบบด้วยบัญชีสมาชิกของคุณเพื่อดูประวัติการจองทั้งหมดโดยอัตโนมัติ": {
            en: "Or sign in to your member account to view all your bookings automatically.",
            zh: "或登录您的会员账户以自动查看所有预订历史。"
        },
        "ไปหน้าพรีวิวห้องพัก": { en: "Browse Room Previews", zh: "前往客房预览" },

        // --- Booking Voucher Page ---
        "เอกสารการจองฉบับสมบูรณ์": { en: "Official Booking Document", zh: "正式预订凭证" },
        "เอกสารยืนยันการจองห้องพัก (Booking Confirmation Voucher)": {
            en: "Booking Confirmation Voucher",
            zh: "酒店预订确认单 (Booking Confirmation Voucher)"
        },
        "พิมพ์เอกสาร / บันทึกเป็น PDF": { en: "Print / Save as PDF", zh: "打印凭证 / 保存为 PDF" },
        "หมายเลขการจองสำหรับแสดงต่อพนักงาน (Booking Reference)": {
            en: "Booking Reference to Present at Front Desk",
            zh: "请向前台出示此预订编号 (Booking Reference)"
        },
        "กรุณาแสดงรหัสนี้ หรือพิมพ์เอกสารฉบับนี้ยื่นต่อพนักงานต้อนรับ ณ วันที่เข้าพัก": {
            en: "Please present this reference code or printed voucher at the reception desk upon check-in.",
            zh: "请在入住当天向前台接待人员出示此编号或打印本确认单。"
        },
        "ข้อมูลผู้เข้าพัก (Guest Information)": { en: "Guest Information", zh: "住客信息 (Guest Information)" },
        "ชื่อ-นามสกุล:": { en: "Full Name:", zh: "姓名：" },
        "เบอร์โทรศัพท์ติดต่อ:": { en: "Contact Phone:", zh: "联系电话：" },
        "อีเมล:": { en: "Email:", zh: "电子邮箱：" },
        "ข้อมูลการเข้าพัก (Stay Schedule)": { en: "Stay Schedule", zh: "入住行程 (Stay Schedule)" },
        "วันที่เช็คอิน (Check-in):": { en: "Check-in Date:", zh: "入住日期 (Check-in)：" },
        "วันที่เช็คเอาท์ (Check-out):": { en: "Check-out Date:", zh: "退房日期 (Check-out)：" },
        "ระยะเวลาเข้าพัก:": { en: "Stay Duration:", zh: "入住晚数：" },
        "วันที่ทำรายการจอง:": { en: "Booking Date:", zh: "预订时间：" },
        "รายละเอียดห้องพัก (Room Details)": { en: "Room Details", zh: "客房详情 (Room Details)" },
        "ความจุ: ผู้เข้าพัก 2 ท่าน • ฟรีอาหารเช้าและสิ่งอำนวยความสะดวกตามมาตรฐานโรงแรม": {
            en: "Capacity: 2 Guests • Free breakfast and standard hotel amenities included",
            zh: "可住2人 • 含免费早餐及酒店标准配套设施"
        },
        "หมายเลขห้องพัก:": { en: "Room Number:", zh: "房间号码：" },
        "พนักงานต้อนรับจะจัดสรรห้องให้ ณ วันเช็คอิน": { en: "Assigned by front desk upon check-in", zh: "入住时由前台分配房间号" },
        "สถานะการชำระเงิน (Payment Status):": { en: "Payment Status:", zh: "付款状态 (Payment Status)：" },
        "ชำระเงินเรียบร้อยแล้ว (Paid)": { en: "Payment Completed (Paid)", zh: "已付款成功 (Paid)" },
        "รอยืนยันการตรวจสอบการชำระเงิน": { en: "Pending Payment Verification", zh: "待核实付款信息" },
        "ยอดชำระสุทธิ (Total Amount):": { en: "Total Amount:", zh: "实付总额 (Total Amount)：" },
        "ราคารวมภาษีมูลค่าเพิ่มและค่าบริการแล้ว": { en: "Inclusive of VAT and service charges", zh: "价格已含增值税及服务费" },
        "ข้อกำหนดและคำแนะนำสำหรับการเข้าพัก (Check-in Terms):": { en: "Check-in Terms & Guidelines:", zh: "入住须知与条款 (Check-in Terms)：" },
        "ช่องทาง:": { en: "Method:", zh: "支付方式：" },
        "การเช็คอิน:": { en: "Check-in Time:", zh: "入住与退房时间：" },
        "สามารถเช็คอินได้ตั้งแต่เวลา": { en: "Check-in is available from", zh: "入住办理时间为" },
        "14:00 น.": { en: "14:00", zh: "14:00" },
        "เป็นต้นไป และเช็คเอาท์ภายในเวลา": { en: "onwards, and check-out is by", zh: "起，退房时间为" },
        "12:00 น.": { en: "12:00.", zh: "12:00 前。" },
        "หลักฐานการเข้าพัก:": { en: "Required ID:", zh: "入住凭证：" },
        "กรุณาแสดงเอกสารยืนยันฉบับนี้พร้อม": { en: "Please present this confirmation voucher along with the guest's", zh: "请在前台出示本确认单以及住客的" },
        "บัตรประชาชน หรือ พาสปอร์ตฉบับจริง": { en: "original National ID Card or Passport", zh: "身份证或护照原件" },
        "ของผู้เข้าพัก ณ เคาน์เตอร์ต้อนรับ": { en: "at the front desk.", zh: "以办理入住。" },
        "การจัดห้องพัก:": { en: "Room Allocation:", zh: "房间分配：" },
        "หมายเลขห้องพักจะถูกจัดสรรจากห้องที่พร้อมให้บริการตามประเภทห้องที่ท่านจองไว้ ณ วันที่ท่านเดินทางมาถึง": {
            en: "Your room number will be assigned from available rooms matching your booked room type upon arrival.",
            zh: "您的具体房间号将在您抵达酒店当天，根据所预订房型的空房情况进行分配。"
        },
        "การยกเลิก / ขอคืนเงิน:": { en: "Cancellation / Refund:", zh: "取消与退款：" },
        "สามารถยื่นเรื่องขอคืนเงินได้ภายใน 7 วันหลังจากการจองผ่านหน้าเว็บไซต์": {
            en: "Refund requests can be submitted online within 7 days of booking.",
            zh: "可在预订后7天内通过网站在线提交退款申请。"
        },
        "ลายมือชื่อผู้เข้าพัก (Guest Signature)": { en: "Guest Signature", zh: "住客签名 (Guest Signature)" },
        "ลายมือชื่อพนักงานต้อนรับ (Staff Signature)": { en: "Staff Signature", zh: "前台接待签名 (Staff Signature)" },
        "วันที่: _____ / _____ / _________": { en: "Date: _____ / _____ / _________", zh: "日期：_____ / _____ / _________" },
        "จำนวนห้องพักที่ต้องการจองพร้อมกัน": { en: "Number of Rooms to Book Together", zh: "同时预订房间数量" },
        "สามารถเพิ่มจำนวนห้องประเภทนี้ หรือกดเพิ่มห้องพักประเภทอื่นเพื่อจองพร้อมกันในครั้งเดียว": {
            en: "Increase the quantity of this room type or click 'Add Another Room' to book multiple rooms at once.",
            zh: "可增加当前房型数量，或点击“添加其他房间”一次性预订多间客房。"
        },
        "จองห้องพักเพิ่ม": { en: "Add Another Room", zh: "添加其他房间" },
        "+ ห้องเสริม": { en: "+ Extra Room", zh: "+ 附加客房" },
        "จำนวน:": { en: "Qty:", zh: "数量：" },
        "สิทธิพิเศษจอง 2 ห้องขึ้นไป!": { en: "2+ Rooms Group Privilege!", zh: "预订 2 间及以上专属优惠！" },
        "รับส่วนลดกลุ่มทันที": { en: "Get an instant group discount of", zh: "立享团队优惠" },
        "15% ทุกห้อง": { en: "15% off all rooms", zh: "所有房间 85 折 (-15%)" },
        "ด้วยโค้ด": { en: "with code", zh: "使用优惠码" },
        "ใช้โค้ด GROUP15 ทันที": { en: "Apply GROUP15 Now", zh: "立即使用 GROUP15" },
        "หมายเลขการจองหลัก (Booking Reference)": { en: "Primary Booking Reference", zh: "主预订编号 (Booking Reference)" },

        // --- Authentication (Login / Register / Profile / Forgot Password) ---
        "ยินดีต้อนรับสู่ Easy Stay Hotel": { en: "Welcome to Easy Stay Hotel", zh: "欢迎来到 Easy Stay 酒店" },
        "เข้าสู่ระบบเพื่อจัดการข้อมูลการจองห้องพัก รับข้อเสนอพิเศษเฉพาะสมาชิก และเพลิดเพลินกับการพักผ่อนระดับพรีเมียม": {
            en: "Sign in to manage your bookings, unlock member-only rates, and enjoy a premium stay.",
            zh: "登录以管理您的客房预订、获取会员专属特惠，尽享尊贵住宿体验。"
        },
        "ส่วนลดเดือนเกิด 10%": { en: "10% Birthday Month Discount", zh: "生日当月 9折优惠" },
        "รับสิทธิ์อัตโนมัติในเดือนเกิดของคุณ": { en: "Automatically applied during your birthday month", zh: "在您生日当月预订自动享受折扣" },
        "ระบุวันเกิดเพื่อรับส่วนลดอัตโนมัติในเดือนเกิด": { en: "Enter your birth date for automatic birthday month discount", zh: "填写出生日期以在生日月自动享9折" },
        "จัดการการจองสะดวก 24 ชม.": { en: "24/7 Booking Management", zh: "24小时便捷管理预订" },
        "ตรวจสอบสถานะห้องพักและสลิปการโอน": { en: "Track room status and payment slips anytime", zh: "随时查看客房状态与付款凭证" },
        "ความปลอดภัยมาตรฐาน": { en: "Enterprise Security", zh: "高标准安全保障" },
        "ระบบยืนยันตัวตนด้วยรหัส OTP ทางอีเมล": { en: "Secure email OTP authentication system", zh: "通过电子邮箱 OTP 验证码进行安全验证" },
        "สิทธิ์ใช้คูปองส่วนลด": { en: "Exclusive Discount Coupons", zh: "专属优惠券权益" },
        "ใช้โค้ด EARLY30 (-15%) และ GROUP15": { en: "Use codes EARLY30 (-15%) and GROUP15", zh: "可使用 EARLY30 (-15%) 与 GROUP15" },
        "ยืนยันอีเมลด้วย OTP": { en: "Email OTP Verification", zh: "邮箱 OTP 安全验证" },
        "ป้องกันบัญชีปลอมด้วยรหัสความปลอดภัย": { en: "Protect your account with one-time security codes", zh: "通过安全验证码保护账户安全" },
        "บันทึกประวัติการเข้าพัก": { en: "Stay History & Vouchers", zh: "保存入住与预订记录" },
        "ติดตามสถานะและตรวจสอบใบเสร็จออนไลน์": { en: "Track booking status and view online receipts", zh: "在线跟踪预订状态并查看收据凭证" },
        "เข้าสู่ระบบสมาชิก": { en: "Member Sign In", zh: "会员登录" },
        "กรอกอีเมลและรหัสผ่านเพื่อเข้าสู่บัญชีผู้ใช้งานของคุณ": {
            en: "Enter your registered email and password to access your account.",
            zh: "请输入您的注册邮箱和密码以登录您的账户。"
        },
        "อีเมลที่ลงทะเบียน": { en: "Registered Email", zh: "注册邮箱" },
        "รหัสผ่าน": { en: "Password", zh: "密码" },
        "จดจำการเข้าสู่ระบบ": { en: "Remember me", zh: "记住登录状态" },
        "ลืมรหัสผ่าน?": { en: "Forgot password?", zh: "忘记密码？" },
        "เข้าสู่ระบบ (Sign In)": { en: "Sign In", zh: "登录 (Sign In)" },
        "ยังไม่มีบัญชีสมาชิกใช่หรือไม่?": { en: "Don't have a member account yet?", zh: "还没有会员账户？" },
        "สมัครสมาชิกใหม่": { en: "Create New Account", zh: "注册新会员" },
        "สมัครสมาชิก Easy Stay": { en: "Join Easy Stay Membership", zh: "注册成为 Easy Stay 会员" },
        "สมัครสมาชิกง่ายๆ ภายในไม่กี่นาที ยืนยันอีเมลด้วยรหัส OTP เพื่อความปลอดภัย พร้อมรับสิทธิประโยชน์สุดพิเศษทันที": {
            en: "Sign up in minutes with secure email OTP verification and unlock instant member rewards.",
            zh: "几分钟即可轻松完成注册，通过邮箱 OTP 安全验证，立即尊享会员专属礼遇。"
        },
        "ลงทะเบียนสมาชิกใหม่": { en: "Create Member Account", zh: "注册新会员账户" },
        "กรอกข้อมูลให้ครบถ้วนเพื่อรับรหัส OTP ยืนยันอีเมลของคุณ": {
            en: "Complete the form below to receive an email OTP verification code.",
            zh: "请完整填写以下信息以获取邮箱 OTP 验证码。"
        },
        "ยืนยันด้วย OTP": { en: "OTP Verified", zh: "OTP 安全验证" },
        "ชื่อ-นามสกุลจริง *": { en: "Full Name *", zh: "真实姓名 *" },
        "อีเมล (ใช้สำหรับเข้าสู่ระบบและรับ OTP) *": { en: "Email Address (for login & OTP) *", zh: "电子邮箱（用于登录及接收验证码）*" },
        "วันเดือนปีเกิด": { en: "Date of Birth", zh: "出生日期" },
        "ตั้งรหัสผ่าน (อย่างน้อย 6 ตัวอักษร) *": { en: "Password (at least 6 characters) *", zh: "设置密码（至少6位字符）*" },
        "ยืนยันรหัสผ่านอีกครั้ง *": { en: "Confirm Password *", zh: "确认密码 *" },
        "ส่งรหัส OTP และดำเนินการสมัครสมาชิก": { en: "Send OTP & Complete Registration", zh: "发送 OTP 验证码并完成注册" },
        "มีบัญชีสมาชิก Easy Stay อยู่แล้ว?": { en: "Already have an Easy Stay account?", zh: "已有 Easy Stay 会员账户？" },
        "เข้าสู่ระบบที่นี่": { en: "Sign In Here", zh: "在此登录" },
        "ยืนยันรหัส OTP จากอีเมล": { en: "Verify Email OTP Code", zh: "验证邮箱 OTP 验证码" },
        "กรุณากรอกรหัสยืนยัน 6 หลัก": { en: "Enter 6-Digit Verification Code", zh: "请输入6位数字验证码" },
        "เราได้ส่งรหัส OTP ไปยังอีเมล:": { en: "We have sent an OTP code to:", zh: "我们已将 OTP 验证码发送至：" },
        "รหัสหมดอายุใน:": { en: "Code expires in:", zh: "验证码有效期剩余：" },
        "ส่งรหัสใหม่อีกครั้ง": { en: "Resend OTP Code", zh: "重新发送验证码" },
        "ยืนยันและเปิดใช้งานบัญชี": { en: "Verify & Activate Account", zh: "确认并激活账户" },
        "แก้ไขข้อมูลการสมัคร": { en: "Edit Registration Info", zh: "返回修改注册信息" },
        "จัดการข้อมูลส่วนตัว": { en: "Manage Personal Profile", zh: "管理个人资料" },
        "แก้ไขข้อมูลส่วนบุคคล เบอร์โทรศัพท์ และตั้งรหัสผ่านใหม่เพื่อความปลอดภัย": {
            en: "Update your personal details, phone number, and security password.",
            zh: "更新您的个人信息、联系电话及安全密码。"
        },
        "โปรไฟล์ผู้ใช้งาน": { en: "User Profile", zh: "用户资料" },
        "ข้อมูลส่วนบุคคล": { en: "Personal Info", zh: "个人信息" },
        "ความปลอดภัย & เปลี่ยนรหัสผ่าน": { en: "Security & Password", zh: "安全与修改密码" },
        "ข้อมูลประจำตัวผู้ใช้งาน": { en: "Account Identity Details", zh: "用户账户资料" },
        "อีเมลประจำบัญชี (ไม่สามารถเปลี่ยนแปลงได้)": { en: "Account Email (Cannot be changed)", zh: "账户绑定邮箱（不可修改）" },
        "อีเมลนี้ใช้สำหรับการเข้าสู่ระบบและการรับใบเสร็จยืนยันการจอง": {
            en: "This email is used for sign-in and receiving booking confirmations.",
            zh: "此邮箱用于登录账户及接收预订确认单。"
        },
        "เลขบัตรประชาชน / หนังสือเดินทาง": { en: "National ID / Passport Number", zh: "身份证号 / 护照号码" },
        "เบอร์ติดต่อ": { en: "Phone", zh: "联系电话" },
        "วันเกิด": { en: "Birthday", zh: "出生日期" },
        "เลขประจำตัว/Passport": { en: "ID / Passport", zh: "证件/护照号" },
        "ยังไม่ระบุ": { en: "Not specified", zh: "未填写" },
        "สิทธิประโยชน์สมาชิก": { en: "Member Privileges", zh: "会员专属权益" },
        "สมาชิก Easy Stay รับสิทธิ์ส่วนลดพิเศษ 10% ในเดือนเกิดของคุณอัตโนมัติ และสะสมสถิติการเข้าพักในเครือทุกสาขา": {
            en: "Easy Stay members enjoy an automatic 10% discount during their birthday month across all branches.",
            zh: "Easy Stay 会员在生日当月自动享9折优惠，并可在所有分店累计入住记录。"
        },
        "เปลี่ยนรหัสผ่านเข้าสู่ระบบ": { en: "Change Sign-In Password", zh: "修改登录密码" },
        "หากไม่ต้องการเปลี่ยนรหัสผ่าน ให้เว้นว่างช่องเหล่านี้ไว้": {
            en: "Leave these fields blank if you do not wish to change your password.",
            zh: "如无需修改密码，请将以下栏位留空。"
        },
        "รหัสผ่านปัจจุบัน": { en: "Current Password", zh: "当前密码" },
        "รหัสผ่านใหม่": { en: "New Password", zh: "新密码" },
        "ยืนยันรหัสผ่านใหม่": { en: "Confirm New Password", zh: "确认新密码" },
        "คำแนะนำด้านความปลอดภัย:": { en: "Security Recommendations:", zh: "安全建议：" },
        "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษรขึ้นไป": { en: "Password must be at least 6 characters long.", zh: "密码长度应至少为6位字符。" },
        "ควรใช้ตัวอักษรพิมพ์เล็ก พิมพ์ใหญ่ และตัวเลขผสมกัน": { en: "Use a mix of uppercase, lowercase letters, and numbers.", zh: "建议混合使用大小写字母与数字。" },
        "ตั้งรหัสผ่านใหม่": { en: "Reset Password", zh: "重置密码" },
        "ลืมรหัสผ่านบัญชี?": { en: "Forgot Your Password?", zh: "忘记账户密码？" },
        "ไม่ต้องกังวล คุณสามารถตั้งรหัสผ่านใหม่ได้อย่างปลอดภัยและรวดเร็วผ่านการยืนยันรหัส OTP ทางอีเมลที่ลงทะเบียนไว้": {
            en: "No worries! Quickly and securely reset your password via email OTP verification.",
            zh: "别担心！您可以通过注册邮箱接收 OTP 验证码，安全快速地重置密码。"
        },
        "กรอกอีเมลที่ลงทะเบียน": { en: "Enter Registered Email", zh: "输入注册邮箱" },
        "ระบบจะตรวจสอบและส่งรหัส OTP ไปที่กล่องข้อความของคุณ": { en: "We will verify and send an OTP code to your inbox", zh: "系统将核实并向您的邮箱发送 OTP 验证码" },
        "รับรหัส OTP 6 หลัก": { en: "Receive 6-Digit OTP", zh: "接收6位 OTP 验证码" },
        "รหัสยืนยันมีอายุ 5 นาที เพื่อความปลอดภัยของบัญชี": { en: "Verification code is valid for 5 minutes", zh: "验证码有效期为5分钟，保障账户安全" },
        "ตั้งรหัสผ่านใหม่และเข้าสู่ระบบ": { en: "Set New Password & Sign In", zh: "设置新密码并登录" },
        "กำหนดรหัสผ่านใหม่ได้ทันทีเพื่อใช้งานระบบได้อย่างต่อเนื่อง": { en: "Set your new password and continue immediately", zh: "立即设置新密码以继续使用系统" },
        "กรอกอีเมลของท่านเพื่อขอรับรหัสยืนยัน OTP ในการรีเซ็ตรหัสผ่าน": {
            en: "Enter your email to receive an OTP verification code for password reset.",
            zh: "请输入您的电子邮箱以获取重置密码所需的 OTP 验证码。"
        },
        "ขอรับ OTP": { en: "Get OTP", zh: "获取验证码" },
        "กรอกรหัสยืนยัน OTP และรหัสผ่านใหม่": { en: "Enter OTP Code & New Password", zh: "输入 OTP 验证码与新密码" },
        "รหัส OTP 6 หลัก จากอีเมล": { en: "6-Digit OTP from Email", zh: "邮箱收到的6位 OTP 验证码" },
        "ยืนยันและบันทึกรหัสผ่านใหม่": { en: "Confirm & Save New Password", zh: "确认并保存新密码" },
        "นึกรหัสผ่านได้แล้ว? กลับไปหน้าเข้าสู่ระบบ": { en: "Remembered your password? Back to Sign In", zh: "想起密码了？返回登录页" },
        "ดำเนินการสำเร็จ": { en: "Success", zh: "操作成功" },
        "เกิดข้อผิดพลาด": { en: "Error Occurred", zh: "发生错误" }
    };

    // 2. คลังแปล Placeholder ของช่อง Input & Textarea
    const PLACEHOLDER_DICT = {
        "ระบุชื่อ-นามสกุลผู้เข้าพัก": { en: "Enter guest full name", zh: "请输入住客真实姓名" },
        "กรอกรหัสคูปอง เช่น GROUP15": { en: "Enter promo code e.g. GROUP15", zh: "输入优惠码，如 GROUP15" },
        "เช่น ES-20261116...": { en: "e.g. ES-20261116...", zh: "例如 ES-20261116..." },
        "เช่น email@example.com หรือ 08X-XXX-XXXX": { en: "e.g. email@example.com or phone", zh: "例如邮箱或手机号码" },
        "ระบุรหัสผ่านของคุณ": { en: "Enter your password", zh: "请输入您的密码" },
        "เช่น นายสมชาย ใจดี": { en: "e.g. John Doe", zh: "例如：张三 / John Doe" },
        "ระบุชื่อและนามสกุล": { en: "Enter full name", zh: "请输入姓名" },
        "ระบุเลขบัตรประชาชน 13 หลัก หรือ Passport": { en: "Enter ID Card or Passport Number", zh: "请输入身份证号或护照号码" },
        "ระบุรหัสผ่านปัจจุบันของคุณ": { en: "Enter current password", zh: "请输入当前密码" },
        "อย่างน้อย 6 ตัวอักษร": { en: "At least 6 characters", zh: "至少6位字符" },
        "ระบุอีกครั้ง": { en: "Re-enter password", zh: "请再次输入" },
        "กรอกรหัสผ่านใหม่อีกครั้ง": { en: "Re-enter new password", zh: "请再次输入新密码" },
        "ต่ำสุด": { en: "Min", zh: "最低" },
        "สูงสุด": { en: "Max", zh: "最高" },
        "เช่น 012-3-45678-9 หรือ เบอร์โทรศัพท์": { en: "e.g. 012-3-45678-9 or phone number", zh: "例如银行账号或手机号" },
        "เช่น 0812345678": { en: "e.g. 0812345678", zh: "例如 0812345678" },
        "กรุณาระบุรายละเอียดหรือเหตุผลเพิ่มเติมในการขอคืนเงิน...": {
            en: "Please provide additional details for your refund request...",
            zh: "请详细说明您申请退款的原因..."
        },
        "เช่น ห้องพักกว้างขวาง วิวสวย เตียงนุ่มสบายมาก": {
            en: "e.g. Spacious room, great view, very comfortable bed",
            zh: "例如：房间宽敞、视野极佳、床铺非常舒适"
        },
        "บอกเล่าความรู้สึกและสิ่งที่ท่านประทับใจ เพื่อเป็นข้อมูลให้กับผู้เข้าพักท่านอื่น...": {
            en: "Share your experience and feedback to help future guests...",
            zh: "分享您的入住体验与感受，为其他住客提供参考..."
        }
    };

    // 3. แปลงชื่อเดือนภาษาไทยเป็น EN / ZH สำหรับวันที่ เช่น "24 พ.ย. 2569" หรือ "24 พ.ย. 2026"
    const THAI_MONTHS_MAP = {
        "ม.ค.": { en: "Jan", zh: "1月" },
        "ก.พ.": { en: "Feb", zh: "2月" },
        "มี.ค.": { en: "Mar", zh: "3月" },
        "เม.ย.": { en: "Apr", zh: "4月" },
        "พ.ค.": { en: "May", zh: "5月" },
        "มิ.ย.": { en: "Jun", zh: "6月" },
        "ก.ค.": { en: "Jul", zh: "7月" },
        "ส.ค.": { en: "Aug", zh: "8月" },
        "ก.ย.": { en: "Sep", zh: "9月" },
        "ต.ค.": { en: "Oct", zh: "10月" },
        "พ.ย.": { en: "Nov", zh: "11月" },
        "ธ.ค.": { en: "Dec", zh: "12月" }
    };

    // 4. กฎแปลข้อความแบบมีตัวเลขหรือตัวแปรไดนามิก (Regex Patterns)
    const REGEX_RULES = [
        {
            regex: /^ว่างรวม\s+(\d+)\s+ห้อง$/,
            en: (_, n) => `${n} Total Room(s) Left`,
            zh: (_, n) => `共剩 ${n} 间空房`
        },
        {
            regex: /^ว่าง\s+(\d+)\s+ห้อง$/,
            en: (_, n) => `${n} Room(s) Available`,
            zh: (_, n) => `剩余 ${n} 间空房`
        },
        {
            regex: /^ว่าง\s+(\d+)\s+ห้องในสาขานี้$/,
            en: (_, n) => `${n} Room(s) Available in Branch`,
            zh: (_, n) => `本分店剩余 ${n} 间空房`
        },
        {
            regex: /^จองพร้อมกัน\s+(\d+)\s+ห้อง$/,
            en: (_, n) => `Booked ${n} Rooms Together`,
            zh: (_, n) => `同时预订 ${n} 间客房`
        },
        {
            regex: /^ห้องที่\s+(\d+)$/,
            en: (_, n) => `Room #${n}`,
            zh: (_, n) => `第 ${n} 间房`
        },
        {
            regex: /^\((\d+)\s+คืน,\s*รวม\s+(\d+)\s+ห้อง\)$/,
            en: (_, n, r) => `(${n} Night(s), Total ${r} Rooms)`,
            zh: (_, n, r) => `(${n} 晚，共 ${r} 间房)`
        },
        {
            regex: /^(\d+)\s+ห้องว่าง$/,
            en: (_, n) => `${n} Available`,
            zh: (_, n) => `${n} 间空房`
        },
        {
            regex: /^ว่าง\s+(\d+)$/,
            en: (_, n) => `${n} Left`,
            zh: (_, n) => `剩 ${n} 间`
        },
        {
            regex: /^(\d+)\s+ห้อง$/,
            en: (_, n) => `${n} Room(s)`,
            zh: (_, n) => `${n} 间`
        },
        {
            regex: /^ห้อง\s+([A-Za-z0-9-]+)$/,
            en: (_, r) => `Room ${r}`,
            zh: (_, r) => `${r} 房`
        },
        {
            regex: /^(\d+)\s+คืน$/,
            en: (_, n) => `${n} Night(s)`,
            zh: (_, n) => `${n} 晚`
        },
        {
            regex: /^\((\d+)\s+คืน\)$/,
            en: (_, n) => `(${n} Night(s))`,
            zh: (_, n) => `(${n} 晚)`
        },
        {
            regex: /^\((\d+)\s+คืน,\s*(\d+)\s+ห้อง\)$/,
            en: (_, n, r) => `(${n} Night(s), ${r} Room(s))`,
            zh: (_, n, r) => `(${n} 晚, ${r} 间)`
        },
        {
            regex: /^\((\d+)\s+คืน,\s*แขก\s+(\d+)\s+ท่าน\)$/,
            en: (_, n, g) => `(${n} Night(s), ${g} Guest(s))`,
            zh: (_, n, g) => `(${n} 晚, ${g} 位住客)`
        },
        {
            regex: /^(\d+)\s+ท่าน$/,
            en: (_, n) => `${n} Guest(s)`,
            zh: (_, n) => `${n} 位`
        },
        {
            regex: /^รองรับสูงสุด\s+(\d+)\s+ท่าน$/,
            en: (_, n) => `Max ${n} Guests`,
            zh: (_, n) => `最多可住 ${n} 位`
        },
        {
            regex: /^รองรับ\s+(\d+)\s+ท่าน$/,
            en: (_, n) => `Max ${n} Guests`,
            zh: (_, n) => `可住 ${n} 位`
        },
        {
            regex: /^สูงสุด\s+(\d+)\s+ท่าน$/,
            en: (_, n) => `Up to ${n} Guests`,
            zh: (_, n) => `最多 ${n} 位`
        },
        {
            regex: /^ขนาด\s+(\d+)\s*ตร\.ม\.$/,
            en: (_, n) => `Size ${n} sq.m.`,
            zh: (_, n) => `面积 ${n} 平方米`
        },
        {
            regex: /^(\d+)\s*ตร\.ม\.$/,
            en: (_, n) => `${n} sq.m.`,
            zh: (_, n) => `${n} 平方米`
        },
        {
            regex: /^ดูพรีวิว\s*\((\d+)\s*ภาพ\)$/,
            en: (_, n) => `Preview (${n} Photos)`,
            zh: (_, n) => `查看预览 (${n} 张图)`
        },
        {
            regex: /^ดูภาพพรีวิว\s*\((\d+)\s*รูป\)$/,
            en: (_, n) => `Preview (${n} Photos)`,
            zh: (_, n) => `查看图片 (${n} 张)`
        },
        {
            regex: /^\((\d+)\s*รีวิว\)$/,
            en: (_, n) => `(${n} Reviews)`,
            zh: (_, n) => `(${n} 条评价)`
        },
        {
            regex: /^พบรายการจองทั้งหมด\s+(\d+)\s+รายการ$/,
            en: (_, n) => `Found ${n} Booking(s)`,
            zh: (_, n) => `共找到 ${n} 条预订记录`
        },
        {
            regex: /^ขอคืนเงิน\s*\(เหลือ\s*(\d+)\s*วัน\)$/,
            en: (_, n) => `Request Refund (${n}d left)`,
            zh: (_, n) => `申请退款（剩 ${n} 天）`
        },
        {
            regex: /^รีวิวแล้ว\s*\((\d+)\/5\s*ดาว\)$/,
            en: (_, n) => `Reviewed (${n}/5 Stars)`,
            zh: (_, n) => `已评价 (${n}/5 星)`
        },
        {
            regex: /^บริการเสริม:\s*เตียงเสริม\s*\((\d+)\s*คืน\)$/,
            en: (_, n) => `Add-on: Extra Bed (${n} Night(s))`,
            zh: (_, n) => `附加服务：加床 (${n} 晚)`
        },
        {
            regex: /^บริการเสริม:\s*อาหารเช้า\s*\((\d+)\s*คืน\)$/,
            en: (_, n) => `Add-on: Breakfast (${n} Night(s))`,
            zh: (_, n) => `附加服务：自助早餐 (${n} 晚)`
        },
        {
            regex: /^รวม\s+(\d+)\s+คืน:\s*(฿[\d,]+(?:\.\d+)?)\s*\(ก่อนหักส่วนลดโปรโมชั่น\)$/,
            en: (_, n, p) => `Total ${n} Night(s): ${p} (Before promo discount)`,
            zh: (_, n, p) => `共 ${n} 晚：${p}（折扣前总额）`
        },
        {
            regex: /^ยอดรวมค่าห้องพัก\s*\((\d+)\s*คืน\)$/,
            en: (_, n) => `Room Total (${n} Night(s))`,
            zh: (_, n) => `房费小计 (${n} 晚)`
        },
        {
            regex: /^จองห้องพักประเภท\s+(.+)\s+ทันที$/,
            en: (_, rt) => `Quick Book: ${translateCore(rt, 'en')}`,
            zh: (_, rt) => `立即预订：${translateCore(rt, 'zh')}`
        },
        {
            regex: /^กรุงเทพมหานคร\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `Bangkok (${n} Available)`,
            zh: (_, n) => `曼谷 (剩 ${n} 间)`
        },
        {
            regex: /^ชลบุรี\s*-\s*พัทยา\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `Chonburi - Pattaya (${n} Available)`,
            zh: (_, n) => `春武里 - 芭堤雅 (剩 ${n} 间)`
        },
        {
            regex: /^ภูเก็ต\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `Phuket (${n} Available)`,
            zh: (_, n) => `普吉岛 (剩 ${n} 间)`
        },
        {
            regex: /^ดูสาขากรุงเทพฯ\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `View Bangkok (${n} Available)`,
            zh: (_, n) => `查看曼谷分店 (剩 ${n} 间)`
        },
        {
            regex: /^ดูสาขาชลบุรี-พัทยา\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `View Chonburi-Pattaya (${n} Available)`,
            zh: (_, n) => `查看芭堤雅分店 (剩 ${n} 间)`
        },
        {
            regex: /^ดูสาขาภูเก็ต\s*\(ว่าง\s*(\d+)\s*ห้อง\)$/,
            en: (_, n) => `View Phuket (${n} Available)`,
            zh: (_, n) => `查看普吉岛分店 (剩 ${n} 间)`
        },
        {
            regex: /^ไม่พบห้องว่างในสาขา\s+(.+)\s+สำหรับวันที่ท่านเลือก$/,
            en: (_, b) => `No available rooms at ${translateCore(b, 'en')} for selected dates`,
            zh: (_, b) => `所选日期在${translateCore(b, 'zh')}暂无空房`
        },
        {
            regex: /^(\d{1,2})\s+(ม\.ค\.|ก\.พ\.|มี\.ค\.|เม\.ย\.|พ\.ค\.|มิ\.ย\.|ก\.ค\.|ส\.ค\.|ก\.ย\.|ต\.ค\.|พ\.ย\.|ธ\.ค\.)\s+(\d{4})$/,
            en: (_, d, m, y) => {
                const yr = parseInt(y, 10) > 2400 ? parseInt(y, 10) - 543 : y;
                return `${d} ${THAI_MONTHS_MAP[m].en} ${yr}`;
            },
            zh: (_, d, m, y) => {
                const yr = parseInt(y, 10) > 2400 ? parseInt(y, 10) - 543 : y;
                return `${yr}年${THAI_MONTHS_MAP[m].zh}${d}日`;
            }
        },
        {
            regex: /^สาขา\s+(.+)$/,
            en: (_, b) => `${translateCore(b, 'en')} Branch`,
            zh: (_, b) => `${translateCore(b, 'zh')}分店`
        },
        {
            regex: /^คุณ\s*(.+)$/,
            en: (_, name) => `${name}`,
            zh: (_, name) => `${name}`
        },
        {
            regex: /^สถานะ:\s*(.+)$/,
            en: (_, r) => `Role: ${r}`,
            zh: (_, r) => `身份：${r}`
        },
        {
            regex: /^สิทธิ์:\s*(.+)$/,
            en: (_, r) => `Role: ${r}`,
            zh: (_, r) => `权限：${r}`
        },
        {
            regex: /^ช่องทาง:\s*(.+)$/,
            en: (_, m) => `Method: ${m}`,
            zh: (_, m) => `支付方式：${m}`
        },
        {
            regex: /^คัดลอก\s+(.+)\s+แล้ว!$/,
            en: (_, c) => `Copied ${c}!`,
            zh: (_, c) => `已复制 ${c}！`
        }
    ];

    // 5. คำศัพท์ย่อยสำหรับประโยคผสมหรือคำบรรยายห้องในฐานข้อมูล (Fragment Replacement)
    const PHRASE_FRAGMENTS = [
        { th: "เมื่อกดปุ่มด้านล่าง ระบบจะส่งรหัส", en: "When you click below, the system will send a", zh: "点击下方按钮后，系统将发送" },
        { th: "OTP 6 หลัก", en: "6-digit OTP code", zh: "6位 OTP 验证码" },
        { th: "ไปยังอีเมลของท่านเพื่อยืนยันความเป็นเจ้าของอีเมลก่อนเปิดใช้งานบัญชี", en: "to your email to verify ownership before activating your account.", zh: "至您的邮箱，以验证邮箱所有权并激活账户。" },
        { th: "หากคุณลืมรหัสผ่านปัจจุบัน สามารถใช้ฟังก์ชัน \"ลืมรหัสผ่าน\" ในหน้าเข้าสู่ระบบเพื่อยืนยัน OTP ทางอีเมล", en: "If you forgot your current password, use 'Forgot Password' on the sign-in page to reset via email OTP.", zh: "如忘记当前密码，可在登录页使用“忘记密码”通过邮箱 OTP 重置。" },
        { th: "โปรโมชั่น Early Bird จองล่วงหน้า >30 วัน", en: "Early Bird (>30 days advance)", zh: "早鸟优惠（提前30天以上）" },
        { th: "โปรโมชั่นพักยาวคุ้มกว่า", en: "Long Stay Promo", zh: "连住特惠" },
        { th: "โปรโมชั่นต้อนรับเดือนเกิด", en: "Birthday Month Promo", zh: "生日月礼遇" },
        { th: "โปรโมชั่นวันธรรมดาน่าพัก Deluxe+", en: "Weekday Special Deluxe+", zh: "工作日豪华房特惠" },
        { th: "เข้าพักแบบกลุ่ม >=2 ห้อง", en: "Group Stay (>=2 Rooms)", zh: "团队预订（>=2间）" },
        { th: "ผลการค้นหา: พบประเภทห้องว่าง", en: "Search Results: Found", zh: "搜索结果：找到" },
        { th: "กำลังดูสาขา:", en: "Viewing Branch:", zh: "当前分店：" },
        { th: "กรุงเทพมหานคร", en: "Bangkok", zh: "曼谷" },
        { th: "กรุงเทพฯ", en: "Bangkok", zh: "曼谷" },
        { th: "ชลบุรี (พัทยา)", en: "Chonburi (Pattaya)", zh: "春武里 (芭堤雅)" },
        { th: "ชลบุรี-พัทยา", en: "Chonburi-Pattaya", zh: "春武里-芭堤雅" },
        { th: "ชลบุรี", en: "Chonburi", zh: "春武里" },
        { th: "ภูเก็ต", en: "Phuket", zh: "普吉岛" },
        { th: "บริการห้องพักมาตรฐานระดับพรีเมียม", en: "Premium Standard Hospitality", zh: "尊贵标准酒店住宿服务" },
        { th: "วันที่ออกเอกสาร:", en: "Issued Date:", zh: "签发日期：" },
        { th: "วันที่ทำรายการ:", en: "Transaction Date:", zh: "交易时间：" },
        { th: "รหัสอ้างอิง:", en: "Reference:", zh: "参考编号：" },
        { th: "ค่าห้องพัก", en: "Room Charge", zh: "客房费用" },
        { th: "ผู้เข้าพัก:", en: "Guest:", zh: "住客：" },
        { th: "ฟรี Wi-Fi และอาหารเช้า", en: "Free Wi-Fi & Breakfast", zh: "免费 Wi-Fi 及早餐" },
        { th: "โค้ดกลุ่ม:", en: "Group Code:", zh: "团队码：" },
        { th: "โค้ด:", en: "Code:", zh: "优惠码：" },
        { th: "(คลิกคัดลอก)", en: "(Click to copy)", zh: "（点击复制）" },
        { th: "ห้องขนาดใหญ่", en: "Spacious room", zh: "宽敞客房" },
        { th: "ห้องพักสุดหรู", en: "Luxury room", zh: "奢华客房" },
        { th: "พร้อมระเบียง", en: "with private balcony", zh: "配私人阳台" },
        { th: "วิวทะเล", en: "with ocean view", zh: "海景" },
        { th: "คืนที่ 3", en: "3rd night", zh: "第3晚" },
        { th: "สาขา", en: "Branch ", zh: "分店 " }
    ];

    function translateCore(trimmed, lang) {
        if (!trimmed || lang === 'th') return trimmed;

        // รองรับข้อความที่มีเครื่องหมายคำพูด "..." ครอบอยู่ (เช่น ข้อความรีวิวลูกค้าในหน้าแรก)
        if (trimmed.length > 2 && trimmed.startsWith('"') && trimmed.endsWith('"')) {
            const inner = trimmed.slice(1, -1).trim();
            const translatedInner = translateCore(inner, lang);
            return `"${translatedInner}"`;
        }

        // 1. ตรวจสอบจาก Exact Dictionary
        if (EXACT_DICT[trimmed] && EXACT_DICT[trimmed][lang]) {
            return EXACT_DICT[trimmed][lang];
        }

        // 2. ตรวจสอบจาก Regex Rules
        for (let i = 0; i < REGEX_RULES.length; i++) {
            const rule = REGEX_RULES[i];
            if (rule.regex.test(trimmed)) {
                return trimmed.replace(rule.regex, rule[lang]);
            }
        }

        // 3. ตรวจสอบคำผสมย่อย (Fragment fallback)
        if (/[\u0E00-\u0E7F]/.test(trimmed)) {
            let result = trimmed;
            let changed = false;
            for (let i = 0; i < PHRASE_FRAGMENTS.length; i++) {
                const frag = PHRASE_FRAGMENTS[i];
                if (result.includes(frag.th)) {
                    result = result.split(frag.th).join(frag[lang]);
                    changed = true;
                }
            }
            if (changed) return result;
        }

        return trimmed;
    }

    // ฟังก์ชันแปล Text Node ทั้งหมดในหน้าเว็บโดยไม่กระทบ HTML Tags หรือไอคอน
    function applyLanguageToDOM(lang) {
        const info = SUPPORTED_LANGS[lang] || SUPPORTED_LANGS.th;
        document.documentElement.setAttribute('lang', info.htmlLang);

        // อัปเดตปุ่มบน Navbar
        const badgeEl = document.getElementById('currentLangBadge');
        const labelEl = document.getElementById('currentLangLabel');
        if (badgeEl) badgeEl.textContent = info.code;
        if (labelEl) labelEl.textContent = info.label;

        document.querySelectorAll('.lang-select-item').forEach(btn => {
            const itemLang = btn.getAttribute('data-lang');
            btn.classList.toggle('active-lang', itemLang === lang);
        });
        document.querySelectorAll('.lang-check-icon').forEach(icon => {
            const iconLang = icon.getAttribute('data-check-lang');
            icon.classList.toggle('d-none', iconLang !== lang);
        });

        // 1. แปล Text Nodes
        const walker = document.createTreeWalker(
            document.body,
            NodeFilter.SHOW_TEXT,
            {
                acceptNode: function (node) {
                    const parent = node.parentElement;
                    if (!parent) return NodeFilter.FILTER_REJECT;
                    const tag = parent.tagName;
                    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT' || tag === 'CODE' || tag === 'TEXTAREA') {
                        return NodeFilter.FILTER_REJECT;
                    }
                    if (parent.closest('.notranslate')) {
                        return NodeFilter.FILTER_REJECT;
                    }
                    return NodeFilter.FILTER_ACCEPT;
                }
            }
        );

        const textNodes = [];
        while (walker.nextNode()) {
            textNodes.push(walker.currentNode);
        }

        textNodes.forEach(node => {
            const currentVal = node.nodeValue;
            if (!currentVal || !currentVal.trim()) return;

            // บันทึกค่าภาษาไทยต้นฉบับไว้ใน _origTh
            if (node._origTh === undefined) {
                node._origTh = currentVal;
            } else if (lang !== 'th' && /[\u0E00-\u0E7F]/.test(currentVal) && currentVal !== node._lastTranslated) {
                // กรณี JavaScript หน้าเว็บอัปเดตข้อความภาษาไทยใหม่แบบไดนามิก
                node._origTh = currentVal;
            }

            if (lang === 'th') {
                if (node.nodeValue !== node._origTh) {
                    node.nodeValue = node._origTh;
                }
                node._lastTranslated = node._origTh;
                return;
            }

            const orig = node._origTh;
            const leadingMatch = orig.match(/^\s*/);
            const trailingMatch = orig.match(/\s*$/);
            const leading = leadingMatch ? leadingMatch[0] : '';
            const trailing = trailingMatch ? trailingMatch[0] : '';
            const core = orig.trim();

            const translatedCore = translateCore(core, lang);
            const finalStr = leading + translatedCore + trailing;
            node._lastTranslated = finalStr;
            if (node.nodeValue !== finalStr) {
                node.nodeValue = finalStr;
            }
        });

        // 2. แปล Placeholders ของ Input & Textarea
        document.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(el => {
            if (el._origPlaceholder === undefined) {
                el._origPlaceholder = el.getAttribute('placeholder') || '';
            }
            const origPh = el._origPlaceholder.trim();
            if (!origPh) return;

            if (lang === 'th') {
                el.setAttribute('placeholder', el._origPlaceholder);
            } else if (PLACEHOLDER_DICT[origPh] && PLACEHOLDER_DICT[origPh][lang]) {
                el.setAttribute('placeholder', PLACEHOLDER_DICT[origPh][lang]);
            }
        });
    }

    function setLanguage(lang) {
        const validLang = SUPPORTED_LANGS[lang] ? lang : 'th';
        localStorage.setItem(STORAGE_KEY, validLang);
        applyLanguageToDOM(validLang);
    }

    document.addEventListener('DOMContentLoaded', function () {
        // ผูก Event ปุ่มเลือกภาษา
        document.querySelectorAll('.lang-select-item').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const selectedLang = this.getAttribute('data-lang');
                setLanguage(selectedLang);
            });
        });

        // โหลดภาษาที่ลูกค้าเลือกไว้ล่าสุด
        const savedLang = localStorage.getItem(STORAGE_KEY) || 'th';
        applyLanguageToDOM(savedLang);

        // เฝ้าดูการเปลี่ยนแปลง DOM แบบไดนามิก (เช่น ตอนกดสลับแท็บสาขา หรือคำนวณราคาใหม่)
        let debounceTimer = null;
        const observer = new MutationObserver(function () {
            const currentLang = localStorage.getItem(STORAGE_KEY) || 'th';
            if (currentLang === 'th') return;
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                applyLanguageToDOM(currentLang);
            }, 60);
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });
    });

    // เปิดให้เรียกใช้งานจากภายนอกได้ผ่าน window.EasyStayI18n
    window.EasyStayI18n = {
        setLanguage: setLanguage,
        getLanguage: () => localStorage.getItem(STORAGE_KEY) || 'th'
    };
})();
