using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
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
        await Moderate(token => _reviews.SetHiddenAsync(id, hidden, token, cancellationToken), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await Moderate(token => _reviews.DeleteReviewAsync(id, token, cancellationToken), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task Moderate(Func<string, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);
            await action(_tokenService.CreateToken(User, roles));
            TempData["Success"] = "The review was updated.";
        }
        catch (CarApiException exception)
        {
            TempData["Error"] = exception.Message;
        }
    }
}
