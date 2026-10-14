using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Campaigns")]
public sealed class AdminCampaignsController : Controller
{
    private readonly IShopCatalogService _catalog;

    public AdminCampaignsController(IShopCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Campaigns - AutoVault";
        return View(await _catalog.GetCampaignsAsync(cancellationToken));
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "New Campaign - AutoVault";
        await LoadProducts(cancellationToken);
        return View("Edit", new ProductCampaign
        {
            StartsAt = DateTime.UtcNow.Date,
            EndsAt = DateTime.UtcNow.Date.AddDays(14),
            IsActive = true
        });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Campaign - AutoVault";
        var campaign = (await _catalog.GetCampaignsAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);
        if (campaign == null)
        {
            return NotFound();
        }

        await LoadProducts(cancellationToken);
        return View(campaign);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductCampaign campaign, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(campaign.Name))
        {
            ModelState.AddModelError(nameof(campaign.Name), "Enter a campaign name.");
        }

        if (!ModelState.IsValid)
        {
            await LoadProducts(cancellationToken);
            return View("Edit", campaign);
        }

        var (ok, error) = await _catalog.SaveCampaignAsync(campaign, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The campaign could not be saved.");
            await LoadProducts(cancellationToken);
            return View("Edit", campaign);
        }

        TempData["Success"] = "The campaign was saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Active(int id, bool isActive, CancellationToken cancellationToken)
    {
        if (!await _catalog.SetCampaignActiveAsync(id, isActive, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = isActive ? "The campaign is active." : "The campaign is inactive.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProducts(CancellationToken cancellationToken)
    {
        ViewBag.Products = await _catalog.GetAllAsync(cancellationToken);
    }
}
