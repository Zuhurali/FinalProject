using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Coupons")]
public sealed class AdminCouponsController : Controller
{
    private readonly ICouponService _coupons;
    private readonly IShopCatalogService _catalog;

    public AdminCouponsController(ICouponService coupons, IShopCatalogService catalog)
    {
        _coupons = coupons;
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Coupons - AutoVault";
        await _coupons.EnsureSeedAsync(cancellationToken);
        return View(await _coupons.GetAllAsync(cancellationToken));
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Coupon - AutoVault";
        await LoadProducts(cancellationToken);
        return View("Edit", new Coupon
        {
            IsActive = true,
            DiscountPercent = 10m,
            MaxUsesPerUser = 1,
            AppliesToAllParts = true,
            MinimumPurchaseAmount = 0m
        });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Coupon - AutoVault";
        var coupon = await _coupons.GetByIdAsync(id, cancellationToken);
        if (coupon == null)
        {
            return NotFound();
        }

        await LoadProducts(cancellationToken);
        return View(coupon);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Coupon coupon, CancellationToken cancellationToken)
    {
        var (ok, error) = await _coupons.SaveAsync(coupon, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not save the coupon.");
            await LoadProducts(cancellationToken);
            ViewData["Title"] = coupon.Id > 0 ? "Edit Coupon - AutoVault" : "Create Coupon - AutoVault";
            return View("Edit", coupon);
        }

        TempData["Success"] = $"Coupon {coupon.Code} was saved. Applying it in the cart only previews the discount; redemption is recorded when an order completes successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Active(int id, bool isActive, CancellationToken cancellationToken)
    {
        var updated = await _coupons.SetActiveAsync(id, isActive, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? $"Coupon marked {(isActive ? "active" : "inactive")}."
            : "Coupon not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (ok, error) = await _coupons.DeleteAsync(id, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Coupon deleted."
            : error ?? "Coupon could not be deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProducts(CancellationToken cancellationToken)
    {
        var products = await _catalog.GetAllAsync(cancellationToken);
        ViewBag.Products = products
            .OrderBy(product => product.Name)
            .Select(product => new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = $"{product.Name} ({product.Price:C})"
            })
            .ToList();
    }
}
