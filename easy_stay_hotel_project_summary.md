# 🏨 Easy Stay Hotel - Project Summary & Code Structure

เอกสารสรุปโครงสร้างระบบ กฎเกณฑ์ และประวัติการพัฒนาโปรเจกต์ Easy Stay Hotel (CSI 402) เพื่อใช้เป็นคู่มืออ้างอิงในการเขียนโค้ด

## 🛠️ เทคโนโลยีที่ใช้ (Tech Stack)
*   **Backend:** C# ASP.NET Core MVC (.NET)
*   **Database:** MySQL, Entity Framework Core (EF Core)
*   **Frontend:** HTML5, Bootstrap 5, JavaScript (jQuery/AJAX)
*   **State Management:** `HttpContext.Session`

---

## 🏛️ สถาปัตยกรรมและกฎเกณฑ์ของระบบ (Core Architecture & Rules)

### 1. ระบบหลายสาขา (Multi-branch Architecture)
*   **กฎเกณฑ์:** ห้ามสร้างตารางฐานข้อมูลแยกตามสาขา (เช่น `Rooms_BKK`, `Rooms_PKT`) ให้ใช้ **ตารางเดียว (Single Table)**
*   **การนำไปใช้:** เพิ่มคอลัมน์ `Branch` (varchar) ลงในตาราง `Users` (สำหรับพนักงาน) และ `Rooms` (สำหรับห้องพัก)
*   **การกรองข้อมูล (Data Filtering):** 
    *   เมื่อพนักงานล็อกอิน ระบบจะเก็บค่าสาขาไว้ใน Session: `HttpContext.Session.SetString("Branch", user.Branch);`
    *   ทุกครั้งที่ดึงข้อมูลใน Controller ฝั่ง Staff ให้ดึงค่าจาก Session มาต่อท้ายคำสั่ง `.Where(r => r.Branch == currentBranch)` เสมอ เพื่อป้องกันข้อมูลข้ามสาขา

### 2. เครื่องยนต์โปรโมชั่น (Promotion Engine)
แบ่งตรรกะโปรโมชั่น 5 แบบ ออกเป็น 2 กลุ่ม (อยู่ใน `Services/PromotionService.cs`):
*   **กลุ่มอัตโนมัติ (Auto Discounts):**
    *   *พักยาว 3 คืนขึ้นไป:* ลด 10% (คำนวณลดเฉพาะยอดของ 1 คืน)
    *   *วันธรรมดา (จันทร์ - พฤหัส):* ลด 5%
    *   *เดือนเกิดลูกค้า:* ลด 10% (ตรวจสอบจาก `DateOfBirth` ของ User)
*   **กลุ่มคูปอง (Coupon Codes):**
    *   *EARLY30 / EASYBIRDBLUE:* จองล่วงหน้า >= 30 วัน ลด 15%
    *   *GROUP15:* จอง >= 2 ห้อง ลดห้องละ 15%
*   **กลไกการทำงาน:** หน้าเว็บจะใช้ JavaScript ส่ง AJAX Post ไปที่ `[HttpPost] CalculateDiscount` เพื่อคำนวณและแสดงผลเรียลไทม์โดยไม่ต้องรีเฟรชหน้า

### 3. การจัดการการจองและชำระเงิน (Booking & Checkout Flow)
*   หน้า Checkout จะคำนวณราคาสุทธิจาก (ค่าห้อง x คืน) + บริการเสริม (เตียงเสริม, อาหารเช้า, รถรับส่ง) - ส่วนลด
*   เมื่อกดยืนยัน ระบบจะทำ 5 ขั้นตอนใน `[HttpPost] ConfirmBooking`:
    1. อัปโหลดไฟล์สลิปเก็บไว้ที่ `wwwroot/uploads/slips`
    2. สร้าง User ใหม่ (ถ้าอีเมลยังไม่เคยสมัคร)
    3. สร้างเรคคอร์ดใน `Bookings` (สถานะ Pending)
    4. สร้างเรคคอร์ดใน `Payments` (สถานะ Pending พร้อมแนบ Path รูป)
    5. อัปเดตสถานะ `Rooms` เป็น "Reserved"

---

## 📂 โครงสร้างไฟล์และโค้ดที่สำคัญ (Key Files & Code Structure)

### 📁 Models (ฐานข้อมูลและโมเดล)
*   `EasystaydbContext.cs`: ไฟล์หลักสำหรับเชื่อมต่อ DB **สำคัญ:** ต้อง Map คอลัมน์ใหม่เสมอใน `OnModelCreating` (เช่น `entity.Property(e => e.Branch).HasColumnName("Branch");`)
*   `Room.cs`: เพิ่ม `public string Branch { get; set; } = "Bangkok";`
*   `User.cs`: เพิ่ม `public string? Branch { get; set; }` และ `public DateTime? DateOfBirth { get; set; }`

### 📁 ViewModels (ตัวส่งผ่านข้อมูล)
*   `CustomerCheckoutViewModel.cs`: เก็บข้อมูลฟอร์มหน้า Checkout ทั้งหมด (RoomId, ข้อมูลลูกค้า, จำนวน Add-ons, ไฟล์สลิป IFormFile)
*   `PromoRequestData.cs`: คลาสรับข้อมูล JSON จาก AJAX ตอนคำนวณโปรโมชั่น

### 📁 Controllers (ตัวควบคุมตรรกะ)
*   `AccountController.cs`: จัดการล็อกอินและบันทึก Session (Branch, Role, UserId)
*   `StaffController.cs`: ระบบจัดการหลังบ้านของพนักงานสาขาและแอดมิน ประกอบด้วย:
    *   `DailyOverview(GET)`: สรุปภาพรวมรายวัน (Today's Arrivals, Departures, In-House, Room status, Daily revenue)
    *   `CheckInDesk(GET)` & `ProcessCheckIn(POST)`: ระบบค้นหารหัสการจองออนไลน์ และจัดหมายเลขห้องพักที่ว่างให้ลูกค้าเข้าพัก
    *   `WalkinBooking(GET/POST)`: ระบบจองหน้าเคาน์เตอร์แบบ Walk-in พร้อม Add-ons และคำนวณราคาเรียลไทม์
    *   `CheckOutBooking(POST)`: ระบบเช็คเอาท์แขก และปรับสถานะห้องเป็น "Cleaning" (รอทำความสะอาด) โดยอัตโนมัติ
    *   `Index(GET)` & `QuickUpdateRoomStatus(POST)`: ผังควบคุมสถานะห้องพักแบบการ์ดเรียลไทม์ พร้อมปุ่ม 1-Click Update
    *   `BookingList(GET)`: รายชื่อผู้เข้าพักพร้อมแท็บแยกสถานะ ค้นหาอัจฉริยะ และปุ่มจัดการเช็คอิน/เช็คเอาท์
    *   `AccountingFeedback(GET/POST)`: ระบบส่งรายงานสรุปยอดกะ/ปิดวัน และ Feedback ส่งต่อให้แผนกบัญชีโดยตรง
*   `BookingController.cs`: หัวใจหลักของฝั่งลูกค้า ประกอบด้วย:
    *   `Search(GET)`: ค้นหาและจัดกลุ่มห้องว่าง (`GroupBy(r => r.RoomType)`)
    *   `Checkout(GET)`: แสดงฟอร์มพร้อมข้อมูลห้องที่เลือก
    *   `CalculateDiscount(POST)`: API รับ-ส่งข้อมูลส่วนลดผ่าน AJAX
    *   `ConfirmBooking(POST)`: เซฟรูปลงเซิร์ฟเวอร์ และบันทึกข้อมูลลงตารางที่เกี่ยวข้อง
    *   `BookingSuccess(GET)`: แสดงหน้าจองสำเร็จ

### 📁 Views (หน้าจอ UI - Customer Frontend)
*   `Home/Index.cshtml`: หน้าแรก มีฟอร์มเลือกสาขาและวันที่
*   `Booking/Search.cshtml`: แสดงการ์ดผลลัพธ์ห้องพักที่ว่าง
*   `Booking/Checkout.cshtml`: ฟอร์มกรอกข้อมูล มี JavaScript คำนวณราคาบริการเสริมแบบเรียลไทม์ และระบบเชื่อมต่อ API โปรโมชั่น
*   `Booking/BookingSuccess.cshtml`: หน้าสรุปการจองพร้อมเลขที่อ้างอิง

### 📁 Services (ฟังก์ชันเสริม)
*   `PromotionService.cs`: คลาสอิสระสำหรับประมวลผลคณิตศาสตร์/ตรรกะส่วนลด ส่งกลับเป็น `PromotionResult`

---

## 🐛 ปัญหาที่พบบ่อยและวิธีแก้ไข (Common Bugs & Troubleshooting)

1.  **EF Core แปลงค่า LINQ ไม่ได้ (`Translation of member 'Branch' failed`):**
    *   *สาเหตุ:* สร้างคอลัมน์ในฐานข้อมูลแล้ว แต่ยังไม่ได้ Map ใน DbContext
    *   *วิธีแก้:* เพิ่ม `entity.Property(e => e.Branch)` ในเมธอด `OnModelCreating` ของ `EasystaydbContext`
2.  **CS0246: The type or namespace name could not be found:**
    *   *สาเหตุ:* ลืมนำเข้า Namespace ของคลาสที่เรียกใช้
    *   *วิธีแก้:* เพิ่ม `using EasyStayHotel.Models.ViewModels;` (หรือโฟลเดอร์ที่เกี่ยวข้อง) ไว้บนสุดของไฟล์
3.  **Cannot implicitly convert type 'System.DateTime' to 'System.DateOnly':**
    *   *สาเหตุ:* ฐานข้อมูลและโมเดลใช้ `DateTime` แต่ไปเรียกใช้คำสั่งแปลงเป็น `DateOnly`
    *   *วิธีแก้:* ลบคำสั่งแปลงออก และจับเท่ากันโดยตรง เช่น `CheckInDate = model.CheckInDate;`
4.  **The name 'context' does not exist in the current context:**
    *   *สาเหตุ:* พิมพ์ชื่อตัวแปรผิด (เช่น พิมพ์ `context` แทนที่จะเป็น `_context`)
    *   *วิธีแก้:* ตรวจสอบการสะกดและเติม Underscore (`_`) ให้ตรงกับการประกาศตัวแปรของคลาส