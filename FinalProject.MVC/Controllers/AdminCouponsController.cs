using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

/// <summary>
/// Coupons were removed from the admin UI. Old URLs redirect to the dashboard.
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Coupons")]
public sealed class AdminCouponsController : Controller
{
    [HttpGet("")]
    [HttpGet("Create")]
    [HttpGet("Edit/{id:int}")]
    public IActionResult Index()
    {
        TempData["Error"] = "Coupons were removed. Use Campaigns for in-store spare-parts discounts.";
        return RedirectToAction("Index", "Admin");
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save()
    {
        TempData["Error"] = "Coupons were removed.";
        return RedirectToAction("Index", "Admin");
    }

    [HttpPost("Deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Deactivate(int id)
    {
        TempData["Error"] = "Coupons were removed.";
        return RedirectToAction("Index", "Admin");
    }
}
