// นำเข้า System สำหรับการใช้งานคลาสพื้นฐาน เช่น String, DateTime, Random
using System;
// นำเข้า System.Linq สำหรับฟังก์ชันคิวรี LINQ เช่น FirstOrDefault, Any
using System.Linq;
// นำเข้า System.Threading.Tasks สำหรับการประมวลผลแบบ Asynchronous (Task, async/await)
using System.Threading.Tasks;
// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Cookie
using Microsoft.AspNetCore.Http;
// นำเข้า Microsoft.AspNetCore.Mvc สำหรับสร้างคอนโทรลเลอร์และ Action Results
using Microsoft.AspNetCore.Mvc;
// นำเข้าโมเดลหลักของโปรเจกต์
using EasyStayHotel.Models;
// นำเข้า ViewModels ที่ใช้ในการรับ-ส่งข้อมูลระหว่าง View กับ Controller
using EasyStayHotel.Models.ViewModels;
// นำเข้าเซอร์วิสต่างๆ เช่น IEmailService
using EasyStayHotel.Services;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers
{
    // ตัวควบคุมระบบสมาชิก การเข้าสู่ระบบ ยืนยันตัวตนด้วยรหัส OTP และการจัดการโปรไฟล์
    public class AccountController : Controller
    {
        // ตัวแปรเชื่อมต่อฐานข้อมูล MySQL ผ่าน EF Core
        private readonly EasystaydbContext _context;
        // เซอร์วิสสำหรับส่งอีเมลยืนยันตัวตนและรหัส OTP
        private readonly IEmailService _emailService;

        // คอนสตรัคเตอร์รับ DbContext และ EmailService ผ่าน Dependency Injection
        public AccountController(EasystaydbContext context, IEmailService emailService)
        {
            // กำหนดค่า DbContext
            _context = context;
            // กำหนดค่า EmailService
            _emailService = emailService;
        }

        // 1. หน้าจอเข้าสู่ระบบ (Login)
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // ตรวจสอบว่าผู้ใช้ล็อกอินอยู่แล้วใน Session หรือไม่
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                // หากล็อกอินอยู่แล้ว ให้นำส่งกลับไปยังหน้าแรกทันทีโดยไม่ต้องล็อกอินซ้ำ
                return RedirectToAction("Index", "Home");
            }

            // ส่งค่า returnUrl ไปยัง View ผ่าน ViewBag เพื่อให้หลังล็อกอินสำเร็จสามารถกลับไปยังหน้าที่ตั้งใจเข้าได้
            ViewBag.ReturnUrl = returnUrl;
            // แสดงหน้าจอ View พร้อมโมเดล LoginViewModel เปล่า
            return View(new LoginViewModel());
        }

        // เมธอดตรวจสอบข้อมูลอีเมลและรหัสผ่านเพื่อเข้าสู่ระบบ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            // เก็บ returnUrl ไว้ใน ViewBag ในกรณีที่เกิดข้อผิดพลาดและต้องแสดงหน้า View เดิม
            ViewBag.ReturnUrl = returnUrl;

            // ตรวจสอบความถูกต้องของโมเดลตาม Data Annotations ([Required], [EmailAddress])
            if (!ModelState.IsValid)
            {
                // หากข้อมูลไม่ผ่านการตรวจสอบ ให้ส่งกลับหน้า View เพื่อแจ้งเตือน
                return View(model);
            }

            // ตัดช่องว่างและปรับอีเมลเป็นตัวพิมพ์เล็กเพื่อค้นหา
            string cleanEmail = model.Email.Trim().ToLower();
            // ค้นหาผู้ใช้จากอีเมลและรหัสผ่านในตาราง Users ของฐานข้อมูล MySQL
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail && u.Password == model.Password);

            // ตรวจสอบว่าพบผู้ใช้ที่ตรงกับเงื่อนไขหรือไม่
            if (user != null)
            {
                // กำหนดบทบาท หากเป็น null ให้เป็น Customer ตามค่าเริ่มต้น
                string userRole = user.Role ?? "Customer";
                // บันทึกรหัสผู้ใช้ลงใน Session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                // บันทึกชื่อ-นามสกุลลงใน Session
                HttpContext.Session.SetString("FullName", user.FullName);
                // บันทึกบทบาทลงใน Session สำหรับตรวจสอบสิทธิ์
                HttpContext.Session.SetString("Role", userRole);
                // บันทึกสาขาประจำการของพนักงาน (หากไม่มีให้เป็น Bangkok)
                HttpContext.Session.SetString("Branch", user.Branch ?? "Bangkok");
                // บันทึกอีเมลลงใน Session
                HttpContext.Session.SetString("Email", user.Email);

                // ตรวจสอบว่ามี returnUrl ที่ปลอดภัย (ป้องกัน Open Redirect Attack โดยตรวจว่าต้องเป็น URL ภายในระบบเท่านั้น)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    // แปลง URL ปลายทางเป็นตัวพิมพ์เล็กเพื่อวิเคราะห์สิทธิ์
                    string lowerUrl = returnUrl.ToLower();
                    // ตรวจสอบว่าเป็น URL ของระบบแอดมินหรือไม่
                    bool isAdminTarget = lowerUrl.Contains("/admin");
                    // ตรวจสอบว่าเป็น URL ของระบบพนักงานต้อนรับหรือไม่
                    bool isStaffTarget = lowerUrl.Contains("/staff");
                    // ตรวจสอบว่าเป็น URL ของระบบบัญชีหรือไม่
                    bool isAccountingTarget = lowerUrl.Contains("/accounting");
                    // ตรวจสอบว่าเป็น URL ของระบบแม่บ้านหรือไม่
                    bool isHousekeeperTarget = lowerUrl.Contains("/housekeeper");

                    // ตรวจสอบสิทธิ์สำหรับหน้า Admin
                    if (isAdminTarget && (userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    // ตรวจสอบสิทธิ์สำหรับหน้า Staff
                    else if (isStaffTarget && (userRole == "Staff" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    // ตรวจสอบสิทธิ์สำหรับหน้า Accounting
                    else if (isAccountingTarget && (userRole == "Accounting" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    // ตรวจสอบสิทธิ์สำหรับหน้า Housekeeper
                    else if (isHousekeeperTarget && (userRole == "Housekeeper" || userRole == "Maid" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    // หากเป็นหน้าทั่วไปของลูกค้าที่ไม่มีการจำกัดสิทธิ์เฉพาะทาง
                    else if (!isAdminTarget && !isStaffTarget && !isAccountingTarget && !isHousekeeperTarget)
                    {
                        return Redirect(returnUrl);
                    }
                }

                // การกระจายเส้นทางอัตโนมัติตามตำแหน่งของผู้ใช้ (Role-based Redirection) กรณีไม่มี returnUrl
                // หากเป็น Admin หรือ SuperAdmin ส่งไปยัง Dashboard ของผู้บริหาร
                if (userRole == "SuperAdmin" || userRole == "Admin") return RedirectToAction("Dashboard", "Admin");
                // หากเป็นพนักงานต้อนรับ ส่งไปยังหน้า BookingList ของพนักงาน
                if (userRole == "Staff") return RedirectToAction("BookingList", "Staff");
                // หากเป็นพนักงานบัญชี ส่งไปยังหน้า RevenueReport ของฝ่ายบัญชี
                if (userRole == "Accounting") return RedirectToAction("RevenueReport", "Accounting");
                // หากเป็นแม่บ้าน ส่งไปยังหน้า Dashboard ของแม่บ้าน
                if (userRole == "Housekeeper" || userRole == "Maid") return RedirectToAction("Dashboard", "Housekeeper");

                // หากเป็นลูกค้าทั่วไป ให้นำส่งกลับสู่หน้าแรก
                return RedirectToAction("Index", "Home");
            }

            // หากไม่พบบัญชีผู้ใช้ หรือรหัสผ่านไม่ถูกต้อง ให้แจ้งเตือนผู้ใช้
            ViewBag.Error = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาตรวจสอบและลองใหม่อีกครั้ง";
            // แสดงหน้าจอเดิมพร้อมข้อมูลที่กรอกไว้
            return View(model);
        }

        // 2. หน้าจอลงทะเบียนสมาชิกใหม่ (Register)
        [HttpGet]
        public IActionResult Register()
        {
            // ตรวจสอบว่าล็อกอินอยู่แล้วหรือไม่
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                // หากล็อกอินอยู่แล้ว ส่งกลับหน้าแรก
                return RedirectToAction("Index", "Home");
            }

            // แสดงหน้าจอสมัครสมาชิกพร้อมโมเดล RegisterViewModel เปล่า
            return View(new RegisterViewModel());
        }

        // เมธอด AJAX API สำหรับส่งรหัส OTP 6 หลักไปยังอีเมลเพื่อยืนยันตัวตนก่อนสมัครสมาชิก
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            // ตรวจสอบว่ามีการระบุอีเมลหรือไม่
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                // แจ้งเตือนข้อผิดพลาดกลับเป็น JSON
                return Json(new { success = false, message = "กรุณาระบุอีเมลที่ต้องการรับรหัส OTP" });
            }

            // ตัดช่องว่างและแปลงอีเมลเป็นตัวพิมพ์เล็ก
            string cleanEmail = request.Email.Trim().ToLower();

            // ตรวจสอบรูปแบบความถูกต้องของโครงสร้างอีเมล
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttr.IsValid(cleanEmail))
            {
                // แจ้งเตือนว่ารูปแบบอีเมลไม่ถูกต้อง
                return Json(new { success = false, message = "รูปแบบอีเมลไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง" });
            }

            // ตรวจสอบว่ามีอีเมลนี้อยู่ในฐานข้อมูลแล้วหรือไม่ (ป้องกันการสมัครซ้ำ)
            bool isEmailTaken = _context.Users.Any(u => u.Email.ToLower() == cleanEmail);
            if (isEmailTaken)
            {
                // แจ้งเตือนว่าอีเมลถูกใช้งานไปแล้ว
                return Json(new { success = false, message = "อีเมลนี้มีบัญชีในระบบแล้ว กรุณาเข้าสู่ระบบ หรือใช้อีเมลอื่น" });
            }

            // สุ่มตัวเลข 6 หลักสำหรับรหัส OTP ระหว่าง 100000 ถึง 999999
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // บันทึกรหัส OTP ลงใน Session ชั่วคราว
            HttpContext.Session.SetString("Register_Otp", otpCode);
            // บันทึกอีเมลที่ผูกกับ OTP ลง Session
            HttpContext.Session.SetString("Register_Email", cleanEmail);
            // กำหนดเวลาหมดอายุของ OTP เป็น 5 นาทีหลังจากนี้ (ISO 8601 string)
            HttpContext.Session.SetString("Register_OtpExpiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            // กำหนดชื่อผู้รับอีเมล
            string recipientName = string.IsNullOrWhiteSpace(request.FullName) ? "ผู้สนใจสมัครสมาชิก" : request.FullName.Trim();

            // ส่งอีเมลรหัส OTP ผ่าน EmailService
            var emailResult = await _emailService.SendOtpEmailAsync(cleanEmail, recipientName, otpCode);

            // ส่งผลลัพธ์กลับในรูปแบบ JSON เพื่อให้ JavaScript แสดงผล (ปิดการส่ง devOtp ออกหน้าจอเพื่อให้รับจากอีเมลจริงเท่านั้น)
            return Json(new
            {
                // สถานะความสำเร็จ
                success = true,
                // ข้อความแจ้งเตือน
                message = emailResult.Message,
                // อีเมลที่ส่งไป
                email = cleanEmail
            });
        }

        // เมธอดประมวลผลการสมัครสมาชิก ตรวจสอบรหัส OTP และบันทึกข้อมูลสมาชิกลง MySQL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            // ตรวจสอบความถูกต้องของโมเดล
            if (!ModelState.IsValid)
            {
                // หากข้อมูลไม่ครบถ้วน ส่งกลับหน้าเดิม
                return View(model);
            }

            // ทำความสะอาดอีเมล
            string cleanEmail = model.Email.Trim().ToLower();

            // ตรวจสอบในฐานข้อมูลอีกครั้งว่าอีเมลยังไม่ถูกแย่งสมัครในระหว่างนี้
            if (_context.Users.Any(u => u.Email.ToLower() == cleanEmail))
            {
                // แจ้งข้อผิดพลาด
                ViewBag.Error = "อีเมลนี้ถูกใช้งานแล้ว กรุณาใช้อีเมลอื่น";
                return View(model);
            }

            // ดึงค่า OTP, อีเมล และเวลาหมดอายุจาก Session
            string? sessionOtp = HttpContext.Session.GetString("Register_Otp");
            string? sessionEmail = HttpContext.Session.GetString("Register_Email");
            string? sessionExpiryStr = HttpContext.Session.GetString("Register_OtpExpiry");

            // ตรวจสอบว่าผู้ใช้ได้กรอกรหัส OTP หรือไม่
            if (string.IsNullOrEmpty(model.OtpCode))
            {
                ViewBag.Error = "กรุณากรอกรหัส OTP ที่ได้รับจากอีเมลเพื่อยืนยันตัวตน";
                return View(model);
            }

            // ตรวจสอบว่ามีข้อมูล OTP ในเซสชันและตรงกับอีเมลที่สมัครหรือไม่
            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail) || sessionEmail != cleanEmail)
            {
                ViewBag.Error = "ไม่พบข้อมูลรหัส OTP สำหรับอีเมลนี้ กรุณากดส่งรหัส OTP ใหม่อีกครั้ง";
                return View(model);
            }

            // ตรวจสอบว่ารหัส OTP หมดอายุเกิน 5 นาทีแล้วหรือไม่
            if (DateTime.TryParse(sessionExpiryStr, out DateTime expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "รหัส OTP หมดอายุแล้ว (เกิน 5 นาที) กรุณากดขอรหัส OTP ใหม่อีกครั้ง";
                    return View(model);
                }
            }

            // ตรวจสอบว่ารหัส OTP ที่กรอกตรงกับรหัสที่สร้างขึ้นหรือไม่
            if (model.OtpCode.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบรหัสจากอีเมลอีกครั้ง";
                return View(model);
            }

            // สร้างอ็อบเจกต์ User ใหม่
            var newUser = new User
            {
                // ชื่อ-นามสกุล
                FullName = model.FullName.Trim(),
                // อีเมลที่ผ่านการยืนยันแล้ว
                Email = cleanEmail,
                // เบอร์โทรศัพท์
                Phone = model.Phone.Trim(),
                // เลขบัตรประชาชนหรือพาสปอร์ต
                IdCardOrPassport = string.IsNullOrWhiteSpace(model.IdCardOrPassport) ? null : model.IdCardOrPassport.Trim(),
                // วันเดือนปีเกิด (ใช้คำนวณโปรโมชั่นเดือนเกิด)
                DateOfBirth = model.DateOfBirth,
                // รหัสผ่านผู้ใช้งาน
                Password = model.Password,
                // สิทธิ์ผู้ใช้เป็น Customer
                Role = "Customer",
                // สาขาตั้งต้น
                Branch = "Bangkok"
            };

            // เพิ่มผู้ใช้ใหม่ลงในฐานข้อมูล
            _context.Users.Add(newUser);
            // บันทึกการเปลี่ยนแปลงลง MySQL
            _context.SaveChanges();

            // ล้างข้อมูล OTP ออกจาก Session เพื่อความปลอดภัย
            HttpContext.Session.Remove("Register_Otp");
            HttpContext.Session.Remove("Register_Email");
            HttpContext.Session.Remove("Register_OtpExpiry");

            // แจ้งข้อความสำเร็จและนำส่งไปยังหน้าเข้าสู่ระบบ
            TempData["SuccessMessage"] = $"สมัครสมาชิกและยืนยันอีเมล {cleanEmail} สำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบ";
            return RedirectToAction("Login", new { email = cleanEmail });
        }

        // 3. หน้าจัดการโปรไฟล์ผู้ใช้งาน (Profile)
        [HttpGet]
        public IActionResult Profile()
        {
            // ดึง UserId ของผู้ใช้ที่ล็อกอินอยู่จาก Session
            int? userId = HttpContext.Session.GetInt32("UserId");
            // ตรวจสอบว่ายังไม่ได้ล็อกอินหรือไม่
            if (userId == null)
            {
                // หากยังไม่ได้ล็อกอิน ให้นำส่งไปหน้า Login พร้อมแนบ returnUrl
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account") });
            }

            // ค้นหาข้อมูลผู้ใช้ในฐานข้อมูลด้วย UserId
            var user = _context.Users.Find(userId.Value);
            // หากไม่พบข้อมูลผู้ใช้ในฐานข้อมูล
            if (user == null)
            {
                // ล้างเซสชันและส่งไปหน้า Login
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // ผูกข้อมูลผู้ใช้เข้ากับ UserProfileViewModel
            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? string.Empty,
                IdCardOrPassport = user.IdCardOrPassport,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role ?? "Customer",
                Branch = user.Branch ?? "Bangkok"
            };

            // แสดงหน้าจอโปรไฟล์
            return View(model);
        }

        // เมธอดประมวลผลการอัปเดตข้อมูลส่วนตัวและเปลี่ยนรหัสผ่าน
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(UserProfileViewModel model)
        {
            // ดึง UserId จาก Session
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // ค้นหาข้อมูลผู้ใช้ในฐานข้อมูล
            var user = _context.Users.Find(userId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // คืนค่า Email, Role, Branch เดิมที่ไม่อนุญาตให้แก้ไขเอง
            model.Email = user.Email;
            model.Role = user.Role;
            model.Branch = user.Branch;

            // ตรวจสอบกรณีที่ผู้ใช้ประสงค์จะเปลี่ยนรหัสผ่านใหม่
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                // ตรวจสอบว่ามีการกรอกรหัสผ่านปัจจุบันหรือไม่
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "กรุณาระบุรหัสผ่านปัจจุบันเพื่อยืนยันการตั้งรหัสผ่านใหม่");
                }
                // ตรวจสอบว่ารหัสผ่านปัจจุบันตรงกับในฐานข้อมูลหรือไม่
                else if (model.CurrentPassword != user.Password)
                {
                    ModelState.AddModelError("CurrentPassword", "รหัสผ่านปัจจุบันไม่ถูกต้อง");
                }

                // ตรวจสอบว่ารหัสผ่านใหม่ตรงกับการยืนยันรหัสผ่านหรือไม่
                if (string.IsNullOrWhiteSpace(model.ConfirmNewPassword) || model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่");
                }
            }

            // ตรวจสอบว่าโมเดลผ่านการตรวจสอบทั้งหมดหรือไม่
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // อัปเดตชื่อ-นามสกุล
            user.FullName = model.FullName.Trim();
            // อัปเดตเบอร์โทรศัพท์
            user.Phone = model.Phone.Trim();
            // อัปเดตวันเดือนปีเกิด
            user.DateOfBirth = model.DateOfBirth;
            // อัปเดตเลขบัตรประชาชนหรือพาสปอร์ต
            user.IdCardOrPassport = string.IsNullOrWhiteSpace(model.IdCardOrPassport) ? null : model.IdCardOrPassport.Trim();

            // หากมีการระบุรหัสผ่านใหม่และผ่านการตรวจสอบแล้ว ให้อัปเดตรหัสผ่าน
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = model.NewPassword;
                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวและเปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
            }
            else
            {
                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวเรียบร้อยแล้ว";
            }

            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
            _context.SaveChanges();

            // อัปเดตชื่อผู้ใช้ใน Session ให้เป็นปัจจุบัน
            HttpContext.Session.SetString("FullName", user.FullName);

            // ส่งกลับไปยังหน้าโปรไฟล์
            return RedirectToAction("Profile");
        }

        // 4. หน้าจอลืมรหัสผ่าน (Forgot Password)
        [HttpGet]
        public IActionResult ForgotPassword(string? email = null)
        {
            // หากล็อกอินอยู่แล้ว ให้กลับไปหน้าแรก
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // สร้าง ViewModel สำหรับหน้ารีเซ็ตรหัสผ่าน
            var model = new ForgotPasswordViewModel
            {
                Email = email ?? string.Empty
            };

            // แสดงหน้าจอ ForgotPassword
            return View(model);
        }

        // เมธอด AJAX API ส่งรหัส OTP ไปยังอีเมลสำหรับรีเซ็ตรหัสผ่าน
        [HttpPost]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] ForgotPasswordRequest request)
        {
            // ตรวจสอบว่ามีการกรอกอีเมลหรือไม่
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Json(new { success = false, message = "กรุณาระบุอีเมลที่ต้องการรีเซ็ตรหัสผ่าน" });
            }

            // ทำความสะอาดอีเมล
            string cleanEmail = request.Email.Trim().ToLower();

            // ค้นหาผู้ใช้จากอีเมลในฐานข้อมูล
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail);
            // หากไม่พบบัญชีผู้ใช้ในระบบ
            if (user == null)
            {
                return Json(new { success = false, message = "ไม่พบบัญชีผู้ใช้งานที่ใช้อีเมลนี้ในระบบ กรุณาตรวจสอบความถูกต้อง" });
            }

            // สุ่มรหัส OTP 6 หลัก
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // บันทึกรหัส OTP และอีเมลลง Session มีอายุ 5 นาที
            HttpContext.Session.SetString("Reset_Otp", otpCode);
            HttpContext.Session.SetString("Reset_Email", cleanEmail);
            HttpContext.Session.SetString("Reset_Expiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            // ส่งอีเมล OTP สำหรับรีเซ็ตรหัสผ่าน
            var emailResult = await _emailService.SendPasswordResetOtpEmailAsync(cleanEmail, user.FullName, otpCode);

            // ส่งผลลัพธ์กลับในรูปแบบ JSON (ปิดการส่ง devOtp ออกหน้าจอเพื่อให้รับรหัสจากอีเมลจริงเท่านั้น)
            return Json(new
            {
                success = true,
                message = emailResult.Message,
                email = cleanEmail
            });
        }

        // เมธอดประมวลผลการตั้งรหัสผ่านใหม่: ตรวจสอบ OTP และบันทึกรหัสผ่านใหม่ลงฐานข้อมูล
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ForgotPasswordViewModel model)
        {
            // ตรวจสอบความถูกต้องของโมเดล
            if (!ModelState.IsValid)
            {
                return View("ForgotPassword", model);
            }

            // ทำความสะอาดอีเมล
            string cleanEmail = model.Email.Trim().ToLower();
            // ค้นหาผู้ใช้ในฐานข้อมูล
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail);
            if (user == null)
            {
                ViewBag.Error = "ไม่พบบัญชีผู้ใช้งานที่ใช้อีเมลนี้ในระบบ กรุณาตรวจสอบความถูกต้อง";
                return View("ForgotPassword", model);
            }

            // ดึงค่า OTP จาก Session
            string? sessionOtp = HttpContext.Session.GetString("Reset_Otp");
            string? sessionEmail = HttpContext.Session.GetString("Reset_Email");
            string? sessionExpiryStr = HttpContext.Session.GetString("Reset_Expiry");

            // ตรวจสอบว่ามีการกรอก OTP หรือไม่
            if (string.IsNullOrEmpty(model.OtpCode))
            {
                ViewBag.Error = "กรุณากรอกรหัส OTP ที่ได้รับจากอีเมลเพื่อยืนยันตัวตน";
                return View("ForgotPassword", model);
            }

            // ตรวจสอบความตรงกันของอีเมลและ OTP ใน Session
            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail) || sessionEmail != cleanEmail)
            {
                ViewBag.Error = "ไม่พบข้อมูลรหัส OTP สำหรับอีเมลนี้ กรุณากดส่งรหัส OTP ใหม่อีกครั้ง";
                return View("ForgotPassword", model);
            }

            // ตรวจสอบเวลาหมดอายุของ OTP
            if (DateTime.TryParse(sessionExpiryStr, out DateTime expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "รหัส OTP หมดอายุแล้ว (เกิน 5 นาที) กรุณากดขอรหัส OTP ใหม่อีกครั้ง";
                    return View("ForgotPassword", model);
                }
            }

            // ตรวจสอบความถูกต้องของรหัส OTP
            if (model.OtpCode.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบรหัสจากอีเมลอีกครั้ง";
                return View("ForgotPassword", model);
            }

            // บันทึกรหัสผ่านใหม่ลงในอ็อบเจกต์ User
            user.Password = model.NewPassword;
            // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
            _context.SaveChanges();

            // ล้างข้อมูล OTP ออกจาก Session
            HttpContext.Session.Remove("Reset_Otp");
            HttpContext.Session.Remove("Reset_Email");
            HttpContext.Session.Remove("Reset_Expiry");

            // แจ้งข้อความสำเร็จและนำส่งไปยังหน้าเข้าสู่ระบบ
            TempData["SuccessMessage"] = "รีเซ็ตรหัสผ่านสำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบด้วยรหัสผ่านใหม่ของคุณ";
            return RedirectToAction("Login", new { email = cleanEmail });
        }

        // 5. ออกจากระบบ (Logout) ล้างค่าเซสชันทั้งหมดและส่งกลับไปหน้าแรก
        [HttpGet]
        public IActionResult Logout()
        {
            // ล้างข้อมูลทั้งหมดใน Session
            HttpContext.Session.Clear();
            // ส่งกลับหน้าแรกของเว็บไซต์
            return RedirectToAction("Index", "Home");
        }
    }
}
