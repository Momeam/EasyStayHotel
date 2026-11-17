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

public class AccountingController : Controller
{
    private readonly EasystaydbContext _context;
    public AccountingController(EasystaydbContext context)
    {
        _context = context;
    }

    // ตรวจสอบสิทธิ์การเข้าถึงระบบบัญชี (Accounting, Admin, SuperAdmin)
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        // 1. ยังไม่ได้เข้าสู่ระบบ
        if (userId == null || string.IsNullOrEmpty(role))
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบบัญชี";
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        // 2. ผู้ไม่มีสิทธิ์ (Customer หรือ Staff ทั่วไป)
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

    [HttpGet]
    public IActionResult RevenueReport(string? branch = "ALL", string? period = "Daily")
    {
        branch = string.IsNullOrEmpty(branch) ? "ALL" : branch;
        period = string.IsNullOrEmpty(period) ? "Daily" : period;

        var allPayments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Include(p => p.Booking)
                .ThenInclude(b => b!.User)
            .OrderByDescending(p => p.PaymentId)
            .ToList();

        // กรองตามสาขา
        var payments = allPayments;
        if (branch != "ALL")
        {
            payments = payments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        var today = DateTime.Today;
        var validPaidPayments = payments.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved").ToList();

        // สรุปยอดรายได้หลัก
        decimal totalRevenue = validPaidPayments.Sum(p => p.Amount);
        decimal cashTotal = validPaidPayments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
        decimal promptpayTotal = validPaidPayments.Where(p => p.PaymentMethod == "PromptPay").Sum(p => p.Amount);
        decimal cardTotal = validPaidPayments.Where(p => p.PaymentMethod == "CreditCard").Sum(p => p.Amount);
        decimal transferTotal = promptpayTotal + cardTotal;

        // สรุปยอดแยก 3 สาขา
        var allValidPaid = allPayments.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved").ToList();
        decimal bkkTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketTotal = allValidPaid.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        // เตรียมข้อมูลกราฟแนวโน้มตาม Period (Daily / Weekly / Monthly)
        var trendLabels = new List<string>();
        var trendData = new List<decimal>();

        if (period == "Weekly")
        {
            // 6 สัปดาห์ย้อนหลัง
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
            // 6 เดือนย้อนหลัง
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
            // 7 วันย้อนหลัง
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

    // POST: /Accounting/SendReportToAdmin
    // ฝ่ายบัญชีส่งรายงานสรุปภาพรวมการเงินไปยัง Admin / ผู้จัดการ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendReportToAdmin(string periodType, string branch, string periodLabel, string? notes)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "ฝ่ายบัญชีและการเงิน";

        // คำนวณตัวเลขตามเงื่อนไขที่เลือก
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

    // POST: /Accounting/ApproveRefund
    // พนักงานฝ่ายบัญชีตรวจสอบข้อมูลและอนุมัติการโอนเงินคืนธนาคาร (ภายใน 3 วัน)
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
            // อัปเดตสถานะการจองในฐานข้อมูล
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

                // คืนสถานะห้องพักกลับเป็น Available
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

    // POST: /Accounting/RejectRefund
    // พนักงานฝ่ายบัญชีปฏิเสธคำขอคืนเงินพร้อมระบุเหตุผล
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
