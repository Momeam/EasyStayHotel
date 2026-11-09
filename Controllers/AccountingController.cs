using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyStayHotel.Controllers;

public class AccountingController : Controller
{
    private readonly EasystaydbContext _context;
    public AccountingController(EasystaydbContext context)
    {
        _context = context;
    }
    public IActionResult RevenueReport()
    {
        return View();
    }
}
