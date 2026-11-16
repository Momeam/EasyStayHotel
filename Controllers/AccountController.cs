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
    public class AccountController : Controller
    {
        private readonly EasystaydbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(EasystaydbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // 1. ระบบเข้าสู่ระบบ (Login)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // ถ้าล็อกอินอยู่แล้ว ให้ส่งกลับหน้าแรก
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = model.Email.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail && u.Password == model.Password);

            if (user != null)
            {
                // บันทึกข้อมูลลง Session
                string userRole = user.Role ?? "Customer";
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", userRole);
                HttpContext.Session.SetString("Branch", user.Branch ?? "Bangkok");
                HttpContext.Session.SetString("Email", user.Email);

                // หากมี ReturnUrl ที่ปลอดภัย และมีสิทธิ์เข้าถึงหน้านั้น ให้ย้อนกลับไปหน้านั้น
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    string lowerUrl = returnUrl.ToLower();
                    bool isAdminTarget = lowerUrl.Contains("/admin");
                    bool isStaffTarget = lowerUrl.Contains("/staff");
                    bool isAccountingTarget = lowerUrl.Contains("/accounting");

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
                    else if (!isAdminTarget && !isStaffTarget && !isAccountingTarget)
                    {
                        return Redirect(returnUrl);
                    }
                }

                // แยกทางไปตาม Role (ตำแหน่ง)
                if (userRole == "SuperAdmin" || userRole == "Admin") return RedirectToAction("Dashboard", "Admin");
                if (userRole == "Staff") return RedirectToAction("BookingList", "Staff");
                if (userRole == "Accounting") return RedirectToAction("RevenueReport", "Accounting");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาตรวจสอบและลองใหม่อีกครั้ง";
            return View(model);
        }

        // ==========================================
        // 2. ระบบสมัครสมาชิกสำหรับลูกค้า (Register)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/SendOtp (AJAX API สำหรับส่งรหัส OTP ไปยังอีเมล)
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

            // เช็คว่ามีอีเมลนี้ในระบบหรือยัง
            bool isEmailTaken = _context.Users.Any(u => u.Email.ToLower() == cleanEmail);
            if (isEmailTaken)
            {
                return Json(new { success = false, message = "อีเมลนี้มีบัญชีในระบบแล้ว กรุณาเข้าสู่ระบบ หรือใช้อีเมลอื่น" });
            }

            // สุ่มรหัส OTP 6 หลัก
            string otpCode = Random.Shared.Next(100000, 999999).ToString();

            // บันทึก OTP ลง Session พร้อมวันหมดอายุ (5 นาที)
            HttpContext.Session.SetString("Register_Otp", otpCode);
            HttpContext.Session.SetString("Register_Email", cleanEmail);
            HttpContext.Session.SetString("Register_OtpExpiry", DateTime.UtcNow.AddMinutes(5).ToString("o"));

            string recipientName = string.IsNullOrWhiteSpace(request.FullName) ? "ผู้สนใจสมัครสมาชิก" : request.FullName.Trim();

            // ส่งอีเมลผ่าน EmailService
            var emailResult = await _emailService.SendOtpEmailAsync(cleanEmail, recipientName, otpCode);

            return Json(new
            {
                success = true,
                message = emailResult.Message,
                email = cleanEmail,
                devOtp = otpCode // ส่งกลับไปเพื่ออำนวยความสะดวกในการทดสอบโหมด Development
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanEmail = model.Email.Trim().ToLower();

            // ตรวจสอบว่าอีเมลซ้ำหรือไม่
            if (_context.Users.Any(u => u.Email.ToLower() == cleanEmail))
            {
                ViewBag.Error = "อีเมลนี้ถูกใช้งานแล้ว กรุณาใช้อีเมลอื่น";
                return View(model);
            }

            // ตรวจสอบความถูกต้องของ OTP
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

            if (DateTime.TryParse(sessionExpiryStr, out DateTime expiryTime))
            {
                if (DateTime.UtcNow > expiryTime)
                {
                    ViewBag.Error = "รหัส OTP หมดอายุแล้ว (เกิน 5 นาที) กรุณากดขอรหัส OTP ใหม่อีกครั้ง";
                    return View(model);
                }
            }

            if (model.OtpCode.Trim() != sessionOtp.Trim())
            {
                ViewBag.Error = "รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบรหัสจากอีเมลอีกครั้ง";
                return View(model);
            }

            // ผ่านการตรวจสอบ OTP ทั้งหมด -> บันทึกข้อมูลลงฐานข้อมูล
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

            // เคลียร์ค่า Session ของ OTP
            HttpContext.Session.Remove("Register_Otp");
            HttpContext.Session.Remove("Register_Email");
            HttpContext.Session.Remove("Register_OtpExpiry");

            TempData["SuccessMessage"] = $"สมัครสมาชิกและยืนยันอีเมล {cleanEmail} สำเร็จเรียบร้อยแล้ว! กรุณาเข้าสู่ระบบ";
            return RedirectToAction("Login", new { email = cleanEmail });
        }

        // ==========================================
        // 3. ระบบออกจากระบบ (Logout)
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // ล้างข้อมูล Session ทั้งหมด
            return RedirectToAction("Index", "Home");
        }
    }
}
