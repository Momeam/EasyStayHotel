// ============================================================================
// Easy Stay Hotel & Resort - Trilingual Support Engine (ไทย / English / 中文)
// รองรับการสลับภาษาแบบทันที (Instant Client-Side I18n) สำหรับลูกค้าไทยและต่างชาติ
// ============================================================================

(function () {
    const STORAGE_KEY = 'easystay_lang';
    const SUPPORTED_LANGS = {
        th: { code: 'TH', label: 'ไทย', htmlLang: 'th' },
        en: { code: 'EN', label: 'English', htmlLang: 'en' },
        zh: { code: '中文', label: '中文', htmlLang: 'zh-CN' }
    };

    // คลังคำศัพท์แปลตรงตัว (Exact Dictionary: Thai -> English & Simplified Chinese)
    const EXACT_DICT = {
        // 1. Navbar & Footer
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

        // 2. Hero & Search Bar (Home / Search)
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
        "ระยะเวลา": { en: "Duration", zh: "入住时长" },
        "จำนวนห้อง": { en: "Rooms", zh: "房间数" },
        "จำนวนแขก": { en: "Guests", zh: "住客人数" },
        "ค้นหาห้องว่าง": { en: "Search Rooms", zh: "搜索空房" },
        "อัปเดตการค้นหา": { en: "Update Search", zh: "更新搜索" },
        "กลับหน้าแรก": { en: "Back to Home", zh: "返回首页" },
        "กลับสู่หน้าหลัก": { en: "Back to Home", zh: "返回首页" },
        "ย้อนกลับ": { en: "Back", zh: "返回" },

        // 3. Room Previews & Branches
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
        "/ คืน": { en: "/ Night", zh: "/ 晚" },
        "ดูรายละเอียด": { en: "View Details", zh: "查看详情" },
        "พรีวิวห้อง": { en: "Preview Room", zh: "预览客房" },
        "จองเลย": { en: "Book Now", zh: "立即预订" },
        "เลือกห้องนี้": { en: "Select Room", zh: "选择此房型" },
        "เลือกสาขาที่ต้องการจอง": { en: "Select Branch to Book", zh: "选择预订分店" },
        "ขนาดห้องพัก": { en: "Room Size", zh: "客房面积" },
        "จำนวนผู้เข้าพัก": { en: "Occupancy", zh: "可住人数" },
        "ราคาต่อคืน": { en: "Price / Night", zh: "每晚价格" },
        "รายละเอียดห้องพัก": { en: "Room Description", zh: "客房详情" },
        "รูปแบบเตียง:": { en: "Bed Type:", zh: "床型：" },
        "สิ่งอำนวยความสะดวกภายในห้องพัก": { en: "Room Amenities & Facilities", zh: "客房配套设施" },
        "เลือกสาขา (พร้อมจำนวนห้องว่าง)": { en: "Select Branch (with availability)", zh: "选择分店（含空房数量）" },
        "ดำเนินการค้นหาและจองห้องนี้": { en: "Search & Book This Room", zh: "搜索并预订此客房" },
        "มุมห้องนอนหลัก (Master Bedroom)": { en: "Master Bedroom View", zh: "主卧室视角 (Master Bedroom)" },
        "มุมพักผ่อนและบรรยากาศภายในห้อง": { en: "Living & Relaxation Area", zh: "休闲区与室内氛围" },
        "ห้องน้ำหรูและสิ่งอำนวยความสะดวก": { en: "Luxury Bathroom & Amenities", zh: "豪华卫浴与配套设施" },

        // 4. Room Types, Descriptions, Bed Types & Amenities
        "Standard Room (สแตนดาร์ด)": { en: "Standard Room", zh: "标准客房 (Standard Room)" },
        "Deluxe Room (ดีลักซ์)": { en: "Deluxe Room", zh: "豪华客房 (Deluxe Room)" },
        "Executive Suite (ห้องสวีท)": { en: "Executive Suite", zh: "行政套房 (Executive Suite)" },
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

        // 5. Destinations & Promotions Section
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

        // 6. Guest Reviews Section
        "เสียงตอบรับจากผู้เข้าพักจริง (Verified Guest Reviews)": { en: "Verified Guest Reviews", zh: "真实住客评价 (Verified Guest Reviews)" },
        "ความประทับใจระดับ 5 ดาวจากลูกค้าของเรา": { en: "5-Star Experiences from Our Guests", zh: "来自住客的五星级好评体验" },
        "สัมผัสประสบการณ์การพักผ่อนเหนือระดับที่ Easy Stay Hotel & Resort ทั้ง 3 สาขา ด้วยคะแนนความพึงพอใจเฉลี่ย 4.9 เต็ม 5 จากผู้เข้าพักจริง": {
            en: "Experience exceptional hospitality across all 3 Easy Stay branches, rated 4.9/5 by verified guests.",
            zh: "在 Easy Stay 三大分店体验卓越住宿服务，真实住客平均满意度高达 4.9 / 5 分。"
        },
        "ข้อความตอบกลับจากทางโรงแรม:": { en: "Response from Hotel Management:", zh: "酒店官方回复：" },
        "เข้าพักจริง": { en: "Verified Stay", zh: "真实入住" },
        "ประทับใจการเข้าพัก": { en: "Wonderful Stay", zh: "非常满意的入住体验" },

        // 7. Search Page
        "เลือกดูและจองห้องพักสาขาอื่น:": { en: "Explore & Book Other Branches:", zh: "切换查看并预订其他分店：" },
        "คลิกที่สาขาด้านล่างเพื่อสลับไปดูห้องว่างในจังหวัดที่คุณต้องการได้ทันที": {
            en: "Click any branch below to instantly switch and check room availability in that city.",
            zh: "点击下方分店按钮，即可立即切换查看对应城市的空房。"
        },
        "ตัวกรองการค้นหา": { en: "Search Filters", zh: "搜索筛选" },
        "ล้างทั้งหมด": { en: "Reset All", zh: "重置全部" },
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

        // 8. Checkout Page
        "ขั้นตอนการจองและเลือกบริการเสริม": { en: "Booking Details & Add-on Services", zh: "预订确认与附加服务" },
        "ตรวจสอบรายละเอียดห้องพัก เลือกบริการเสริม และชำระเงินผ่าน PromptPay": {
            en: "Review your room details, select optional add-ons, and complete payment via PromptPay.",
            zh: "请核对客房详情、选择附加服务，并通过 PromptPay 完成支付。"
        },
        "ข้อมูลห้องพักและการเข้าพัก": { en: "Room & Stay Information", zh: "客房与入住信息" },
        "จัดสรรหมายเลขห้อง ณ วันเช็คอิน": { en: "Room assigned at check-in", zh: "入住时分配房间号" },
        "จัดสรรห้องตอนเช็คอิน": { en: "Assigned at check-in", zh: "入住时分配房间" },
        "ยกเลิกฟรีล่วงหน้า 3 วันก่อนเข้าพัก": { en: "Free cancellation up to 3 days before check-in", zh: "入住前3天可免费取消" },
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
        "ส่วนลดโปรโมชั่น": { en: "Promotion Discount", zh: "促销优惠减免" },
        "ยอดรวมสุทธิชำระ": { en: "Grand Total", zh: "应付总金额" },
        "โค้ดส่วนลด / คูปองโปรโมชั่น": { en: "Promo Code / Discount Coupon", zh: "优惠码 / 促销礼券" },
        "ใช้คูปอง": { en: "Apply", zh: "使用优惠码" },
        "คูปองแนะนำ:": { en: "Suggested Coupons:", zh: "推荐优惠码：" },
        "สิทธิพิเศษโปรโมชั่น Easy Stay:": { en: "Easy Stay Promotion Privileges:", zh: "Easy Stay 专属优惠礼遇：" },
        "ยืนยันและจองห้องพักตอนนี้": { en: "Confirm & Complete Booking", zh: "立即确认并提交预订" },
        "คุณจะได้รับหมายเลขการจองทันทีหลังจากส่งหลักฐานการชำระเงิน": {
            en: "You will receive your booking reference immediately after submitting payment proof.",
            zh: "提交付款凭证后，您将立即获得预订参考编号。"
        },

        // 9. Booking Success, MyBookings & Voucher
        "การจองสำเร็จ!": { en: "Booking Successful!", zh: "预订成功！" },
        "ขอบคุณสำหรับการจองห้องพักกับ Easy Stay ระบบได้รับหลักฐานการชำระเงินของคุณแล้ว": {
            en: "Thank you for booking with Easy Stay. We have received your payment slip.",
            zh: "感谢您预订 Easy Stay 酒店，系统已收到您的付款凭证。"
        },
        "หมายเลขการจอง (Booking Reference)": { en: "Booking Reference", zh: "预订编号 (Booking Reference)" },
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
        "ดำเนินการสำเร็จ": { en: "Success", zh: "操作成功" },
        "เกิดข้อผิดพลาด": { en: "Error Occurred", zh: "发生错误" },
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

        // 10. Authentication (Login / Register / Profile / Forgot Password)
        "ยินดีต้อนรับสู่ Easy Stay Hotel": { en: "Welcome to Easy Stay Hotel", zh: "欢迎来到 Easy Stay 酒店" },
        "เข้าสู่ระบบเพื่อจัดการข้อมูลการจองห้องพัก รับข้อเสนอพิเศษเฉพาะสมาชิก และเพลิดเพลินกับการพักผ่อนระดับพรีเมียม": {
            en: "Sign in to manage your bookings, unlock member-only rates, and enjoy a premium stay.",
            zh: "登录以管理您的客房预订、获取会员专属特惠，尽享尊贵住宿体验。"
        },
        "ส่วนลดเดือนเกิด 10%": { en: "10% Birthday Month Discount", zh: "生日当月 9折优惠" },
        "รับสิทธิ์อัตโนมัติในเดือนเกิดของคุณ": { en: "Automatically applied during your birthday month", zh: "在您生日当月预订自动享受折扣" },
        "จัดการการจองสะดวก 24 ชม.": { en: "24/7 Booking Management", zh: "24小时便捷管理预订" },
        "ตรวจสอบสถานะห้องพักและสลิปการโอน": { en: "Track room status and payment slips anytime", zh: "随时查看客房状态与付款凭证" },
        "ความปลอดภัยมาตรฐาน": { en: "Enterprise Security", zh: "高标准安全保障" },
        "ระบบยืนยันตัวตนด้วยรหัส OTP ทางอีเมล": { en: "Secure email OTP authentication system", zh: "通过电子邮箱 OTP 验证码进行安全验证" },
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
        "จัดการข้อมูลส่วนตัว": { en: "Manage Personal Profile", zh: "管理个人资料" },
        "โปรไฟล์ผู้ใช้งาน": { en: "User Profile", zh: "用户资料" },
        "ข้อมูลส่วนบุคคล": { en: "Personal Info", zh: "个人信息" },
        "ความปลอดภัย & เปลี่ยนรหัสผ่าน": { en: "Security & Password", zh: "安全与修改密码" },
        "ตั้งรหัสผ่านใหม่": { en: "Reset Password", zh: "重置密码" },
        "ลืมรหัสผ่านบัญชี?": { en: "Forgot Your Password?", zh: "忘记账户密码？" },
        "ขอรับ OTP": { en: "Get OTP", zh: "获取验证码" },
        "ยืนยันและบันทึกรหัสผ่านใหม่": { en: "Confirm & Save New Password", zh: "确认并保存新密码" }
    };

    // คลังแปล Placeholder ของช่อง Input
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
        "ต่ำสุด": { en: "Min", zh: "最低" },
        "สูงสุด": { en: "Max", zh: "最高" }
    };

    // กฎแปลข้อความแบบมีตัวเลขหรือตัวแปรไดนามิก (Regex Patterns)
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
            regex: /^รองรับสูงสุด\s+(\d+)\s+ท่าน$/,
            en: (_, n) => `Max ${n} Guests`,
            zh: (_, n) => `最多可住 ${n} 位`
        },
        {
            regex: /^สูงสุด\s+(\d+)\s+ท่าน$/,
            en: (_, n) => `Up to ${n} Guests`,
            zh: (_, n) => `最多 ${n} 位`
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
            regex: /^สาขา\s+(.+)$/,
            en: (_, b) => `${translateCore(b, 'en')} Branch`,
            zh: (_, b) => `${translateCore(b, 'zh')}分店`
        },
        {
            regex: /^คุณ\s+(.+)$/,
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

    // คำศัพท์ย่อยสำหรับประโยคผสม
    const PHRASE_FRAGMENTS = [
        { th: "ผลการค้นหา: พบประเภทห้องว่าง", en: "Search Results: Found", zh: "搜索结果：找到" },
        { th: "รายการ", en: "room type(s)", zh: "种可用房型" },
        { th: "กำลังดูสาขา:", en: "Viewing Branch:", zh: "当前分店：" },
        { th: "กรุงเทพมหานคร", en: "Bangkok", zh: "曼谷" },
        { th: "ชลบุรี (พัทยา)", en: "Chonburi (Pattaya)", zh: "春武里 (芭堤雅)" },
        { th: "ภูเก็ต", en: "Phuket", zh: "普吉岛" },
        { th: "ค่าห้องพัก", en: "Room Charge", zh: "客房费用" },
        { th: "ผู้เข้าพัก:", en: "Guest:", zh: "住客：" },
        { th: "ฟรี Wi-Fi และอาหารเช้า", en: "Free Wi-Fi & Breakfast", zh: "免费 Wi-Fi 及早餐" },
        { th: "โค้ดกลุ่ม:", en: "Group Code:", zh: "团队码：" },
        { th: "โค้ด:", en: "Code:", zh: "优惠码：" },
        { th: "(คลิกคัดลอก)", en: "(Click to copy)", zh: "(点击复制)" }
    ];

    function translateCore(trimmed, lang) {
        if (!trimmed || lang === 'th') return trimmed;

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

        // 2. แปล Placeholders ของ Input
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
