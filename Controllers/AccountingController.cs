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
            else
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            return;
        }

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult RevenueReport(string? branch = null)
    {
        var payments = _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b!.Room)
            .Include(p => p.Booking)
                .ThenInclude(b => b!.User)
            .OrderByDescending(p => p.PaymentId)
            .ToList();

        if (!string.IsNullOrEmpty(branch))
        {
            payments = payments.Where(p => p.Booking?.Room?.Branch == branch).ToList();
        }

        decimal totalRevenue = payments.Where(p => p.PaymentStatus == "Paid" || p.PaymentStatus == "Confirmed").Sum(p => p.Amount);
        decimal cashTotal = payments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
        decimal transferTotal = payments.Where(p => p.PaymentMethod == "PromptPay" || p.PaymentMethod == "CreditCard").Sum(p => p.Amount);

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.CashTotal = cashTotal;
        ViewBag.TransferTotal = transferTotal;
        ViewBag.SelectedBranch = branch ?? "ALL";
        ViewBag.StaffFeedbacks = AccountingFeedbackService.GetAll(branch == "ALL" ? null : branch);

        return View(payments);
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
}
