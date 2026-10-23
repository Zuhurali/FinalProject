using System.Security.Claims;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class ContactController : Controller
{
    private readonly IContactRequestService _contactRequests;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactRequestService contactRequests,
        UserManager<ApplicationUser> userManager,
        ILogger<ContactController> logger)
    {
        _contactRequests = contactRequests;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Contact - AutoVault";
        return View(new ContactFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Send(
        [FromForm] string? name,
        [FromForm] string? email,
        [FromForm] string? phone,
        [FromForm] string? car,
        [FromForm] string? message,
        CancellationToken cancellationToken)
    {
        var model = new ContactFormModel
        {
            Name = name ?? string.Empty,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            Car = car ?? string.Empty,
            Message = message ?? string.Empty
        };

        var userId = await ResolveAuthenticatedUserIdAsync();

        var (ok, id, errors, error) = await _contactRequests.SaveAsync(model, userId, cancellationToken);
        if (!ok)
        {
            return new JsonResult(new
            {
                success = false,
                message = error ?? "Please correct the highlighted fields.",
                errors
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        _logger.LogInformation(
            "Contact request #{Id} saved from {Name} <{Email}> user {UserId} interest {Interest}",
            id,
            model.Name,
            model.Email,
            userId ?? "(anonymous)",
            model.Car);

        return new JsonResult(new
        {
            success = true,
            message = "Thank you. Your request was saved. Our team will get back to you shortly."
        });
    }

    private async Task<string?> ResolveAuthenticatedUserIdAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = _userManager.GetUserId(User)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        var user = await _userManager.GetUserAsync(User);
        return user?.Id;
    }
}
