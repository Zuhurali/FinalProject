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
            product => product.RealSalesCount,
            product => product.FavoriteCount,
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

        var (ok, error) = await _catalog.RecordSaleAsync(
            productId,
            quantity,
            user.Id,
            note,
            submissionKey,
            couponCode: null,
            customerUserId: null,
            cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "The in-person sale was recorded and stock was reduced."
            : error;

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
