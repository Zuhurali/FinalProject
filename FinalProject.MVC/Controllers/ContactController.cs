using System.Security.Claims;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class ContactController : Controller
{
    private readonly IContactRequestService _contactRequests;
    private readonly IContactInquiryMailer _mailer;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactRequestService contactRequests,
        IContactInquiryMailer mailer,
        UserManager<ApplicationUser> userManager,
        ILogger<ContactController> logger)
    {
        _contactRequests = contactRequests;
        _mailer = mailer;
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
        [FromForm] string? company,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(company))
        {
            return new JsonResult(new
            {
                success = false,
                message = "Please correct the highlighted fields."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var model = new ContactFormModel
        {
            Name = name ?? string.Empty,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            Car = car ?? string.Empty,
            Message = message ?? string.Empty
        };

        var fieldErrors = ContactFormRules.Validate(model);
        if (fieldErrors.Count > 0)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Please correct the highlighted fields.",
                errors = fieldErrors
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var (sent, mailError) = await _mailer.SendAsync(model, cancellationToken);
        if (!sent)
        {
            return new JsonResult(new
            {
                success = false,
                message = mailError ?? ContactInquiryMailer.DeliveryFailedMessage
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        var userId = await ResolveAuthenticatedUserIdAsync();
        var (ok, id, errors, error) = await _contactRequests.SaveAsync(model, userId, cancellationToken);
        if (!ok)
        {
            _logger.LogWarning(
                "Contact email was sent for {Email} but the request was not stored: {Error}",
                model.Email,
                error);
        }
        else
        {
            _logger.LogInformation(
                "Contact request #{Id} emailed and saved from {Name} <{Email}> user {UserId} interest {Interest}",
                id,
                model.Name,
                model.Email,
                userId ?? "(anonymous)",
                model.Car);
        }

        return new JsonResult(new
        {
            success = true,
            message = "Thank you. Your message was sent. Our team will get back to you shortly.",
            errors
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
