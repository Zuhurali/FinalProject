using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Teams")]
public sealed class AdminTeamsController : Controller
{
    private readonly ITeamMemberService _teams;

    public AdminTeamsController(ITeamMemberService teams)
    {
        _teams = teams;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Team - AutoVault";
        return View(await _teams.GetAllAsync(cancellationToken));
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add team member - AutoVault";
        return View(new TeamMemberEditViewModel { IsPublished = true, SortOrder = 0 });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Create(TeamMemberEditViewModel model, IFormFile? image, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add team member - AutoVault";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error, _) = await _teams.CreateAsync(model, image, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The team member could not be created.");
            return View(model);
        }

        TempData["Success"] = "Team member was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit team member - AutoVault";
        var member = await _teams.GetByIdAsync(id, cancellationToken);
        if (member == null)
        {
            return NotFound();
        }

        return View(ToEditModel(member));
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, TeamMemberEditViewModel model, IFormFile? image, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit team member - AutoVault";
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (ok, error) = await _teams.UpdateAsync(model, image, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The team member could not be updated.");
            return View(model);
        }

        TempData["Success"] = "Team member was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _teams.DeleteAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Team member #{id} was deleted."
            : "Team member not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish, CancellationToken cancellationToken)
    {
        var updated = await _teams.SetPublishedAsync(id, publish, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? (publish ? $"Team member #{id} is published." : $"Team member #{id} is unpublished.")
            : "Team member not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Move/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int direction, CancellationToken cancellationToken)
    {
        var (ok, error) = await _teams.MoveAsync(id, direction, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "Team order was updated." : error ?? "Move failed.";
        return RedirectToAction(nameof(Index));
    }

    private static TeamMemberEditViewModel ToEditModel(TeamMember member) => new()
    {
        Id = member.Id,
        Name = member.Name,
        Position = member.Position,
        ImagePath = member.ImagePath,
        Biography = member.Biography,
        Experience = member.Experience,
        Department = member.Department,
        Phone = member.Phone,
        Email = member.Email,
        SortOrder = member.SortOrder,
        IsPublished = member.IsPublished
    };
}
