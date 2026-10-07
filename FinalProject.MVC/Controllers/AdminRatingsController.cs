using FinalProject.MVC.Models;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Ratings")]
public sealed class AdminRatingsController : Controller
{
    private readonly IProductRatingApiService _ratingApiService;
    private readonly IApiAccessTokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IShopCatalogService _shopCatalog;

    public AdminRatingsController(
        IProductRatingApiService ratingApiService,
        IApiAccessTokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IShopCatalogService shopCatalog)
    {
        _ratingApiService = ratingApiService;
        _tokenService = tokenService;
        _userManager = userManager;
        _shopCatalog = shopCatalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Manage Product Ratings - AutoVault";
        var catalog = await _shopCatalog.GetAllAsync(cancellationToken);
        var productNames = catalog.ToDictionary(product => product.Id, product => product.Name);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(User, roles);
            var requests = catalog.Select(product =>
                _ratingApiService.GetRatingsForAdminAsync(
                    product.Id,
                    accessToken,
                    cancellationToken));
            var summaries = await Task.WhenAll(requests);
            var ratings = summaries
                .Where(summary => summary != null)
                .SelectMany(summary => summary!.Ratings)
                .Select(rating => new AdminProductRatingViewModel
                {
                    Id = rating.Id,
                    ProductId = rating.ProductId,
                    ProductName = productNames.GetValueOrDefault(
                        rating.ProductId,
                        $"Product #{rating.ProductId}"),
                    UserId = rating.UserId,
                    UserDisplayName = rating.UserDisplayName,
                    Rating = rating.Rating,
                    CreatedAt = rating.CreatedAt,
                    UpdatedAt = rating.UpdatedAt
                })
                .OrderByDescending(rating => rating.UpdatedAt)
                .ToList();

            return View(ratings);
        }
        catch (CarApiException)
        {
            ViewData["RatingServiceUnavailable"] = true;
            return View(new List<AdminProductRatingViewModel>());
        }
    }

    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int productId,
        int ratingId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        try
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(User, roles);
            await _ratingApiService.DeleteRatingAsync(
                productId,
                ratingId,
                accessToken,
                cancellationToken);
            TempData["Success"] = "The product rating was deleted.";
        }
        catch (CarApiException)
        {
            TempData["Error"] = "The rating could not be deleted. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}
