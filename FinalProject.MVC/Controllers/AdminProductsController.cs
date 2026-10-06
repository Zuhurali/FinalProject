using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Products")]
public sealed class AdminProductsController : AdminControllerBase
{
    private readonly IShopCatalogService _catalog;

    public AdminProductsController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        IShopCatalogService catalog)
        : base(userManager, tokenService)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Admin Products - AutoVault";
        return View(await _catalog.GetAllAsync(cancellationToken));
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Product - AutoVault";
        return View(new ShopCatalogProduct { InStock = true });
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShopCatalogProduct product, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        await _catalog.CreateAsync(product, cancellationToken);
        TempData["Success"] = "The product was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Product - AutoVault";
        var product = await _catalog.GetRecordAsync(id, cancellationToken);
        return product == null ? NotFound() : View(product);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ShopCatalogProduct product, CancellationToken cancellationToken)
    {
        product.Id = id;
        if (!ModelState.IsValid)
        {
            return View(product);
        }

        if (!await _catalog.UpdateAsync(product, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = "The product was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await _catalog.DeleteAsync(id, cancellationToken))
        {
            TempData["Error"] = "The product was not found.";
        }
        else
        {
            TempData["Success"] = "The product was deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
