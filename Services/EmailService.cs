using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EasyStayHotel.Services
{
    /// <summary>
    /// อินเตอร์เฟซ IEmailService: สัญญาระบุฟังก์ชันการส่งอีเมลของระบบ (Service Interface)
    /// คำถามที่อาจารย์มักถาม:
    /// - ทำไมต้องมี Interface?
    ///   -> เพื่อรองรับหลักการ Dependency Inversion Principle (DIP) และ Loose Coupling ทำให้สามารถสลับผู้ให้บริการอีเมลหรือ Mock ข้อมูลในการทำ Unit Test ได้ง่าย
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// ส่งรหัส OTP ยืนยันตัวตนสำหรับการสมัครสมาชิกใหม่
        /// </summary>
        Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string fullName, string otpCode);

        /// <summary>
        /// ส่งรหัส OTP สำหรับการรีเซ็ตรหัสผ่าน (Forgot Password)
        /// </summary>
        Task<(bool Success, string Message)> SendPasswordResetOtpEmailAsync(string toEmail, string fullName, string otpCode);

        /// <summary>
        /// ส่งอีเมลยืนยันการจองห้องพักสำเร็จพร้อมรายละเอียดและลิงก์เปิดใบเสร็จ Voucher
        /// </summary>
        Task<(bool Success, string Message)> SendBookingConfirmationEmailAsync(string toEmail, string guestName, string bookingRef, string roomTypeName, string branch, string checkIn, string checkOut, decimal totalAmount, string voucherUrl);
    }

    /// <summary>
    /// เซอร์วิส EmailService: จัดการส่งอีเมลผ่าน SMTP (Simple Mail Transfer Protocol)
    /// คุณสมบัติเด่น:
    /// - รองรับการส่งจริงผ่าน Gmail SMTP (port 587, SSL/TLS)
    /// - มีระบบจำลองการส่ง (Local Development Simulation Mode) หากยังไม่ได้ระบุ Credentials ใน appsettings.json ทำให้ระบบทดสอบได้ทันทีโดยไม่เกิด Error ขัดข้อง
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        /// <summary>
        /// Constructor รับ IConfiguration สำหรับอ่านการตั้งค่า SMTP และ ILogger สำหรับบันทึก Log การส่งอีเมล
        /// </summary>
        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// ส่งรหัส OTP สำหรับยืนยันตัวตนตอนสมัครสมาชิกใหม่ (อายุการใช้งาน 5 นาที)
        /// </summary>
        public async Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            string host = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            int port = int.TryParse(_config["EmailSettings:SmtpPort"], out int p) ? p : 587;
            string senderEmail = _config["EmailSettings:SenderEmail"] ?? "";
            string senderPassword = _config["EmailSettings:SenderPassword"] ?? "";
            string senderName = _config["EmailSettings:SenderName"] ?? "Easy Stay Hotel & Resort";
            bool enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true;

            // ตรวจสอบว่าได้ตั้งค่า SenderEmail หรือยัง ถ้ายังไม่ได้ตั้งค่า ให้จำลองการส่ง (Local Development Mode)
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogInformation("=================================================");
                _logger.LogInformation("[Easy Stay Hotel] EMAIL SIMULATION (LOCAL DEV)");
                _logger.LogInformation("ส่งไปยัง: {Email} ({Name})", toEmail, fullName);
                _logger.LogInformation("รหัส OTP คือ: {Otp}", otpCode);
                _logger.LogInformation("=================================================");

                return (true, "จำลองการส่งรหัส OTP เรียบร้อยแล้ว (ยังไม่ได้ระบุ SMTP ใน appsettings.json)");
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"[Easy Stay Hotel] รหัสยืนยันการสมัครสมาชิกของคุณคือ {otpCode}",
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail, fullName));

                message.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: linear-gradient(135deg, #0f172a 0%, #0d6efd 100%); color: #ffffff; padding: 36px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; }}
        .header p {{ margin: 8px 0 0 0; font-size: 14px; opacity: 0.85; }}
        .content {{ padding: 36px 30px; color: #334155; line-height: 1.6; }}
        .otp-box {{ background-color: #f1f5f9; border: 2px dashed #0d6efd; border-radius: 12px; text-align: center; padding: 22px; margin: 24px 0; }}
        .otp-code {{ font-size: 34px; font-weight: 800; letter-spacing: 8px; color: #0d6efd; }}
        .warning {{ font-size: 13px; color: #64748b; margin-top: 20px; text-align: center; }}
        .footer {{ background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 30px; text-align: center; font-size: 12px; color: #94a3b8; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>Easy Stay Hotel & Resort</h1>
            <p>ระบบยืนยันตัวตนสำหรับการลงทะเบียนสมาชิกใหม่</p>
        </div>
        <div class='content'>
            <p>เรียน คุณ <strong>{fullName}</strong>,</p>
            <p>ขอบคุณที่ให้ความสนใจสมัครสมาชิกกับ Easy Stay Hotel & Resort กรุณานำรหัสยืนยัน (OTP) ด้านล่างนี้ไปกรอกในหน้าเว็บไซต์เพื่อยืนยันอีเมลของคุณ:</p>
            
            <div class='otp-box'>
                <div style='font-size: 13px; color: #64748b; margin-bottom: 8px; text-transform: uppercase;'>รหัสยืนยัน OTP (Valid for 5 mins)</div>
                <div class='otp-code'>{otpCode}</div>
            </div>

            <p class='warning'>
                <strong>ข้อควรระวัง:</strong> รหัส OTP นี้มีอายุการใช้งาน <strong>5 นาที</strong> และสามารถใช้งานได้ครั้งเดียว<br>
                เพื่อความปลอดภัยของข้อมูล โปรดอย่าเปิดเผยรหัสนี้ให้บุคคลอื่นทราบ
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Easy Stay Hotel & Resort (CSI 402 Project)<br>
            หากคุณไม่ได้เป็นผู้ร้องขอการสมัครสมาชิกนี้ โปรดละเลยอีเมลฉบับนี้
        </div>
    </div>
</body>
</html>";

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    Timeout = 10000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("ส่งอีเมล OTP ไปยัง {Email} สำเร็จ", toEmail);
                return (true, "ส่งรหัส OTP ไปยังอีเมลเรียบร้อยแล้ว");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการส่งอีเมล OTP ไปยัง {Email}", toEmail);
                return (false, "ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์อีเมลได้ แต่คุณสามารถใช้รหัสจำลองเพื่อทดสอบได้");
            }
        }

        /// <summary>
        /// ส่งรหัส OTP สำหรับการรีเซ็ตรหัสผ่าน (Password Reset)
        /// </summary>
        public async Task<(bool Success, string Message)> SendPasswordResetOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            string host = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            int port = int.TryParse(_config["EmailSettings:SmtpPort"], out int p) ? p : 587;
            string senderEmail = _config["EmailSettings:SenderEmail"] ?? "";
            string senderPassword = _config["EmailSettings:SenderPassword"] ?? "";
            string senderName = _config["EmailSettings:SenderName"] ?? "Easy Stay Hotel & Resort";
            bool enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogInformation("=================================================");
                _logger.LogInformation("[Easy Stay Hotel] PASSWORD RESET OTP SIMULATION");
                _logger.LogInformation("ส่งไปยัง: {Email} ({Name})", toEmail, fullName);
                _logger.LogInformation("รหัส OTP คือ: {Otp}", otpCode);
                _logger.LogInformation("=================================================");

                return (true, "จำลองการส่งรหัส OTP สำหรับรีเซ็ตรหัสผ่านเรียบร้อยแล้ว");
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"[Easy Stay Hotel] รหัส OTP สำหรับรีเซ็ตรหัสผ่านของคุณคือ {otpCode}",
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail, fullName));

                message.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: linear-gradient(135deg, #0f172a 0%, #dc3545 100%); color: #ffffff; padding: 36px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; }}
        .header p {{ margin: 8px 0 0 0; font-size: 14px; opacity: 0.85; }}
        .content {{ padding: 36px 30px; color: #334155; line-height: 1.6; }}
        .otp-box {{ background-color: #fff5f5; border: 2px dashed #dc3545; border-radius: 12px; text-align: center; padding: 22px; margin: 24px 0; }}
        .otp-code {{ font-size: 34px; font-weight: 800; letter-spacing: 8px; color: #dc3545; }}
        .warning {{ font-size: 13px; color: #64748b; margin-top: 20px; text-align: center; }}
        .footer {{ background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 30px; text-align: center; font-size: 12px; color: #94a3b8; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>Easy Stay Hotel & Resort</h1>
            <p>ระบบยืนยันตัวตนสำหรับการตั้งรหัสผ่านใหม่ (Password Reset)</p>
        </div>
        <div class='content'>
            <p>เรียน คุณ <strong>{fullName}</strong>,</p>
            <p>เราได้รับคำขอรีเซ็ตรหัสผ่านสำหรับบัญชี Easy Stay Hotel ของคุณ กรุณานำรหัสยืนยัน (OTP) ด้านล่างนี้ไปกรอกในหน้าเว็บไซต์เพื่อตั้งรหัสผ่านใหม่:</p>
            
            <div class='otp-box'>
                <div style='font-size: 13px; color: #64748b; margin-bottom: 8px; text-transform: uppercase;'>รหัสยืนยัน OTP (Valid for 5 mins)</div>
                <div class='otp-code'>{otpCode}</div>
            </div>

            <p class='warning'>
                <strong>ข้อควรระวัง:</strong> รหัส OTP นี้มีอายุการใช้งาน <strong>5 นาที</strong><br>
                หากคุณไม่ได้เป็นผู้ร้องขอการรีเซ็ตรหัสผ่าน โปรดติดต่อเจ้าหน้าที่โรงแรมทันทีเพื่อความปลอดภัยของบัญชี
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Easy Stay Hotel & Resort<br>
            อีเมลฉบับนี้ส่งจากระบบอัตโนมัติ กรุณาอย่าตอบกลับ
        </div>
    </div>
</body>
</html>";

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    Timeout = 10000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("ส่งอีเมล Password Reset OTP ไปยัง {Email} สำเร็จ", toEmail);
                return (true, "ส่งรหัส OTP สำหรับรีเซ็ตรหัสผ่านเรียบร้อยแล้ว");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการส่งอีเมล Password Reset OTP ไปยัง {Email}", toEmail);
                return (false, "ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์อีเมลได้ แต่คุณสามารถใช้รหัสจำลองเพื่อทดสอบได้");
            }
        }

        /// <summary>
        /// ส่งอีเมลยืนยันการจองห้องพักสำเร็จ (Booking Confirmation) แจ้งรายละเอียดการเข้าพักและลิงก์เปิดใบเสร็จ Voucher
        /// </summary>
        public async Task<(bool Success, string Message)> SendBookingConfirmationEmailAsync(
            string toEmail, 
            string guestName, 
            string bookingRef, 
            string roomTypeName, 
            string branch, 
            string checkIn, 
            string checkOut, 
            decimal totalAmount, 
            string voucherUrl)
        {
            string host = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            int port = int.TryParse(_config["EmailSettings:SmtpPort"], out int p) ? p : 587;
            string senderEmail = _config["EmailSettings:SenderEmail"] ?? "";
            string senderPassword = _config["EmailSettings:SenderPassword"] ?? "";
            string senderName = _config["EmailSettings:SenderName"] ?? "Easy Stay Hotel & Resort";
            bool enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogInformation("=================================================");
                _logger.LogInformation("[Easy Stay Hotel] EMAIL CONFIRMATION SIMULATION (LOCAL DEV)");
                _logger.LogInformation("ส่งใบยืนยันการจองไปยัง: {Email} ({Name})", toEmail, guestName);
                _logger.LogInformation("รหัสการจอง: {Ref} | สาขา: {Branch} | ยอดเงิน: ฿{Amount:N2}", bookingRef, branch, totalAmount);
                _logger.LogInformation("ลิงก์ดูใบเสร็จ Voucher: {Url}", voucherUrl);
                _logger.LogInformation("=================================================");

                return (true, "จำลองการส่งอีเมลยืนยันการจองเรียบร้อยแล้ว");
            }

            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"[Easy Stay Hotel] ยืนยันการจองห้องพักสำเร็จ - หมายเลขการจอง {bookingRef}",
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail, guestName));

                message.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #0d6efd 0%, #1e40af 100%); color: #ffffff; padding: 32px 24px; text-align: center; }}
        .content {{ padding: 32px 28px; color: #1e293b; line-height: 1.6; }}
        .booking-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px; margin: 20px 0; }}
        .btn {{ display: inline-block; background-color: #0d6efd; color: #ffffff !important; padding: 12px 28px; border-radius: 8px; text-decoration: none; font-weight: bold; margin-top: 15px; }}
        .footer {{ background: #f1f5f9; padding: 20px; text-align: center; font-size: 13px; color: #64748b; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h2 style='margin:0 0 8px 0; color:#ffffff;'>Easy Stay Hotel & Resort</h2>
            <p style='margin:0; opacity:0.9; font-size:14px;'>ใบยืนยันการจองห้องพัก (Booking Confirmation)</p>
        </div>
        <div class='content'>
            <p>เรียนคุณ <strong>{guestName}</strong>,</p>
            <p>ขอขอบพระคุณที่ท่านได้ไว้วางใจเลือกจองห้องพักกับ Easy Stay Hotel & Resort ระบบได้รับข้อมูลการจองและหลักฐานการชำระเงินของท่านเรียบร้อยแล้ว</p>
            
            <div class='booking-card'>
                <div style='border-bottom: 1px solid #e2e8f0; padding-bottom: 10px; margin-bottom: 12px;'>
                    <span style='color: #64748b; font-size: 13px;'>หมายเลขการจอง (Booking Reference):</span><br>
                    <strong style='font-size: 20px; color: #0d6efd;'>{bookingRef}</strong>
                </div>
                <table style='width: 100%; font-size: 14px; border-collapse: collapse;'>
                    <tr><td style='padding: 6px 0; color: #64748b;'>สาขาที่เข้าพัก:</td><td><strong>{branch}</strong></td></tr>
                    <tr><td style='padding: 6px 0; color: #64748b;'>ประเภทห้องพัก:</td><td><strong>{roomTypeName}</strong></td></tr>
                    <tr><td style='padding: 6px 0; color: #64748b;'>วันที่ Check-in:</td><td><strong>{checkIn}</strong> (ตั้งแต่ 14:00 น.)</td></tr>
                    <tr><td style='padding: 6px 0; color: #64748b;'>วันที่ Check-out:</td><td><strong>{checkOut}</strong> (ก่อน 12:00 น.)</td></tr>
                    <tr style='border-top: 1px dashed #cbd5e1;'><td style='padding: 10px 0 0 0; color: #64748b;'>ยอดชำระสุทธิ:</td><td style='padding: 10px 0 0 0;'><strong style='font-size: 18px; color: #16a34a;'>฿{totalAmount:N2}</strong></td></tr>
                </table>
            </div>

            <div style='text-align: center; margin: 25px 0;'>
                <a href='{voucherUrl}' class='btn' target='_blank'>ดูและพิมพ์ใบเสร็จการจอง (Voucher)</a>
            </div>

            <p style='font-size: 13px; color: #64748b;'>
                * ในวันเข้าพัก กรุณาแสดงหมายเลขการจองนี้หรือเปิดหน้า Voucher ในโทรศัพท์มือถือต่อพนักงานต้อนรับที่เคาน์เตอร์โรงแรม
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Easy Stay Hotel & Resort. All rights reserved.<br>
            หากมีข้อสงสัยหรือต้องการความช่วยเหลือ กรุณาติดต่อฝ่ายบริการลูกค้าของโรงแรม
        </div>
    </div>
</body>
</html>";

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    Timeout = 10000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("ส่งอีเมลยืนยันการจอง {Ref} ไปยัง {Email} สำเร็จ", bookingRef, toEmail);
                return (true, "ส่งอีเมลยืนยันการจองเรียบร้อยแล้ว");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการส่งอีเมลยืนยันการจองไปยัง {Email}", toEmail);
                return (false, "ไม่สามารถส่งอีเมลยืนยันได้");
            }
        }
    }
}
