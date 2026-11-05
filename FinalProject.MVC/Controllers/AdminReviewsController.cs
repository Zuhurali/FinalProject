using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Reviews")]
public sealed class AdminReviewsController : Controller
{
    private readonly IProductReviewApiService _reviews;
    private readonly IApiAccessTokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IShopCatalogService _catalog;

    public AdminReviewsController(
        IProductReviewApiService reviews,
        IApiAccessTokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IShopCatalogService catalog)
    {
        _reviews = reviews;
        _tokenService = tokenService;
        _userManager = userManager;
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Review Moderation - AutoVault";
        var products = await _catalog.GetAllAsync(cancellationToken);
        ViewBag.ProductNames = products.ToDictionary(product => product.Id, product => product.Name);
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.CreateToken(User, roles);
            return View(await _reviews.GetForModerationAsync(token, cancellationToken));
        }
        catch (CarApiException)
        {
            ViewData["ReviewServiceUnavailable"] = true;
            return View(new List<ProductReviewDto>());
        }
    }

    [HttpPost("Hide/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id, bool hidden, CancellationToken cancellationToken)
    {
        var wantsJson = string.Equals(Request.Headers["X-Requested-With"], "fetch", StringComparison.OrdinalIgnoreCase);
        try
        {
            var token = await CreateTokenAsync();
            if (token == null)
            {
                return wantsJson
                    ? Json(new { ok = false, error = "Sign in is required." })
                    : Challenge();
            }

            await _reviews.SetHiddenAsync(id, hidden, token, cancellationToken);
            if (wantsJson)
            {
                return Json(new { ok = true, hidden });
            }

            TempData["Success"] = hidden ? "The review is hidden." : "The review is visible again.";
        }
        catch (CarApiException exception)
        {
            if (wantsJson)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json(new { ok = false, error = exception.Message });
            }

            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var token = await CreateTokenAsync();
            if (token == null)
            {
                return Challenge();
            }

            await _reviews.DeleteReviewAsAdminAsync(id, token, cancellationToken);
            TempData["Success"] = $"Review #{id} was permanently deleted.";
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(exception.Message)
                ? "The review could not be deleted. Please try again."
                : exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("History/{id:int}")]
    public async Task<IActionResult> History(int id, CancellationToken cancellationToken)
    {
        try
        {
            var token = await CreateTokenAsync();
            if (token == null)
            {
                return Unauthorized(new { ok = false, error = "Sign in is required." });
            }

            var history = await _reviews.GetHistoryAsync(id, token, cancellationToken);
            return Json(new
            {
                ok = true,
                items = history.Select(item => new
                {
                    previousContent = item.PreviousContent,
                    editedAt = item.EditedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm") + " UTC"
                })
            });
        }
        catch (CarApiException exception)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { ok = false, error = exception.Message });
        }
    }

    private async Task<string?> CreateTokenAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return _tokenService.CreateToken(User, roles);
    }
}
