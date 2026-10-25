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

        ViewBag.CurrentUserId = UserManager.GetUserId(User);
        return View(models);
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Users/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string role)
    {
        var (ok, message) = await ApplyRoleChangeAsync(userId, role);
        TempData[ok ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Users/Roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRoles(List<AdminUserRoleChangeItem>? items)
    {
        items ??= [];
        if (items.Count == 0)
        {
            TempData["Error"] = "No role changes were submitted.";
            return RedirectToAction(nameof(Users));
        }

        var updated = 0;
        var errors = new List<string>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.UserId) || string.IsNullOrWhiteSpace(item.Role))
            {
                continue;
            }

            var (ok, message) = await ApplyRoleChangeAsync(item.UserId, item.Role);
            if (!ok)
            {
                errors.Add(message);
                continue;
            }

            if (!message.Contains("already has this role", StringComparison.OrdinalIgnoreCase))
            {
                updated++;
            }
        }

        if (updated > 0 && errors.Count == 0)
        {
            TempData["Success"] = updated == 1
                ? "1 user role was updated."
                : $"{updated} user roles were updated.";
        }
        else if (updated > 0)
        {
            TempData["Success"] =
                $"{updated} user role(s) were updated. Some changes could not be applied: {string.Join(" ", errors)}";
        }
        else if (errors.Count > 0)
        {
            TempData["Error"] = string.Join(" ", errors);
        }
        else
        {
            TempData["Success"] = "No role changes were needed.";
        }

        return RedirectToAction(nameof(Users));
    }

    private async Task<(bool Ok, string Message)> ApplyRoleChangeAsync(string userId, string role)
    {
        if (role is not (AppRoles.SuperAdmin or AppRoles.Admin or AppRoles.Member))
        {
            return (false, "Choose SuperAdmin, Admin, or Member.");
        }

        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        var currentUserId = UserManager.GetUserId(User);
        var roles = await UserManager.GetRolesAsync(user);
        var isSuperAdmin = roles.Contains(AppRoles.SuperAdmin);
        if (user.Id == currentUserId && role != AppRoles.SuperAdmin)
        {
            return (false, "You cannot remove your own SuperAdmin role.");
        }

        if (isSuperAdmin && role != AppRoles.SuperAdmin)
        {
            var superAdmins = await UserManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            if (superAdmins.Count <= 1)
            {
                return (false, "At least one SuperAdmin account must remain.");
            }
        }

        var changed = false;
        foreach (var existing in AppRoles.All)
        {
            if (existing != role && roles.Contains(existing))
            {
                var removed = await UserManager.RemoveFromRoleAsync(user, existing);
                if (!removed.Succeeded)
                {
                    return (false, string.Join(", ", removed.Errors.Select(error => error.Description)));
                }

                changed = true;
            }
        }

        if (!roles.Contains(role))
        {
            var added = await UserManager.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                return (false, string.Join(", ", added.Errors.Select(error => error.Description)));
            }

            changed = true;
        }

        // Leave the security stamp unchanged. The cookie validator rebuilds role
        // claims from the store on the next request, so this user stays signed in
        // and the SuperAdmin who made the change is not signed out either.
        return changed
            ? (true, "The user role was updated.")
            : (true, "That user already has this role.");
    }
}
