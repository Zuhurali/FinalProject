using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Faqs")]
public sealed class AdminFaqsController : Controller
{
    private readonly IFaqService _faqs;

    public AdminFaqsController(IFaqService faqs)
    {
        _faqs = faqs;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "FAQ Management - AutoVault";
        return View(await _faqs.GetAllAsync(cancellationToken));
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create FAQ - AutoVault";
        return View(new FaqItemEditViewModel { IsPublished = true, SortOrder = 0 });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FaqItemEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create FAQ - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error, _) = await _faqs.CreateAsync(model, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The FAQ could not be created.");
            return View(model);
        }

        TempData["Success"] = "FAQ item was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit FAQ - AutoVault";
        var item = await _faqs.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        return View(ToEditModel(item));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FaqItemEditViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit FAQ - AutoVault";
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _faqs.UpdateAsync(model, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The FAQ could not be updated.");
            return View(model);
        }

        TempData["Success"] = "FAQ item was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _faqs.DeleteAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"FAQ #{id} was deleted."
            : "FAQ item not found or was already deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish, CancellationToken cancellationToken)
    {
        var updated = await _faqs.SetPublishedAsync(id, publish, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? (publish ? $"FAQ #{id} is now published." : $"FAQ #{id} is now unpublished.")
            : "FAQ item not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Move/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
    {
        var (ok, error) = await _faqs.MoveAsync(id, direction, cancellationToken);
        if (!ok)
        {
            TempData["Error"] = error ?? "The FAQ order could not be changed.";
        }
        else
        {
            TempData["Success"] = "FAQ order was updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static FaqItemEditViewModel ToEditModel(FaqItem item) => new()
    {
        Id = item.Id,
        Question = item.Question,
        Answer = item.Answer,
        SortOrder = item.SortOrder,
        IsPublished = item.IsPublished
    };
}
