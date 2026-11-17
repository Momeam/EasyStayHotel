using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using EasyStayHotel.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EasyStayHotel.Controllers;

/// <summary>
/// คอนโทรลเลอร์ AccountingController: ดูแลระบบบัญชี การเงิน และการจัดการเงินคืนของโรงแรม (Accounting & Finance)
/// 
/// บทบาทหน้าที่สำคัญ (Key Responsibilities):
/// 1. รายงานสรุปรายได้ (Revenue Dashboard): คำนวณรายรับรวม แยกเงินสด (Cash) กับเงินโอน (PromptPay/Credit Card)
/// 2. วิเคราะห์รายได้ 3 สาขา (Bangkok, Chonburi, Phuket) และสร้างข้อมูลกราฟแนวโน้ม (Daily, Weekly, Monthly)
/// 3. ตรวจสอบการส่งยอดเงินประจำกะ (Shift Handover) จากพนักงานต้อนรับหน้าร้าน
/// 4. ตรวจสอบและอนุมัติ/ปฏิเสธคำร้องขอคืนเงิน (Refund Requests) ตามเงื่อนไข 7 วันและโอนคืนภายใน 3 วันทำการ
/// 5. จัดทำรายงานทางการเงินส่งต่อให้ผู้บริหาร (Admin / General Manager) ตรวจรับทราบและอนุมัติ
/// </summary>
public class AccountingController : Controller
{
    private readonly EasystaydbContext _context;

    /// <summary>
    /// Constructor รับ EasystaydbContext ผ่าน Dependency Injection
    /// </summary>
    public AccountingController(EasystaydbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// ตรวจสอบสิทธิ์การเข้าถึงระบบบัญชี: อนุญาตเฉพาะ Role: Accounting, Admin, SuperAdmin
    /// หากเป็นผู้ใช้กลุ่มอื่น จะถูกดีดกลับไปยังหน้าที่เหมาะสมกับบทบาทของตนเอง
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        // 1. ตรวจสอบว่าเข้าสู่ระบบหรือยัง
        if (userId == null || string.IsNullOrEmpty(role))
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบบัญชี";
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        // 2. ปฏิเสธสิทธิ์ผู้ใช้ทั่วไปที่ไม่ใช่ฝ่ายบัญชีหรือผู้บริหาร
        if (role != "Accounting" && role != "Admin" && role != "SuperAdmin")
        {
            TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบบัญชี";
            if (role == "Staff")
            {
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
            }
            else if (role == "Housekeeper" || role == "Maid")
            {
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
            }
            else
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            return;
        }

        base.OnActionExecuting(context);
    }

    // =========================================================================
    // 1. แดชบอร์ดรายงานรายได้และการเงิน (Revenue Report Dashboard)
    // =========================================================================
    /// <summary>
    /// แสดงหน้ารายงานรายได้ สถิติช่องทางชำระเงิน กราฟแนวโน้ม และรายการขอคืนเงิน
    /// </summary>
    /// <param name="branch">ตัวกรองสาขา (ALL, Bangkok, Chonburi, Phuket)</param>
    /// <param name="period">ตัวกรองรอบเวลา (Daily = 7 วัน, Weekly = 6 สัปดาห์, Monthly = 6 เดือน)</param>
    [HttpGet]
    public IActionResult RevenueReport(string? branch = "ALL", string? period = "Daily")
    {
        branch = string.IsNullOrEmpty(branch) ? "ALL" : branch;
        period = string.IsNullOrEmpty(period) ? "Daily" : period;

        // ดึงรายการธุรกรรมการชำระเงินทั้งหมด พร้อม Eager Loading ข้อมูลการจอง ห้องพัก และผู้ใช้
        var allPayments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Include(p => p.Booking)
                .ThenInclude(b => b!.User)
            .OrderByDescending(p => p.PaymentId)
            .ToList();

        // กรองตามสาขาที่เลือก
        var payments = allPayments;
        if (branch != "ALL")
        {
            payments = payments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        var today = DateTime.Today;
        // กรองเฉพาะรายการที่ชำระเงินเรียบร้อยแล้ว (Paid / Confirmed / Approved)
        var validPaidPayments = payments.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved").ToList();

        // 1. คำนวณสรุปยอดรายได้หลักตามช่องทางชำระเงิน
        decimal totalRevenue = validPaidPayments.Sum(p => p.Amount);
        decimal cashTotal = validPaidPayments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
        decimal promptpayTotal = validPaidPayments.Where(p => p.PaymentMethod == "PromptPay").Sum(p => p.Amount);
        decimal cardTotal = validPaidPayments.Where(p => p.PaymentMethod == "CreditCard").Sum(p => p.Amount);
        decimal transferTotal = promptpayTotal + cardTotal;

        // 2. คำนวณสัดส่วนรายได้แยกตาม 3 สาขา
        var allValidPaid = allPayments.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved").ToList();
        decimal bkkTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        // 3. เตรียมชุดข้อมูลแกน X (Labels) และแกน Y (Data) สำหรับแสดงผลบน Chart.js
        var trendLabels = new List<string>();
        var trendData = new List<decimal>();

        if (period == "Weekly")
        {
            // คำนวณสรุปย้อนหลัง 6 สัปดาห์
            for (int i = 5; i >= 0; i--)
            {
                var startOfWeek = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1)).AddDays(-i * 7);
                var endOfWeek = startOfWeek.AddDays(6);
                string label = $"{startOfWeek:dd/MM} - {endOfWeek:dd/MM}";
                decimal sum = validPaidPayments
                    .Where(p => {
                        var date = p.TransferDate?.Date ?? today;
                        return date >= startOfWeek && date <= endOfWeek;
                    })
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }
        else if (period == "Monthly")
        {
            // คำนวณสรุปย้อนหลัง 6 เดือน
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                int targetYear = monthDate.Year;
                int targetMonth = monthDate.Month;
                string label = monthDate.ToString("MMM yyyy");
                decimal sum = validPaidPayments
                    .Where(p => {
                        var date = p.TransferDate ?? today;
                        return date.Year == targetYear && date.Month == targetMonth;
                    })
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }
        else // Daily
        {
            // คำนวณสรุปย้อนหลัง 7 วัน
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                string label = targetDate.ToString("dd MMM");
                decimal sum = validPaidPayments
                    .Where(p => (p.TransferDate?.Date ?? today) == targetDate)
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }

        var allRefunds = RefundService.GetAll(branch == "ALL" ? null : branch);

        // ส่งตัวแปรสถิติทั้งหมดไปยัง View ผ่าน ViewBag
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.CashTotal = cashTotal;
        ViewBag.TransferTotal = transferTotal;
        ViewBag.PromptPayTotal = promptpayTotal;
        ViewBag.CardTotal = cardTotal;
        ViewBag.SelectedBranch = branch;
        ViewBag.SelectedPeriod = period;

        ViewBag.BkkRevenue = bkkTotal;
        ViewBag.ChonRevenue = chonTotal;
        ViewBag.PhuketRevenue = phuketTotal;

        ViewBag.TrendLabels = trendLabels;
        ViewBag.TrendData = trendData;

        ViewBag.StaffFeedbacks = AccountingFeedbackService.GetAll(branch == "ALL" ? null : branch);
        ViewBag.RefundRequests = allRefunds;
        ViewBag.PendingRefundsCount = allRefunds.Count(r => r.Status == "Pending");

        ViewBag.FinancialReports = FinancialReportService.GetAll();
        ViewBag.PendingFinancialReportsCount = FinancialReportService.GetAll().Count(r => r.Status == "Pending");

        return View(payments);
    }

    // =========================================================================
    // 2. ส่งรายงานสรุปทางการเงินไปยังผู้บริหาร (Send Financial Report to Admin)
    // =========================================================================
    /// <summary>
    /// ฝ่ายบัญชีรวบรวมตัวเลขสรุปรายได้ เงินสด เงินโอน ยอดขอคืนเงิน และส่งรายงานไปยังแดชบอร์ดของผู้จัดการ
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendReportToAdmin(string periodType, string branch, string periodLabel, string? notes)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "ฝ่ายบัญชีและการเงิน";

        // รวบรวมตัวเลขการชำระเงินที่ผ่านการตรวจสอบแล้ว
        var allPayments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
            .ToList();

        var filteredPayments = allPayments;
        if (!string.IsNullOrEmpty(branch) && branch != "ALL")
        {
            filteredPayments = filteredPayments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        decimal totalRev = filteredPayments.Sum(p => p.Amount);
        decimal cashRev = filteredPayments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
        decimal transRev = filteredPayments.Where(p => p.PaymentMethod == "PromptPay" || p.PaymentMethod == "CreditCard").Sum(p => p.Amount);
        int totalBookings = filteredPayments.Select(p => p.BookingId).Distinct().Count();

        decimal bkkRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        var refunds = RefundService.GetAll(branch == "ALL" ? null : branch);
        decimal refundAmount = refunds.Where(r => r.Status == "Approved").Sum(r => r.Amount);

        // สร้างรายงานทางการเงินใหม่
        var report = new FinancialReport
        {
            CreatedAt = DateTime.Now,
            CreatedBy = userName,
            PeriodType = periodType ?? "Daily",
            Branch = branch ?? "ALL",
            PeriodLabel = string.IsNullOrWhiteSpace(periodLabel) ? $"{periodType} Summary ณ {DateTime.Now:dd/MM/yyyy HH:mm}" : periodLabel.Trim(),
            TotalRevenue = totalRev,
            CashAmount = cashRev,
            TransferAmount = transRev,
            RefundAmount = refundAmount,
            TotalBookings = totalBookings,
            BangkokRevenue = bkkRev,
            ChonburiRevenue = chonRev,
            PhuketRevenue = phuketRev,
            Notes = notes?.Trim(),
            Status = "Pending"
        };

        bool saved = FinancialReportService.Create(report);
        if (saved)
        {
            TempData["SuccessMessage"] = $"ส่งรายงานสรุปการเงิน ({report.PeriodLabel}) ถึงผู้จัดการ (Admin) เรียบร้อยแล้ว! รหัสรายงาน: {report.ReportId}";
        }
        else
        {
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกและส่งรายงาน";
        }

        return RedirectToAction(nameof(RevenueReport), new { branch = branch, period = periodType });
    }

    // =========================================================================
    // 3. รับทราบรายงานปิดกะจากพนักงานต้อนรับ (Acknowledge Staff Feedback)
    // =========================================================================
    /// <summary>
    /// ฝ่ายบัญชีกดรับทราบยอดเงินส่งกะ (Cash & Transfer Handover) จากพนักงานหน้าร้าน
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcknowledgeFeedback(string id)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานบัญชี";
        bool result = AccountingFeedbackService.Acknowledge(id, userName);
        if (result)
        {
            TempData["SuccessMessage"] = "บันทึกการรับทราบรายงานเรียบร้อยแล้ว";
        }
        return RedirectToAction(nameof(RevenueReport));
    }

    // =========================================================================
    // 4. อนุมัติการคืนเงินให้ลูกค้า (Approve Refund Request)
    // =========================================================================
    /// <summary>
    /// ฝ่ายบัญชีตรวจสอบและอนุมัติการโอนเงินคืนธนาคารให้แก่ลูกค้า:
    /// 1. บันทึกสถานะคำร้องเป็น Approved
    /// 2. เปลี่ยนสถานะการจองเป็น "Refunded"
    /// 3. ปลดล็อกห้องพักกลับเป็น "Available" เพื่อให้ผู้อื่นสามารถจองต่อได้
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveRefund(string refundId, string? adminNote, string? slipUrl)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานฝ่ายบัญชี";
        var refund = RefundService.GetById(refundId);
        if (refund == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลคำขอคืนเงินที่ระบุ";
            return RedirectToAction(nameof(RevenueReport));
        }

        bool success = RefundService.Approve(refundId, userName, adminNote, slipUrl);
        if (success)
        {
            // อัปเดตสถานะการจองในฐานข้อมูล MySQL
            var booking = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == refund.BookingId);

            if (booking != null)
            {
                booking.BookingStatus = "Refunded";

                // อัปเดตสถานะการชำระเงิน
                var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                if (latestPayment != null)
                {
                    latestPayment.PaymentStatus = "Refunded";
                }

                // คืนสถานะห้องพักกลับเป็น Available (ถ้าห้องไม่ได้อยู่ระหว่างทำความสะอาด)
                if (booking.Room != null && booking.Room.Status != "Cleaning")
                {
                    booking.Room.Status = "Available";
                }

                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = $"อนุมัติการคืนเงินรหัส {refund.BookingRef} ยอดเงิน ฿{refund.Amount:N2} สำเร็จแล้ว! บันทึกประวัติการโอนคืนเข้า {refund.BankName} ({refund.BankAccountNumber}) เรียบร้อย";
        }
        else
        {
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการอนุมัติคำขอคืนเงิน";
        }

        return RedirectToAction(nameof(RevenueReport));
    }

    // =========================================================================
    // 5. ปฏิเสธคำร้องขอคืนเงิน (Reject Refund Request)
    // =========================================================================
    /// <summary>
    /// ฝ่ายบัญชีปฏิเสธคำร้องขอคืนเงิน (เช่น ขอยื่นเกิน 7 วัน หรือเข้าพักแล้ว) พร้อมระบุเหตุผล
    /// และปรับสถานะการจองกลับมาเป็น "Confirmed" ตามเดิม
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectRefund(string refundId, string rejectionReason)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานฝ่ายบัญชี";
        var refund = RefundService.GetById(refundId);
        if (refund == null)
        {
            TempData["ErrorMessage"] = "ไม่พบข้อมูลคำขอคืนเงินที่ระบุ";
            return RedirectToAction(nameof(RevenueReport));
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "กรุณาระบุเหตุผลในการปฏิเสธคำขอคืนเงิน";
            return RedirectToAction(nameof(RevenueReport));
        }

        bool success = RefundService.Reject(refundId, userName, rejectionReason.Trim());
        if (success)
        {
            // คืนสถานะการจองกลับเป็น Confirmed
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == refund.BookingId);
            if (booking != null)
            {
                booking.BookingStatus = "Confirmed";
                _context.SaveChanges();
            }

            TempData["SuccessMessage"] = $"ปฏิเสธคำขอคืนเงินรหัส {refund.BookingRef} เรียบร้อยแล้ว (เหตุผล: {rejectionReason})";
        }
        else
        {
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการปฏิเสธคำขอคืนเงิน";
        }

        return RedirectToAction(nameof(RevenueReport));
    }
}
