using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using EasyStayHotel.Models.ViewModels;
using EasyStayHotel.Services;

namespace EasyStayHotel.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์ AccountController: จัดการระบบบัญชีผู้ใช้ การยืนยันตัวตน และความปลอดภัย (Authentication & User Management)
    /// 
    /// วงจรการทำงานหลัก (Key Modules):
    /// 1. เข้าสู่ระบบ (Login): ตรวจสอบอีเมล/รหัสผ่าน จัดเก็บสถานะลง Session และกระจายสิทธิ์ (Role Redirection)
    /// 2. สมัครสมาชิก (Register): ระบบยืนยันตัวตนด้วยรหัส OTP ทางอีเมล 6 หลัก (อายุ 5 นาที) ป้องกันสแปมและอีเมลปลอม
    /// 3. จัดการโปรไฟล์ (Profile): แสดงและแก้ไขข้อมูลส่วนตัว พร้อมระบบเปลี่ยนรหัสผ่านที่ต้องยืนยันรหัสเดิม
    /// 4. ลืมรหัสผ่าน (Forgot Password): ขอรหัส OTP ทางอีเมลเพื่อตั้งรหัสผ่านใหม่ได้อย่างปลอดภัย
    /// 5. ออกจากระบบ (Logout): ล้างข้อมูลใน Session ทั้งหมดเพื่อความปลอดภัย
    /// </summary>
    public class AccountController : Controller
    {
        private readonly EasystaydbContext _context;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor รับ DbContext และ EmailService ผ่าน Dependency Injection
        /// </summary>
        public AccountController(EasystaydbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================================
        // 1. ระบบเข้าสู่ระบบ (Login)
        // =========================================================================

        /// <summary>
        /// แสดงหน้าจอ Login (หากผู้ใช้ล็อกอินอยู่แล้ว จะนำส่งกลับไปยังหน้าแรกทันที)
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// ประมวลผลการเข้าสู่ระบบ:
        /// 1. ค้นหาข้อมูลผู้ใช้จากอีเมลและรหัสผ่าน
        /// 2. บันทึกข้อมูลสำคัญลงใน Session (UserId, FullName, Role, Branch, Email)
        /// 3. ตรวจสอบความปลอดภัยของ returnUrl (ป้องกัน Open Redirect Attack)
        /// 4. แยกการนำทางตามบทบาท (Role-based Routing)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            // ตรวจสอบ Data Annotations ใน LoginViewModel ([Required], [EmailAddress])
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = model.Email.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail && u.Password == model.Password);

            if (user != null)
            {
                // บันทึกสถานะผู้ใช้งานลงใน Session เพื่อใช้ตรวจสอบสิทธิ์ใน Controller อื่นๆ
                string userRole = user.Role ?? "Customer";
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", userRole);
                HttpContext.Session.SetString("Branch", user.Branch ?? "Bangkok");
                HttpContext.Session.SetString("Email", user.Email);

                // หากมี ReturnUrl ที่ปลอดภัย (เป็น URL ภายในระบบ) และมีสิทธิ์ตรงตามบทบาท ให้ส่งกลับไปหน้านั้น
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    string lowerUrl = returnUrl.ToLower();
                    bool isAdminTarget = lowerUrl.Contains("/admin");
                    bool isStaffTarget = lowerUrl.Contains("/staff");
                    bool isAccountingTarget = lowerUrl.Contains("/accounting");
                    bool isHousekeeperTarget = lowerUrl.Contains("/housekeeper");

                    if (isAdminTarget && (userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    else if (isStaffTarget && (userRole == "Staff" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    else if (isAccountingTarget && (userRole == "Accounting" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    else if (isHousekeeperTarget && (userRole == "Housekeeper" || userRole == "Maid" || userRole == "Admin" || userRole == "SuperAdmin"))
                    {
                        return Redirect(returnUrl);
                    }
                    else if (!isAdminTarget && !isStaffTarget && !isAccountingTarget && !isHousekeeperTarget)
                    {
                        return Redirect(returnUrl);
                    }
                }

                // นำทางผู้ใช้ไปยังแดชบอร์ดตามบทบาทหน้าที่ (Role-Based Redirection)
                if (userRole == "SuperAdmin" || userRole == "Admin") return RedirectToAction("Dashboard", "Admin");
                if (userRole == "Staff") return RedirectToAction("BookingList", "Staff");
                if (userRole == "Accounting") return RedirectToAction("RevenueReport", "Accounting");
                if (userRole == "Housekeeper" || userRole == "Maid") return RedirectToAction("Dashboard", "Housekeeper");

                // ลูกค้าทั่วไปส่งไปยังหน้าหลักของโรงแรม
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาตรวจสอบและลองใหม่อีกครั้ง";
            return View(model);
        }

        // =========================================================================
        // 2. ระบบสมัครสมาชิกสำหรับลูกค้า (Register with Email OTP)
        // =========================================================================

        /// <summary>
        /// แสดงหน้าจอสมัครสมาชิกใหม่
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// AJAX API: สุ่มรหัส OTP 6 หลัก และส่งไปยังอีเมลผู้สมัคร เพื่อใช้ยืนยันความเป็นเจ้าของอีเมล
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Json(new { success = false, message = "กรุณาระบุอีเมลที่ต้องการรับรหัส OTP" });
            }

            string cleanEmail = request.Email.Trim().ToLower();

            // ตรวจสอบรูปแบบอีเมล
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttr.IsValid(cleanEmail))
            {
                return Json(new { success = false, message = "รูปแบบอีเมลไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง" });
            }

            // ตรวจสอบว่ามีอีเมลนี้ในฐานข้อมูลแล้วหรือยัง (ป้องกันสมัครซ้ำ)
            bool isEmailTaken = _context.Users.Any(u => u.Email.ToLower() == cleanEmail);
            if (isEmailTaken)
            {
                return Json(new { success = false, message = "อีเมลนี้มีบัญชีในระบบแล้ว กรุณาเข้าสู่ระบบ หรือใช้อีเมลอื่น" });
            }

            // สุ่มรหัส OTP 6 หลัก
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // บันทึก OTP และวันหมดอายุ (5 นาที) ลง Session
            HttpContext.Session.SetString("Register_Otp", otpCode);
            HttpContext.Session.SetString("Register_Email", cleanEmail);
            HttpContext.Session.SetString("Register_OtpExpiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            string recipientName = string.IsNullOrWhiteSpace(request.FullName) ? "ผู้สนใจสมัครสมาชิก" : request.FullName.Trim();

            // ส่งอีเมลจริงผ่าน EmailService
            var emailResult = await _emailService.SendOtpEmailAsync(cleanEmail, recipientName, otpCode);

            return Json(new
            {
                success = true,
                message = emailResult.Message,
                email = cleanEmail,
                devOtp = otpCode // สำหรับการทดสอบในสภาพแวดล้อม Development
            });
        }

        /// <summary>
        /// ประมวลผลการสมัครสมาชิก: ตรวจสอบความถูกต้องของ OTP และบันทึกข้อมูลลูกค้าใหม่ลงฐานข้อมูล
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = model.Email.Trim().ToLower();

            // ตรวจสอบอีเมลซ้ำอีกครั้งในฝั่งเซิร์ฟเวอร์
            if (_context.Users.Any(u => u.Email.ToLower() == cleanEmail))
            {
                ViewBag.Error = "อีเมลนี้ถูกใช้งานแล้ว กรุณาใช้อีเมลอื่น";
                return View(model);
            }

            // ตรวจสอบรหัส OTP ใน Session
            string? sessionOtp = HttpContext.Session.GetString("Register_Otp");
            string? sessionEmail = HttpContext.Session.GetString("Register_Email");
            string? sessionExpiryStr = HttpContext.Session.GetString("Register_OtpExpiry");

            if (string.IsNullOrEmpty(model.OtpCode))
            {
                ViewBag.Error = "กรุณากรอกรหัส OTP ที่ได้รับจากอีเมลเพื่อยืนยันตัวตน";
                return View(model);
            }

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail) || sessionEmail != cleanEmail)
            {
                ViewBag.Error = "ไม่พบข้อมูลรหัส OTP สำหรับอีเมลนี้ กรุณากดส่งรหัส OTP ใหม่อีกครั้ง";
                return View(model);
            }

            // ตรวจสอบอายุการใช้งานของ OTP (ไม่เกิน 5 นาที)
            if (DateTime.TryParse(sessionExpiryStr, out DateTime expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "รหัส OTP หมดอายุแล้ว (เกิน 5 นาที) กรุณากดขอรหัส OTP ใหม่อีกครั้ง";
                    return View(model);
                }
            }

            // ตรวจสอบความตรงกันของรหัส OTP
            if (model.OtpCode.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบรหัสจากอีเมลอีกครั้ง";
                return View(model);
            }

            // สร้าง Record ผู้ใช้ใหม่ในตาราง users ของ MySQL
            var newUser = new User
            {
                FullName = model.FullName.Trim(),
                Email = cleanEmail,
                Phone = model.Phone.Trim(),
                IdCardOrPassport = string.IsNullOrWhiteSpace(model.IdCardOrPassport) ? null : model.IdCardOrPassport.Trim(),
                DateOfBirth = model.DateOfBirth,
                Password = model.Password,
                Role = "Customer",
                Branch = "Bangkok"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // ล้างค่า Session ของ OTP เมื่อสมัครสำเร็จ
            HttpContext.Session.Remove("Register_Otp");
            HttpContext.Session.Remove("Register_Email");
            HttpContext.Session.Remove("Register_OtpExpiry");

            TempData["SuccessMessage"] = $"สมัครสมาชิกและยืนยันอีเมล {cleanEmail} สำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบ";
            return RedirectToAction("Login", new { email = cleanEmail });
        }

        // =========================================================================
        // 3. ระบบจัดการโปรไฟล์ผู้ใช้งาน (User Profile)
        // =========================================================================

        /// <summary>
        /// แสดงหน้าจอโปรไฟล์และข้อมูลส่วนตัวของผู้ใช้งานที่ล็อกอินอยู่
        /// </summary>
        [HttpGet]
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account") });
            }

            var user = _context.Users.Find(userId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

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

            return View(model);
        }

        /// <summary>
        /// บันทึกการแก้ไขข้อมูลโปรไฟล์ และเปลี่ยนรหัสผ่านใหม่
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(UserProfileViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            // ป้องกันการแก้ไขฟิลด์สำคัญผ่านทาง Client
            model.Email = user.Email;
            model.Role = user.Role;
            model.Branch = user.Branch;

            // ตรวจสอบเงื่อนไขกรณีผู้ใช้ต้องการเปลี่ยนรหัสผ่านใหม่
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "กรุณาระบุรหัสผ่านปัจจุบันเพื่อยืนยันการตั้งรหัสผ่านใหม่");
                }
                else if (model.CurrentPassword != user.Password)
                {
                    ModelState.AddModelError("CurrentPassword", "รหัสผ่านปัจจุบันไม่ถูกต้อง");
                }

                if (string.IsNullOrWhiteSpace(model.ConfirmNewPassword) || model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "รหัสผ่านยืนยันไม่ตรงกับรหัสผ่านใหม่");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // บันทึกการแก้ไขข้อมูลทั่วไป
            user.FullName = model.FullName.Trim();
            user.Phone = model.Phone.Trim();
            user.DateOfBirth = model.DateOfBirth;
            user.IdCardOrPassport = string.IsNullOrWhiteSpace(model.IdCardOrPassport) ? null : model.IdCardOrPassport.Trim();

            // อัปเดตรหัสผ่านใหม่หากมีการระบุ
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = model.NewPassword;
                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวและเปลี่ยนรหัสผ่านใหม่เรียบร้อยแล้ว";
            }
            else
            {
                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวเรียบร้อยแล้ว";
            }

            _context.SaveChanges();

            // อัปเดตชื่อใน Session ให้เป็นปัจจุบัน
            HttpContext.Session.SetString("FullName", user.FullName);

            return RedirectToAction("Profile");
        }

        // =========================================================================
        // 4. ระบบลืมรหัสผ่าน / รีเซ็ตรหัสผ่าน (Forgot & Reset Password)
        // =========================================================================

        /// <summary>
        /// แสดงหน้าจอลืมรหัสผ่าน
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword(string? email = null)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ForgotPasswordViewModel
            {
                Email = email ?? string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// AJAX API: ส่งรหัส OTP ไปยังอีเมลสำหรับรีเซ็ตรหัสผ่าน
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Json(new { success = false, message = "กรุณาระบุอีเมลที่ต้องการรีเซ็ตรหัสผ่าน" });
            }

            string cleanEmail = request.Email.Trim().ToLower();

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail);
            if (user == null)
            {
                return Json(new { success = false, message = "ไม่พบบัญชีผู้ใช้งานที่ใช้อีเมลนี้ในระบบ กรุณาตรวจสอบความถูกต้อง" });
            }

            // สุ่มรหัส OTP 6 หลัก
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // บันทึกลง Session 5 นาที
            HttpContext.Session.SetString("Reset_Otp", otpCode);
            HttpContext.Session.SetString("Reset_Email", cleanEmail);
            HttpContext.Session.SetString("Reset_Expiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            // ส่งอีเมล OTP ผ่าน EmailService
            var emailResult = await _emailService.SendPasswordResetOtpEmailAsync(cleanEmail, user.FullName, otpCode);

            return Json(new
            {
                success = true,
                message = emailResult.Message,
                email = cleanEmail,
                devOtp = otpCode // สำหรับโหมดทดสอบ
            });
        }

        /// <summary>
        /// ประมวลผลการรีเซ็ตรหัสผ่านใหม่: ตรวจสอบ OTP และบันทึกรหัสผ่านใหม่ลงฐานข้อมูล
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ForgotPassword", model);
            }

            string cleanEmail = model.Email.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail);
            if (user == null)
            {
                ViewBag.Error = "ไม่พบบัญชีผู้ใช้งานที่ใช้อีเมลนี้ในระบบ กรุณาตรวจสอบความถูกต้อง";
                return View("ForgotPassword", model);
            }

            // ตรวจสอบความถูกต้องของ OTP ใน Session
            string? sessionOtp = HttpContext.Session.GetString("Reset_Otp");
            string? sessionEmail = HttpContext.Session.GetString("Reset_Email");
            string? sessionExpiryStr = HttpContext.Session.GetString("Reset_Expiry");

            if (string.IsNullOrEmpty(model.OtpCode))
            {
                ViewBag.Error = "กรุณากรอกรหัส OTP ที่ได้รับจากอีเมลเพื่อยืนยันตัวตน";
                return View("ForgotPassword", model);
            }

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionEmail) || sessionEmail != cleanEmail)
            {
                ViewBag.Error = "ไม่พบข้อมูลรหัส OTP สำหรับอีเมลนี้ กรุณากดส่งรหัส OTP ใหม่อีกครั้ง";
                return View("ForgotPassword", model);
            }

            if (DateTime.TryParse(sessionExpiryStr, out DateTime expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "รหัส OTP หมดอายุแล้ว (เกิน 5 นาที) กรุณากดขอรหัส OTP ใหม่อีกครั้ง";
                    return View("ForgotPassword", model);
                }
            }

            if (model.OtpCode.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบรหัสจากอีเมลอีกครั้ง";
                return View("ForgotPassword", model);
            }

            // บันทึกรหัสผ่านใหม่ลงฐานข้อมูล MySQL
            user.Password = model.NewPassword;
            _context.SaveChanges();

            // ล้าง Session
            HttpContext.Session.Remove("Reset_Otp");
            HttpContext.Session.Remove("Reset_Email");
            HttpContext.Session.Remove("Reset_Expiry");

            TempData["SuccessMessage"] = "รีเซ็ตรหัสผ่านสำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบด้วยรหัสผ่านใหม่ของคุณ";
            return RedirectToAction("Login", new { email = cleanEmail });
        }

        // =========================================================================
        // 5. ระบบออกจากระบบ (Logout)
        // =========================================================================

        /// <summary>
        /// ออกจากระบบ: ล้างข้อมูล Session ทั้งหมดและนำส่งกลับสู่หน้าหลักของเว็บไซต์
        /// </summary>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // ล้าง Session ทั้งหมด
            return RedirectToAction("Index", "Home");
        }
    }
}
