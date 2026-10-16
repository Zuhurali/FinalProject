using FinalProject.MVC.Commerce;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinalProject.MVC.Models;

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
        ViewBag.Products = await _catalog.GetAllAsync(cancellationToken);
        var ranked = (await _catalog.GetAllAsync(cancellationToken)).ToList();
        await _popularity.ApplyAsync(ranked, cancellationToken);
        ViewBag.Ranking = PopularityRanking.ByPopularity(
            ranked,
            product => product.PopularityScore,
            product => product.SalesCount,
            product => product.Id).ToList();
        ViewBag.Formula = PopularityRanking.Explanation;
        return View(await _catalog.GetSalesAsync(cancellationToken));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int productId, int quantity, string? note, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var (ok, error) = await _catalog.RecordSaleAsync(productId, quantity, user.Id, note, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "The in-person sale was recorded and stock was reduced." : error;
        return RedirectToAction(nameof(Index));
    }
}
