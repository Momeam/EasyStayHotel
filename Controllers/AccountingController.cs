// นำเข้า System สำหรับการใช้งานคลาสพื้นฐาน เช่น DateTime, String, Math
using System;
// นำเข้า System.Diagnostics สำหรับการตรวจสอบและวินิจฉัยข้อผิดพลาด
using System.Diagnostics;
// นำเข้า System.Linq สำหรับฟังก์ชันคิวรี LINQ เช่น Where, Sum, Count, OrderByDescending
using System.Linq;
// นำเข้า Microsoft.AspNetCore.Mvc สำหรับโครงสร้าง MVC Controller และ Action Results
using Microsoft.AspNetCore.Mvc;
// นำเข้าโมเดลของ EasyStayHotel
using EasyStayHotel.Models;
// นำเข้าเซอร์วิสต่างๆ ของระบบ เช่น RefundService, FinancialReportService, AccountingFeedbackService
using EasyStayHotel.Services;
// นำเข้า Microsoft.EntityFrameworkCore สำหรับฟังก์ชัน Include, ThenInclude, ToList
using Microsoft.EntityFrameworkCore;
// นำเข้า Microsoft.AspNetCore.Http สำหรับจัดการ Session และ Request Context
using Microsoft.AspNetCore.Http;
// นำเข้า Microsoft.AspNetCore.Mvc.Filters สำหรับ ActionExecutingContext ใน OnActionExecuting
using Microsoft.AspNetCore.Mvc.Filters;

// กำหนดเนมสเปซ EasyStayHotel.Controllers
namespace EasyStayHotel.Controllers;

// คอนโทรลเลอร์สำหรับควบคุมระบบการเงิน รายได้ และการจัดทำรายงานของฝ่ายบัญชี (Accounting Console)
public class AccountingController : Controller
{
    // ตัวแปรจัดเก็บ DbContext สำหรับเข้าถึงฐานข้อมูล MySQL
    private readonly EasystaydbContext _context;

    // คอนสตรัคเตอร์รับ DbContext ผ่านระบบ Dependency Injection
    public AccountingController(EasystaydbContext context)
    {
        // กำหนดค่า DbContext ให้กับฟิลด์ภายในคลาส
        _context = context;
    }

    // ฟังก์ชันตรวจสอบสิทธิ์การเข้าใช้งาน (RBAC) ก่อนที่ Action ใดๆ ในคอนโทรลเลอร์นี้จะทำงาน
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // ดึงรหัส UserId ของผู้ใช้ที่ล็อกอินอยู่ใน Session ปัจจุบัน
        var userId = HttpContext.Session.GetInt32("UserId");
        // ดึงตำแหน่ง Role ของผู้ใช้จาก Session
        var role = HttpContext.Session.GetString("Role");

        // ตรวจสอบว่าผู้ใช้ยังไม่ได้เข้าสู่ระบบหรือไม่
        if (userId == null || string.IsNullOrEmpty(role))
        {
            // แจ้งเตือนให้เข้าสู่ระบบก่อน
            TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานหน้าระบบบัญชี";
            // จดจำ URL ปลายทางที่ต้องการเข้าถึง เพื่อส่งกลับมาหลังล็อกอินเสร็จ
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            // สั่งนำทาง (Redirect) ไปยังหน้าเข้าสู่ระบบ Login
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            // จบการทำงานทันที
            return;
        }

        // ตรวจสอบสิทธิ์ว่าบทบาทไม่ใช่ Accounting, Admin หรือ SuperAdmin หรือไม่
        if (role != "Accounting" && role != "Admin" && role != "SuperAdmin")
        {
            // แจ้งเตือนว่าไม่มีสิทธิ์เข้าถึงหน้าระบบบัญชี
            TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าระบบบัญชี";
            // หากเป็นพนักงานต้อนรับ (Staff) ให้ส่งไปยังหน้า BookingList ของพนักงาน
            if (role == "Staff")
            {
                context.Result = new RedirectToActionResult("BookingList", "Staff", null);
            }
            // หากเป็นแม่บ้าน ให้ส่งไปยังหน้า Dashboard ของแม่บ้าน
            else if (role == "Housekeeper" || role == "Maid")
            {
                context.Result = new RedirectToActionResult("Dashboard", "Housekeeper", null);
            }
            // หากเป็นลูกค้าหรือบทบาทอื่น ส่งกลับหน้าแรก Home
            else
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            // จบการทำงาน
            return;
        }

        // หากสิทธิ์ถูกต้อง ให้ดำเนินการประมวลผล Action ตามปกติ
        base.OnActionExecuting(context);
    }

    // ฟังก์ชันช่วยแปลงวันที่ของรายการชำระเงินให้เป็นปี ค.ศ. มาตรฐานเสมอ (แก้ปัญหาข้อมูลเก่าที่บันทึกเป็นปี พ.ศ. 2569)
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

    // ฟังก์ชันช่วยจัดหมวดหมู่ว่าเป็นเงินสดหรือไม่
    private static bool IsCashMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return false;
        string m = method.Trim().ToLower();
        return m == "cash" || m.Contains("เงินสด");
    }

    // เมธอดแสดงหน้ารายงานรายได้รวม กราฟแนวโน้ม และรายการคำขอคืนเงิน (รองรับการดูเฉพาะรายวัน รายสัปดาห์ รายเดือน และย้อนหลังได้)
    [HttpGet]
    public IActionResult RevenueReport(string? branch = "ALL", string? period = "Daily", string? date = null)
    {
        // กำหนดค่าเริ่มต้นของสาขาเป็น ALL หากไม่มีการระบุค่า
        branch = string.IsNullOrEmpty(branch) ? "ALL" : branch;
        // กำหนดค่าเริ่มต้นของช่วงเวลาเป็น Daily (รายวัน)
        period = string.IsNullOrEmpty(period) ? "Daily" : period;

        // ดึงข้อมูลรายการชำระเงินทั้งหมดจาก MySQL พร้อมเชื่อมตาราง Booking, Room และ User
        var allPayments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Include(p => p.Booking)
                .ThenInclude(b => b!.User)
            .OrderByDescending(p => p.PaymentId)
            .ToList();

        // ปรับวันที่ TransferDate ของทุกรายการในหน่วยความจำให้เป็นปี ค.ศ. มาตรฐานเดียวกัน
        foreach (var p in allPayments)
        {
            if (p.TransferDate.HasValue)
            {
                p.TransferDate = DateHelper.NormalizeToGregorian(p.TransferDate.Value);
            }
        }

        // ดึงวันที่ปัจจุบัน และแปลงวันที่เป้าหมาย (date) ที่ผู้ใช้เลือกดูย้อนหลัง
        var today = DateTime.Today;
        DateTime refDate = today;
        if (!string.IsNullOrWhiteSpace(date))
        {
            // รองรับทั้งรูปแบบ yyyy-MM-dd และ yyyy-MM (สำหรับรายเดือน)
            if (date.Length == 7 && DateTime.TryParse(date + "-01", out var parsedMonth))
            {
                refDate = DateHelper.NormalizeToGregorian(parsedMonth).Date;
            }
            else if (DateTime.TryParse(date, out var parsedDate))
            {
                refDate = DateHelper.NormalizeToGregorian(parsedDate).Date;
            }
        }

        // คำนวณวันเริ่มต้น (filterStart) และวันสิ้นสุด (filterEnd) ของงวดที่เลือก พร้อมลิงก์ย้อนหลัง/ถัดไป
        DateTime filterStart;
        DateTime filterEnd;
        string prevDateParam;
        string nextDateParam;
        string currentPeriodDisplay;

        if (period == "Weekly")
        {
            // คำนวณวันจันทร์ - วันอาทิตย์ ของสัปดาห์ที่เลือก
            int diffToMonday = (int)refDate.DayOfWeek == 0 ? 6 : (int)refDate.DayOfWeek - 1;
            filterStart = refDate.AddDays(-diffToMonday).Date;
            filterEnd = filterStart.AddDays(6).Date;
            prevDateParam = filterStart.AddDays(-7).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddDays(7).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"สัปดาห์วันที่ {filterStart:dd/MM/yyyy} - {filterEnd:dd/MM/yyyy}";
        }
        else if (period == "Monthly")
        {
            // คำนวณวันแรก - วันสุดท้าย ของเดือนที่เลือก
            filterStart = new DateTime(refDate.Year, refDate.Month, 1);
            filterEnd = filterStart.AddMonths(1).AddDays(-1);
            prevDateParam = filterStart.AddMonths(-1).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddMonths(1).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"ประจำเดือน {filterStart:MMMM yyyy}";
        }
        else
        {
            // รายวัน (Daily): แสดงเฉพาะวันเดียวตามวันที่เลือก (refDate)
            period = "Daily";
            filterStart = refDate.Date;
            filterEnd = refDate.Date;
            prevDateParam = filterStart.AddDays(-1).ToString("yyyy-MM-dd");
            nextDateParam = filterStart.AddDays(1).ToString("yyyy-MM-dd");
            currentPeriodDisplay = $"ประจำวันที่ {filterStart:dd/MM/yyyy}" + (filterStart == today ? " (วันนี้)" : "");
        }

        // กรองรายการชำระเงินเฉพาะในช่วงเวลาที่เลือก (รายวันนั้น / สัปดาห์นั้น / เดือนนั้น)
        var periodPayments = allPayments
            .Where(p => {
                var pDate = GetNormalizedPaymentDate(p).Date;
                return pDate >= filterStart && pDate <= filterEnd;
            })
            .ToList();

        // กำหนดตัวแปรสำหรับเก็บรายการชำระเงินที่จะนำไปคำนวณตามสาขาในช่วงเวลาที่เลือก
        var payments = periodPayments;
        if (branch != "ALL")
        {
            payments = payments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        // กรองเฉพาะรายการที่ชำระเงินสำเร็จจริงในช่วงเวลาที่เลือก (และไม่ได้ถูกคืนเงิน Refunded)
        var validPaidPayments = payments
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();

        // รายการที่รอพนักงานตรวจสอบสลิปในช่วงเวลาที่เลือก
        var pendingPayments = payments
            .Where(p => p.PaymentStatus == "Pending" && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();
        decimal pendingAmount = pendingPayments.Sum(p => p.Amount);

        // คำนวณยอดเงินสดรับจริงหน้าเคาน์เตอร์ (Cash) เฉพาะในงวดที่เลือก
        decimal cashTotal = validPaidPayments.Where(p => IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        int cashCount = validPaidPayments.Count(p => IsCashMethod(p.PaymentMethod));

        // คำนวณยอดเงินโอนและดิจิทัลทั้งหมด เฉพาะในงวดที่เลือก
        var digitalPayments = validPaidPayments.Where(p => !IsCashMethod(p.PaymentMethod)).ToList();
        decimal transferTotal = digitalPayments.Sum(p => p.Amount);
        int digitalCount = digitalPayments.Count;
        decimal promptpayTotal = digitalPayments.Where(p => string.IsNullOrEmpty(p.PaymentMethod) || p.PaymentMethod.Contains("PromptPay", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);
        decimal cardTotal = transferTotal - promptpayTotal;

        // คำนวณยอดรายรับสุทธิของงวดที่เลือก (Cash + Digital = TotalRevenue ตรงกัน 100%)
        decimal totalRevenue = cashTotal + transferTotal;

        // ดึงคำขอคืนเงินของลูกค้าตามสาขาและกรองตามช่วงเวลาที่เลือก
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

        // รายการชำระเงินสำเร็จของทุกสาขาในงวดที่เลือก เพื่อนำมาสรุปยอดรายรับเปรียบเทียบทั้ง 3 สาขา
        var allValidPaidInPeriod = periodPayments
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();
        decimal bkkTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketTotal = allValidPaidInPeriod.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        // ข้อมูลทั้งหมดของสาขาที่เลือกสำหรับพล็อตกราฟเปรียบเทียบย้อนหลังและสร้างปุ่มทางลัดวันที่ที่มีข้อมูล
        var branchAllValidPaid = (branch == "ALL" ? allPayments : allPayments.Where(p => p.Booking?.Room?.Branch == branch))
            .Where(p => (p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed" || p.PaymentStatus == "Approved")
                        && p.Booking?.BookingStatus != "Refunded" && p.Booking?.BookingStatus != "Cancelled")
            .ToList();

        // ดึงรายการวันที่ที่มีธุรกรรมรับเงินจริงล่าสุด 5 วัน เพื่อทำปุ่มกดสลับดูย้อนหลังได้ในคลิกเดียว
        var activeDatesWithData = branchAllValidPaid
            .Select(p => GetNormalizedPaymentDate(p).Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(5)
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToList();

        var trendLabels = new List<string>();
        var trendData = new List<decimal>();

        // กรณีเลือกดูกราฟแบบรายสัปดาห์ (Weekly) - แสดงรายรับทั้ง 7 วัน (จันทร์-อาทิตย์) ในสัปดาห์ที่เลือก
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
        // กรณีเลือกดูกราฟแบบรายเดือน (Monthly) - ย้อนหลัง 6 เดือนเทียบกับเดือนที่เลือก
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
        // กรณีเลือกดูกราฟแบบรายวัน (Daily) - แสดงย้อนหลัง 7 วันจนถึงวันที่เลือก (refDate)
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

        // ส่งค่ายอดรายรับรวมไปยัง View ผ่าน ViewBag
        ViewBag.TotalRevenue = totalRevenue;
        // ส่งยอดเงินสด
        ViewBag.CashTotal = cashTotal;
        // ส่งยอดเงินโอนดิจิทัลรวม
        ViewBag.TransferTotal = transferTotal;
        // ส่งยอดพร้อมเพย์
        ViewBag.PromptPayTotal = promptpayTotal;
        // ส่งยอดบัตรเครดิต
        ViewBag.CardTotal = cardTotal;
        // ส่งสาขาที่กำลังเลือกดู
        ViewBag.SelectedBranch = branch;
        // ส่งช่วงเวลาที่กำลังเลือกดู
        ViewBag.SelectedPeriod = period;

        // ส่งยอดรายรับของแต่ละสาขา
        ViewBag.BkkRevenue = bkkTotal;
        ViewBag.ChonRevenue = chonTotal;
        ViewBag.PhuketRevenue = phuketTotal;

        // ส่งป้ายกำกับและข้อมูลตัวเลขกราฟไปยัง View เพื่อให้ Chart.js นำไปเรนเดอร์
        ViewBag.TrendLabels = trendLabels;
        ViewBag.TrendData = trendData;

        // ดึงรายการรายงานปิดกะและ Feedback จากพนักงานต้อนรับ
        ViewBag.StaffFeedbacks = AccountingFeedbackService.GetAll(branch == "ALL" ? null : branch);
        // ส่งรายการคำขอคืนเงิน
        ViewBag.RefundRequests = allRefunds;
        // นับจำนวนคำขอคืนเงินที่ยังรอดำเนินการ (Pending)
        ViewBag.PendingRefundsCount = allRefunds.Count(r => r.Status == "Pending");

        // ดึงรายการรายงานสรุปการเงินที่เคยส่งให้ผู้จัดการ (Admin)
        ViewBag.FinancialReports = FinancialReportService.GetAll();
        // นับจำนวนรายงานการเงินที่รอผู้จัดการลงนามอนุมัติ
        ViewBag.PendingFinancialReportsCount = FinancialReportService.GetAll().Count(r => r.Status == "Pending");

        // ส่งข้อมูลรายการชำระเงินไปยังหน้า View
        return View(payments);
    }

    // เมธอดสำหรับจัดทำและส่งรายงานสรุปภาพรวมการเงินตรงไปยังหน้าแดชบอร์ดของผู้จัดการ (Admin) ตามงวดและวันที่ที่เลือก
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendReportToAdmin(string periodType, string branch, string periodLabel, string? notes, string? reportDate = null)
    {
        // ดึงชื่อของพนักงานบัญชีผู้จัดทำรายงานจาก Session
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

        // ดึงรายการชำระเงินที่ชำระสำเร็จแล้วในช่วงเวลาที่เลือก (ไม่นับรายการที่คืนเงินแล้ว)
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

        // ตัวแปรกรองตามสาขา
        var filteredPayments = allPayments;
        if (!string.IsNullOrEmpty(branch) && branch != "ALL")
        {
            filteredPayments = filteredPayments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        // คำนวณยอดเงินสดและเงินโอนดิจิทัลให้ดุลกัน 100%
        decimal cashRev = filteredPayments.Where(p => IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        decimal transRev = filteredPayments.Where(p => !IsCashMethod(p.PaymentMethod)).Sum(p => p.Amount);
        decimal totalRev = cashRev + transRev;
        // นับจำนวนรายการจองห้องพักทั้งหมดที่ไม่ซ้ำกัน
        int totalBookings = filteredPayments.Select(p => p.BookingId).Distinct().Count();

        // คำนวณรายได้แยกตาม 3 สาขาสำหรับประกอบรายงาน
        decimal bkkRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Bangkok").Sum(p => p.Amount);
        decimal chonRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Chonburi").Sum(p => p.Amount);
        decimal phuketRev = allPayments.Where(p => p.Booking?.Room?.Branch == "Phuket").Sum(p => p.Amount);

        // ดึงข้อมูลคำขอคืนเงินในสาขานั้น
        var refunds = RefundService.GetAll(branch == "ALL" ? null : branch);
        // คำนวณยอดเงินรวมที่อนุมัติคืนเงินไปแล้ว
        decimal refundAmount = refunds.Where(r => r.Status == "Approved").Sum(r => r.Amount);

        // สร้างอ็อบเจกต์รายงานการเงินใหม่
        var report = new FinancialReport
        {
            // วันเวลาที่สร้างรายงาน
            CreatedAt = DateTime.Now,
            // ผู้จัดทำรายงาน
            CreatedBy = userName,
            // ประเภทช่วงเวลา (Daily, Weekly, Monthly)
            PeriodType = periodType ?? "Daily",
            // สาขา
            Branch = branch ?? "ALL",
            // ข้อความหัวเรื่องรายงาน
            PeriodLabel = string.IsNullOrWhiteSpace(periodLabel) ? $"{periodType} Summary ณ {DateTime.Now:dd/MM/yyyy HH:mm}" : periodLabel.Trim(),
            // รายรับรวม
            TotalRevenue = totalRev,
            // ยอดเงินสด
            CashAmount = cashRev,
            // ยอดเงินโอน
            TransferAmount = transRev,
            // ยอดคืนเงิน
            RefundAmount = refundAmount,
            // จำนวนการจอง
            TotalBookings = totalBookings,
            // รายได้กรุงเทพฯ
            BangkokRevenue = bkkRev,
            // รายได้ชลบุรี
            ChonburiRevenue = chonRev,
            // รายได้ภูเก็ต
            PhuketRevenue = phuketRev,
            // หมายเหตุหรือข้อเสนอแนะของฝ่ายบัญชี
            Notes = notes?.Trim(),
            // สถานะเริ่มต้นของรายงานคือ Pending (รอผู้จัดการลงนามอนุมัติ)
            Status = "Pending"
        };

        // บันทึกรายงานผ่าน FinancialReportService ลงใน JSON Datastore
        bool saved = FinancialReportService.Create(report);
        // ตรวจสอบว่าบันทึกสำเร็จหรือไม่
        if (saved)
        {
            // บันทึกข้อความแจ้งเตือนความสำเร็จ
            TempData["SuccessMessage"] = $"ส่งรายงานสรุปการเงิน ({report.PeriodLabel}) ถึงผู้จัดการ (Admin) เรียบร้อยแล้ว! รหัสรายงาน: {report.ReportId}";
        }
        else
        {
            // บันทึกข้อความแจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกและส่งรายงาน";
        }

        // นำทางกลับไปยังหน้ารายงานรายได้เดิมพร้อมคงวันที่ที่เลือกไว้
        return RedirectToAction(nameof(RevenueReport), new { branch = branch, period = periodType, date = reportDate });
    }

    // เมธอดสำหรับฝ่ายบัญชีในการลงนามรับทราบรายงานส่งกะและยอดเงินสดจากพนักงานต้อนรับ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcknowledgeFeedback(string id)
    {
        // ดึงชื่อพนักงานบัญชีผู้ลงนามรับทราบ
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานบัญชี";
        // เรียกใช้เซอร์วิสเพื่อลงนามรับทราบและบันทึกเวลา
        bool result = AccountingFeedbackService.Acknowledge(id, userName);
        // ตรวจสอบผลลัพธ์
        if (result)
        {
            // แจ้งเตือนความสำเร็จ
            TempData["SuccessMessage"] = "บันทึกการรับทราบรายงานเรียบร้อยแล้ว";
        }
        // นำทางกลับสู่หน้ารายงาน
        return RedirectToAction(nameof(RevenueReport));
    }

    // เมธอดสำหรับอนุมัติการคืนเงินให้ลูกค้า พร้อมปรับสถานะการจองเป็น Refunded และปลดล็อกห้องว่างกลับเป็น Available
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveRefund(string refundId, string? adminNote, string? slipUrl)
    {
        // ดึงชื่อพนักงานบัญชีผู้อนุมัติจาก Session
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานฝ่ายบัญชี";
        // ค้นหาข้อมูลคำขอคืนเงินจาก JSON ด้วย RefundId
        var refund = RefundService.GetById(refundId);
        // ตรวจสอบว่าพบข้อมูลหรือไม่
        if (refund == null)
        {
            // หากไม่พบ ให้แจ้งข้อผิดพลาดและกลับสู่หน้ารายงาน
            TempData["ErrorMessage"] = "ไม่พบข้อมูลคำขอคืนเงินที่ระบุ";
            return RedirectToAction(nameof(RevenueReport));
        }

        // อนุมัติคำขอในระบบ RefundService และบันทึกประวัติการโอนคืน
        bool success = RefundService.Approve(refundId, userName, adminNote, slipUrl);
        // หากอนุมัติสำเร็จ
        if (success)
        {
            // ค้นหารายการจองในฐานข้อมูล MySQL พร้อมเชื่อมโยงห้องพักและการชำระเงิน
            var booking = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BookingId == refund.BookingId);

            // หากพบรายการจอง
            if (booking != null)
            {
                // เปลี่ยนสถานะการจองเป็น Refunded (คืนเงินแล้ว)
                booking.BookingStatus = "Refunded";

                // ค้นหารายการชำระเงินล่าสุดของการจองนี้
                var latestPayment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                // หากพบรายการชำระเงิน
                if (latestPayment != null)
                {
                    // เปลี่ยนสถานะการชำระเงินเป็น Refunded
                    latestPayment.PaymentStatus = "Refunded";
                }

                // หากมีการผูกห้องพักไว้ และห้องไม่ได้อยู่ในสถานะ Cleaning
                if (booking.Room != null && booking.Room.Status != "Cleaning")
                {
                    // ปลดล็อกสถานะห้องพักให้กลับมาเป็น Available (ว่างพร้อมขาย)
                    booking.Room.Status = "Available";
                }

                // บันทึกการเปลี่ยนแปลงทั้งหมดลงในฐานข้อมูล MySQL
                _context.SaveChanges();
            }

            // แจ้งข้อความสำเร็จพร้อมรายละเอียดบัญชีปลายทาง
            TempData["SuccessMessage"] = $"อนุมัติการคืนเงินรหัส {refund.BookingRef} ยอดเงิน ฿{refund.Amount:N2} สำเร็จแล้ว! บันทึกประวัติการโอนคืนเข้า {refund.BankName} ({refund.BankAccountNumber}) เรียบร้อย";
        }
        else
        {
            // แจ้งข้อผิดพลาดหากไม่สามารถอนุมัติได้
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการอนุมัติคำขอคืนเงิน";
        }

        // นำทางกลับไปยังหน้ารายงาน
        return RedirectToAction(nameof(RevenueReport));
    }

    // เมธอดสำหรับปฏิเสธคำขอคืนเงิน พร้อมบันทึกเหตุผลประกอบ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectRefund(string refundId, string rejectionReason)
    {
        // ดึงชื่อพนักงานฝ่ายบัญชีผู้ปฏิเสธ
        string userName = HttpContext.Session.GetString("FullName") ?? "พนักงานฝ่ายบัญชี";
        // ค้นหาข้อมูลคำขอคืนเงิน
        var refund = RefundService.GetById(refundId);
        // ตรวจสอบว่าพบข้อมูลหรือไม่
        if (refund == null)
        {
            // หากไม่พบ ให้แจ้งเตือนและกลับสู่หน้ารายงาน
            TempData["ErrorMessage"] = "ไม่พบข้อมูลคำขอคืนเงินที่ระบุ";
            return RedirectToAction(nameof(RevenueReport));
        }

        // ตรวจสอบว่ามีการระบุเหตุผลในการปฏิเสธหรือไม่
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            // แจ้งเตือนให้กรอกเหตุผล
            TempData["ErrorMessage"] = "กรุณาระบุเหตุผลในการปฏิเสธคำขอคืนเงิน";
            return RedirectToAction(nameof(RevenueReport));
        }

        // ปฏิเสธคำขอใน RefundService พร้อมบันทึกเหตุผล
        bool success = RefundService.Reject(refundId, userName, rejectionReason.Trim());
        // หากสำเร็จ
        if (success)
        {
            // ค้นหารายการจองใน MySQL
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == refund.BookingId);
            // หากพบรายการจอง
            if (booking != null)
            {
                // คืนสถานะการจองกลับมาเป็น Confirmed (ยืนยันการจองตามเดิม)
                booking.BookingStatus = "Confirmed";
                // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล MySQL
                _context.SaveChanges();
            }

            // แจ้งเตือนความสำเร็จ
            TempData["SuccessMessage"] = $"ปฏิเสธคำขอคืนเงินรหัส {refund.BookingRef} เรียบร้อยแล้ว (เหตุผล: {rejectionReason})";
        }
        else
        {
            // แจ้งเตือนข้อผิดพลาด
            TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการปฏิเสธคำขอคืนเงิน";
        }

        // นำทางกลับสู่หน้ารายงาน
        return RedirectToAction(nameof(RevenueReport));
    }
}
