using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Sales")]
public sealed class AdminSalesController : Controller
{
    private readonly IShopCatalogService _catalog;
    private readonly IProductPopularityService _popularity;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminSalesController(
        IShopCatalogService catalog,
        IProductPopularityService popularity,
        UserManager<ApplicationUser> userManager)
    {
        _catalog = catalog;
        _popularity = popularity;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "In-Person Sales - AutoVault";
        var products = (await _catalog.GetAllAsync(cancellationToken)).ToList();
        ViewBag.Products = products;
        var ranked = products.ToList();
        await _popularity.ApplyAsync(ranked, cancellationToken);
        ViewBag.Ranking = PopularityRanking.ByPopularity(
            ranked,
            product => product.PopularityScore,
            product => product.RealSalesCount,
            product => product.Id).ToList();
        ViewBag.Formula = PopularityRanking.Explanation;
        ViewBag.SubmissionKey = Guid.NewGuid();
        return View(await _catalog.GetSalesAsync(cancellationToken));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(
        int productId,
        int quantity,
        string? note,
        Guid submissionKey,
        string? couponCode,
        string? customerEmail,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (submissionKey == Guid.Empty)
        {
            TempData["Error"] = "That sale form expired. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        string? customerUserId = null;
        if (!string.IsNullOrWhiteSpace(couponCode) || !string.IsNullOrWhiteSpace(customerEmail))
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                TempData["Error"] = "Enter the customer's account email to redeem a coupon in store.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await _userManager.FindByEmailAsync(customerEmail.Trim());
            if (customer == null)
            {
                TempData["Error"] = "No customer account matches that email.";
                return RedirectToAction(nameof(Index));
            }

            customerUserId = customer.Id;
        }

        var (ok, error) = await _catalog.RecordSaleAsync(
            productId,
            quantity,
            user.Id,
            note,
            submissionKey,
            couponCode,
            customerUserId,
            cancellationToken);
        if (ok)
        {
            TempData["Success"] = string.IsNullOrWhiteSpace(couponCode)
                ? "The in-person sale was recorded and stock was reduced."
                : "The in-person sale was recorded, stock was reduced, and the coupon was redeemed in SQL.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Void/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var (ok, error) = await _catalog.VoidSaleAsync(id, user.Id, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "The sale was voided and stock was restored."
            : error;
        return RedirectToAction(nameof(Index));
    }
}
