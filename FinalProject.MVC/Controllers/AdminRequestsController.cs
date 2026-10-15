using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Requests")]
public sealed class AdminRequestsController : Controller
{
    private readonly IShopCatalogService _catalog;

    public AdminRequestsController(IShopCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Purchase requests - AutoVault";
        return View(await _catalog.GetPurchaseRequestsAsync(cancellationToken));
    }

    [HttpPost("Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, string status, CancellationToken cancellationToken)
    {
        var updated = await _catalog.SetPurchaseRequestStatusAsync(id, status, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? $"Request #{id} marked {status}."
            : "Request not found or status not allowed.";
        return RedirectToAction(nameof(Index));
    }
}
