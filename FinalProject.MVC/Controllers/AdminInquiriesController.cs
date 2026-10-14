using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Inquiries")]
public sealed class AdminInquiriesController : Controller
{
    private readonly IShopCatalogService _catalog;

    public AdminInquiriesController(IShopCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Product questions - AutoVault";
        return View(await _catalog.GetInquiriesAsync(cancellationToken));
    }

    [HttpPost("Handled")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Handled(int id, bool handled, CancellationToken cancellationToken)
    {
        var updated = await _catalog.SetInquiryHandledAsync(id, handled, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? "Question status updated."
            : "Question not found.";
        return RedirectToAction(nameof(Index));
    }
}
