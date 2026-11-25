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

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null || string.IsNullOrEmpty(role))
        {
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบบัญชี";
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

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

    private static DateTime GetNormalizedPaymentDate(Payment p)
    {
        if (p.TransferDate.HasValue)
        {
            return DateHelper.NormalizeToGregorian(p.TransferDate.Value);
        }
        if (p.Booking != null)
        {
            return DateHelper.NormalizeToGregorian(p.Booking.CheckInDate).ToDateTime(TimeOnly.MinValue);
        }
        return DateTime.Today;
    }

    private static bool IsCashMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return false;
        string m = method.Trim().ToLower();
        return m == "cash" || m.Contains("เงินสด");
    }

    [HttpGet]
    public IActionResult RevenueReport(string? branch = "ALL", string? period = "Daily", string? date = null)
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

        foreach (var p in allPayments)
        {
            if (p.TransferDate.HasValue)
            {
                p.TransferDate = DateHelper.NormalizeToGregorian(p.TransferDate.Value);
            }
        }

        var today = DateTime.Today;
        DateTime refDate = today;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (date.Length == 7 && DateTime.TryParse(date + "-01", out var parsedMonth))
            {
                refDate = DateHelper.NormalizeToGregorian(parsedMonth).Date;
            }
            else if (DateTime.TryParse(date, out var parsedDate))
            {
                refDate = DateHelper.NormalizeToGregorian(parsedDate).Date;
            }
        }

        DateTime filterStart;
        DateTime filterEnd;
        string prevDateParam;
        string nextDateParam;
        string currentPeriodDisplay;

        if (period == "Weekly")
        {
            int diffToMonday = (int)refDate.DayOfWeek == 0 ? 6 : (int)refDate.DayOfWeek - 1;
            filterStart = refDate.AddDays(-diffToMonday).Date;
            filterEnd = filterStart.AddDays(6).Date;
            prevDateParam = filterStart.AddDays(-7).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddDays(7).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"สัปดาห์วันที่ {filterStart:dd/MM/yyyy} - {filterEnd:dd/MM/yyyy}";
        }
        else if (period == "Monthly")
        {
            filterStart = new DateTime(refDate.Year, refDate.Month, 1);
            filterEnd = filterStart.AddMonths(1).AddDays(-1);
            prevDateParam = filterStart.AddMonths(-1).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddMonths(1).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"ประจำเดือน {filterStart:MMMM yyyy}";
        }
        else
        {
            period = "Daily";
            filterStart = refDate.Date;
            filterEnd = refDate.Date;
            prevDateParam = filterStart.AddDays(-1).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddDays(1).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"ประจำวันที่ {filterStart:dd/MM/yyyy}" + (filterStart == today ? " (วันนี้)" : "");
        }

        var periodPayments = allPayments
            .Where(p => {
                var pDate = GetNormalizedPaymentDate(p).Date;
                return pDate >= filterStart && pDate <= filterEnd;
            })
            .ToList();

        var payments = periodPayments;
        if (branch != "ALL")
        {
            payments = payments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        var validPaidPayments = payments
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();

        var pendingPayments = payments
            .Where(p => p.PaymentStatus == "Pending" && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();
        decimal pendingAmount = pendingPayments.Sum(p => p.Amount);

        decimal cashTotal = validPaidPayments.Where(p => IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        int cashCount = validPaidPayments.Count(p => IsCashMethod(p.PaymentMethod));

        var digitalPayments = validPaidPayments.Where(p => !IsCashMethod(p.PaymentMethod)).ToList();
        decimal transferTotal = digitalPayments.Sum(p => p.Amount);
        int digitalCount = digitalPayments.Count;
        decimal promptpayTotal = digitalPayments.Where(p => string.IsNullOrEmpty(p.PaymentMethod) || p.PaymentMethod.Contains("PromptPay", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);
        decimal cardTotal = transferTotal - promptpayTotal;

        decimal totalRevenue = cashTotal + transferTotal;

        var allBranchRefunds = RefundService.GetAll(branch == "ALL" ? null : branch);
        var periodRefunds = allBranchRefunds
            .Where(r => {
                var rDate = DateHelper.NormalizeToGregorian(r.ProcessedAt ?? r.RequestedAt).Date;
                return rDate >= filterStart && rDate <= filterEnd;
            })
            .ToList();
        decimal refundedTotalAmount = periodRefunds.Where(r => r.Status == "Approved").Sum(r => r.Amount);
        decimal pendingRefundAmount = allBranchRefunds.Where(r => r.Status == "Pending").Sum(r => r.Amount);
        decimal grossRevenueBeforeRefund = totalRevenue + refundedTotalAmount;

        var allValidPaidInPeriod = periodPayments
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();
        decimal bkkTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        var branchAllValidPaid = (branch == "ALL" ? allPayments : allPayments.Where(p => p.Booking?.Room?.Branch == branch))
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();

        var activeDatesWithData = branchAllValidPaid
            .Select(p => GetNormalizedPaymentDate(p).Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(5)
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToList();

        var trendLabels = new List<string>();
        var trendData = new List<decimal>();

        if (period == "Weekly")
        {
            string[] dayNamesTh = { "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส.", "อา." };
            for (int i = 0; i < 7; i++)
            {
                var targetDay = filterStart.AddDays(i);
                string label = $"{dayNamesTh[i]} {targetDay:dd/MM}";
                decimal sum = branchAllValidPaid
                    .Where(p => GetNormalizedPaymentDate(p).Date == targetDay)
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }
        else if (period == "Monthly")
        {
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = filterStart.AddMonths(-i);
                int targetYear = monthDate.Year;
                int targetMonth = monthDate.Month;
                string label = monthDate.ToString("MMM yyyy");
                decimal sum = branchAllValidPaid
                    .Where(p => {
                        var d = GetNormalizedPaymentDate(p);
                        return d.Year == targetYear && d.Month == targetMonth;
                    })
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }
        else
        {
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = filterStart.AddDays(-i);
                string label = targetDate.ToString("dd MMM");
                decimal sum = branchAllValidPaid
                    .Where(p => GetNormalizedPaymentDate(p).Date == targetDate)
                    .Sum(p => p.Amount);
                trendLabels.Add(label);
                trendData.Add(sum);
            }
        }

        ViewBag.GrossRevenue = grossRevenueBeforeRefund;
        ViewBag.RefundedTotalAmount = refundedTotalAmount;
        ViewBag.PendingRefundAmount = pendingRefundAmount;
        ViewBag.PendingPaymentAmount = pendingAmount;
        ViewBag.PendingPaymentCount = pendingPayments.Count;
        ViewBag.PaidTransactionCount = validPaidPayments.Count;
        ViewBag.CashCount = cashCount;
        ViewBag.DigitalCount = digitalCount;
        ViewBag.RefDateStr = refDate.ToString("yyyy-MM-dd");
        ViewBag.RefMonthStr = refDate.ToString("yyyy-MM");
        ViewBag.FilterStartStr = filterStart.ToString("dd/MM/yyyy");
        ViewBag.FilterEndStr = filterEnd.ToString("dd/MM/yyyy");
        ViewBag.PrevDateParam = prevDateParam;
        ViewBag.NextDateParam = nextDateParam;
        ViewBag.CurrentPeriodDisplay = currentPeriodDisplay;
        ViewBag.ActiveDatesWithData = activeDatesWithData;
        var allRefunds = allBranchRefunds;

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendReportToAdmin(string periodType, string branch, string periodLabel, string? notes, string? reportDate = null)
    {
        string userName = HttpContext.Session.GetString("FullName") ?? "ฝ่ายบัญชีและการเงิน";

        DateTime refDate = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(reportDate) && DateTime.TryParse(reportDate, out var parsedRef))
        {
            refDate = DateHelper.NormalizeToGregorian(parsedRef).Date;
        }

        DateTime filterStart;
        DateTime filterEnd;
        if (periodType == "Weekly")
        {
            int diffToMonday = (int)refDate.DayOfWeek == 0 ? 6 : (int)refDate.DayOfWeek - 1;
            filterStart = refDate.AddDays(-diffToMonday).Date;
            filterEnd = filterStart.AddDays(6).Date;
        }
        else if (periodType == "Monthly")
        {
            filterStart = new DateTime(refDate.Year, refDate.Month, 1);
            filterEnd = filterStart.AddMonths(1).AddDays(-1);
        }
        else
        {
            filterStart = refDate.Date;
            filterEnd = refDate.Date;
        }

        var allPayments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking != null && p.Booking.BookingStatus != "Refunded" && p.Booking.BookingStatus != "Cancelled")
            .ToList()
            .Where(p => {
                var d = GetNormalizedPaymentDate(p).Date;
                return d >= filterStart && d <= filterEnd;
            })
            .ToList();

        var filteredPayments = allPayments;
        if (!string.IsNullOrEmpty(branch) && branch != "ALL")
        {
            filteredPayments = filteredPayments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        decimal cashRev = filteredPayments.Where(p => IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        decimal transRev = filteredPayments.Where(p => !IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        decimal totalRev = cashRev + transRev;
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

        return RedirectToAction(nameof(RevenueReport), new { branch = branch, period = periodType, date = reportDate });
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
            var booking = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == refund.BookingId);

            if (booking != null)
            {
                booking.BookingStatus = "Refunded";

                var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                if (latestPayment != null)
                {
                    latestPayment.PaymentStatus = "Refunded";
                }

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
