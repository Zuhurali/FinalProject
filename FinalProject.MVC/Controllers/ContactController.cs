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

        var userId = await ResolveAuthenticatedUserIdAsync();
        bool saved;
        int? id;
        Dictionary<string, string> errors;
        string? error;
        try
        {
            (saved, id, errors, error) = await _contactRequests.SaveAsync(model, userId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Contact inquiry from {Email} could not be stored.", model.Email);
            return new JsonResult(new
            {
                success = false,
                emailSent = false,
                message = ContactFormOutcome.SaveFailed
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        if (!saved)
        {
            if (errors.Count > 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    emailSent = false,
                    message = "Please correct the highlighted fields.",
                    errors
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            _logger.LogWarning("Contact inquiry from {Email} was not stored: {Error}", model.Email, error);
            return new JsonResult(new
            {
                success = false,
                emailSent = false,
                message = error ?? ContactFormOutcome.SaveFailed
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        var (emailed, mailError) = await _mailer.SendAsync(model, cancellationToken);
        var emailUnavailable = !emailed
            && string.Equals(mailError, ContactInquiryMailer.NotConfiguredMessage, StringComparison.Ordinal);
        var outcome = ContactFormOutcome.AfterSuccessfulSave(emailed, emailUnavailable);

        if (emailed)
        {
            _logger.LogInformation(
                "Contact request #{Id} saved and emailed from {Name} <{Email}> user {UserId} interest {Interest}",
                id,
                model.Name,
                model.Email,
                userId ?? "(anonymous)",
                model.Car);
        }
        else
        {
            _logger.LogWarning(
                "Contact request #{Id} was saved but email was not sent: {Error}",
                id,
                mailError);
        }

        return new JsonResult(new
        {
            success = outcome.Success,
            emailSent = outcome.EmailSent,
            message = outcome.Message
        })
        {
            StatusCode = outcome.StatusCode
        };
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
