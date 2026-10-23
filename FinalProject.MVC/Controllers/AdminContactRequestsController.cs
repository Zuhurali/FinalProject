using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/ContactRequests")]
public sealed class AdminContactRequestsController : Controller
{
    private readonly IContactRequestService _contactRequests;

    public AdminContactRequestsController(IContactRequestService contactRequests)
    {
        _contactRequests = contactRequests;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Contact Requests - AutoVault";
        return View(await _contactRequests.GetAllAsync(cancellationToken));
    }

    [HttpPost("Status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, string status, CancellationToken cancellationToken)
    {
        if (!ContactRequestStatus.IsValid(status))
        {
            TempData["Error"] = "Choose a valid status.";
            return RedirectToAction(nameof(Index));
        }

        var updated = await _contactRequests.SetStatusAsync(id, status, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? $"Contact request #{id} marked {status}."
            : "Contact request not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _contactRequests.DeleteAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Contact request #{id} was deleted."
            : "Contact request not found or was already deleted.";
        return RedirectToAction(nameof(Index));
    }
}
