using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EasyStayHotel.Services
{
    public interface IEmailService
    {
        Task<(bool Success, string Message)> SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
        Task<(bool Success, string Message)> SendPasswordResetOtpEmailAsync(string toEmail, string fullName, string otpCode);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

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
            <h1>🏨 Easy Stay Hotel & Resort</h1>
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
                ⚠️ รหัส OTP นี้มีอายุการใช้งาน <strong>5 นาที</strong> และสามารถใช้งานได้ครั้งเดียว<br>
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
            <h1>🔒 Easy Stay Hotel & Resort</h1>
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
                ⚠️ รหัส OTP นี้มีอายุการใช้งาน <strong>5 นาที</strong><br>
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
    }
}
