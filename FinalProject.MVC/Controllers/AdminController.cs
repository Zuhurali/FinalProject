using FinalProject.MVC.Models;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin")]
public sealed class AdminController : AdminControllerBase
{
    private readonly IAdminApiService _adminApi;
    private readonly IShopCatalogService _shopCatalog;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        IAdminApiService adminApi,
        IShopCatalogService shopCatalog)
        : base(userManager, tokenService)
    {
        _adminApi = adminApi;
        _shopCatalog = shopCatalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Admin Dashboard - AutoVault";
        var model = new AdminDashboardViewModel
        {
            TotalMembers = (await UserManager.GetUsersInRoleAsync(AppRoles.Member)).Count,
            TotalProducts = await _shopCatalog.CountAsync(cancellationToken)
        };

        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        try
        {
            model.Summary = await _adminApi.GetSummaryAsync(token, cancellationToken);
        }
        catch (CarApiException)
        {
            model.ServiceUnavailable = true;
        }

        return View(model);
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpGet("Users")]
    public async Task<IActionResult> Users()
    {
        ViewData["Title"] = "Users - AutoVault";
        var users = await UserManager.Users.OrderBy(user => user.Email).ToListAsync();
        var models = new List<AdminUserViewModel>();
        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);
            models.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                EmailConfirmed = user.EmailConfirmed,
                Role = roles.Contains(AppRoles.SuperAdmin)
                    ? AppRoles.SuperAdmin
                    : roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.Member,
                CreatedAt = user.CreatedAt,
                IsSuperAdmin = roles.Contains(AppRoles.SuperAdmin)
            });
        }

        return View(models);
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Users/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        if (role is not (AppRoles.Admin or AppRoles.Member))
        {
            TempData["Error"] = "Choose Member or Admin.";
            return RedirectToAction(nameof(Users));
        }

        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (await UserManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            TempData["Error"] = "A SuperAdmin account cannot be changed from this page.";
            return RedirectToAction(nameof(Users));
        }

        if (await UserManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            await UserManager.RemoveFromRoleAsync(user, AppRoles.Admin);
        }

        if (await UserManager.IsInRoleAsync(user, AppRoles.Member))
        {
            await UserManager.RemoveFromRoleAsync(user, AppRoles.Member);
        }

        var result = await UserManager.AddToRoleAsync(user, role);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "The user role was updated."
            : string.Join(", ", result.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Users));
    }
}
