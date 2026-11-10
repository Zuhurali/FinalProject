using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

/// <summary>
/// Legacy online-order archive was removed from admin navigation.
/// New sales are recorded under In-Store Sales.
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Orders")]
public sealed class AdminOrdersController : Controller
{
    [HttpGet("")]
    [HttpGet("{id:int}")]
    public IActionResult Index(int? id = null)
    {
        TempData["Error"] =
            "Legacy Orders was removed. AutoVault does not take online payment — use In-Store Sales.";
        return RedirectToAction("Index", "Admin");
    }
}
