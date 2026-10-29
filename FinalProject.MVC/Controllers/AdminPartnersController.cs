using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Partners")]
public sealed class AdminPartnersController : Controller
{
    private readonly IPartnerStripService _partners;

    public AdminPartnersController(IPartnerStripService partners) => _partners = partners;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Partners - AutoVault";
        var settings = _partners.GetSettings();
        ViewData["Settings"] = new PartnerStripSettingsEditViewModel
        {
            Eyebrow = settings.Eyebrow,
            HeadingBefore = settings.HeadingBefore,
            HeadingHighlight = settings.HeadingHighlight
        };
        return View(await _partners.GetAllItemsAsync(cancellationToken));
    }

    [HttpPost("Settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(PartnerStripSettingsEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Partner strip headings could not be saved.";
            return RedirectToAction(nameof(Index));
        }

        await _partners.SaveSettingsAsync(model, cancellationToken);
        TempData["Success"] = "Partner strip headings were saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add partner - AutoVault";
        return View(new PartnerStripItemEditViewModel { IsPublished = true });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Create(PartnerStripItemEditViewModel model, IFormFile? logo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add partner - AutoVault";
        if (!ModelState.IsValid) return View(model);
        var (ok, error, _) = await _partners.CreateItemAsync(model, logo, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not create partner.");
            return View(model);
        }

        TempData["Success"] = "Partner was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _partners.GetItemByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        ViewData["Title"] = "Edit partner - AutoVault";
        return View(new PartnerStripItemEditViewModel
        {
            Id = item.Id,
            Name = item.Name,
            LogoPath = item.LogoPath,
            LinkUrl = item.LinkUrl,
            SortOrder = item.SortOrder,
            IsPublished = item.IsPublished
        });
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, PartnerStripItemEditViewModel model, IFormFile? logo, CancellationToken cancellationToken)
    {
        model.Id = id;
        ViewData["Title"] = "Edit partner - AutoVault";
        if (!ModelState.IsValid) return View(model);
        var (ok, error) = await _partners.UpdateItemAsync(model, logo, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not update partner.");
            return View(model);
        }

        TempData["Success"] = "Partner was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _partners.DeleteItemAsync(id, cancellationToken);
        TempData["Success"] = "Partner was deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish, CancellationToken cancellationToken)
    {
        await _partners.SetItemPublishedAsync(id, publish, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Move/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
    {
        await _partners.MoveItemAsync(id, direction, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
