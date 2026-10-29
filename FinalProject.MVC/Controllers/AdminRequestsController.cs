using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Requests")]
public sealed class AdminRequestsController : Controller
{
    private readonly IShopCatalogService _catalog;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminRequestsController(IShopCatalogService catalog, UserManager<ApplicationUser> userManager)
    {
        _catalog = catalog;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Purchase requests - AutoVault";
        ViewBag.StatusFilter = status;
        ViewBag.Statuses = ShowroomRequestStatus.All;
        return View(await _catalog.GetPurchaseRequestsAsync(status, cancellationToken));
    }

    [HttpPost("Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, string status, CancellationToken cancellationToken)
    {
        var (ok, error) = await _catalog.SetPurchaseRequestStatusAsync(id, status, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"Request #{id} marked {status}."
            : error ?? "Request not found or status not allowed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("CompleteSale/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSale(
        int id,
        string? note,
        string? couponCode,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var (ok, error) = await _catalog.CompletePurchaseRequestWithSaleAsync(
            id,
            user.Id,
            note,
            couponCode,
            cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? string.IsNullOrWhiteSpace(couponCode)
                ? $"Request #{id} completed and the in-store sale was recorded. Stock was reduced."
                : $"Request #{id} completed, stock reduced, and coupon redeemed in SQL."
            : error ?? "Could not complete that request with a sale.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _catalog.DeletePurchaseRequestAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Purchase request #{id} was deleted."
            : "Purchase request not found or was already deleted.";
        return RedirectToAction(nameof(Index));
    }
}
