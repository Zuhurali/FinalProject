using System.Security.Claims;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Inquiries")]
public sealed class AdminInquiriesController : Controller
{
    private readonly IShopCatalogService _catalog;

    public AdminInquiriesController(IShopCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Product questions - AutoVault";
        ViewBag.StatusFilter = status;
        ViewBag.Statuses = new[] { "New", "Answered", "Closed" };
        return View(await _catalog.GetInquiriesAsync(status, cancellationToken));
    }

    [HttpPost("Handled")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Handled(int id, bool handled, CancellationToken cancellationToken)
    {
        var updated = await _catalog.SetInquiryHandledAsync(id, handled, cancellationToken);
        TempData[updated ? "Success" : "Error"] = updated
            ? "Question status updated."
            : "Question not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _catalog.DeleteInquiryAsync(id, cancellationToken);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Product question #{id} was deleted."
            : "Product question not found or was already deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// AJAX endpoint — returns JSON so the admin UI can update inline without a full-page reload.
    /// Requires antiforgery token supplied as the X-XSRF-TOKEN header or form field.
    /// </summary>
    [HttpPost("Reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string reply, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Json(new { ok = false, error = "Not authenticated." });
        }

        var (ok, error) = await _catalog.SaveInquiryReplyAsync(id, reply, adminUserId, cancellationToken);
        if (!ok)
        {
            return Json(new { ok = false, error });
        }

        var inquiry = await _catalog.GetInquiryAsync(id, cancellationToken);

        return Json(new
        {
            ok = true,
            reply = inquiry?.AdminReply,
            repliedAt = inquiry?.AdminRepliedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
            statusLabel = inquiry?.StatusLabel ?? "Answered"
        });
    }
}
