using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Sales")]
public sealed class AdminSalesController : Controller
{
    private static readonly TimeSpan SubmissionKeyTtl = TimeSpan.FromHours(6);
    private readonly IShopCatalogService _catalog;
    private readonly IProductPopularityService _popularity;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _submissionCache;

    public AdminSalesController(
        IShopCatalogService catalog,
        IProductPopularityService popularity,
        UserManager<ApplicationUser> userManager,
        IMemoryCache submissionCache)
    {
        _catalog = catalog;
        _popularity = popularity;
        _userManager = userManager;
        _submissionCache = submissionCache;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "In-Person Sales - AutoVault";
        ViewBag.Products = await _catalog.GetAllAsync(cancellationToken);
        var ranked = (await _catalog.GetAllAsync(cancellationToken)).ToList();
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

        var cacheKey = "admin-sale-submit:" + submissionKey.ToString("N");
        if (_submissionCache.TryGetValue(cacheKey, out _))
        {
            TempData["Success"] = "That sale was already recorded. Stock was not changed again.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, error) = await _catalog.RecordSaleAsync(productId, quantity, user.Id, note, cancellationToken);
        if (ok)
        {
            _submissionCache.Set(cacheKey, true, SubmissionKeyTtl);
            TempData["Success"] = "The in-person sale was recorded and stock was reduced.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }
}
