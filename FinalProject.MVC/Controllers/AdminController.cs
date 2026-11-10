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
    private readonly ICarApiService _cars;
    private readonly IAdminDashboardService _dashboard;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IApiAccessTokenService tokenService,
        IAdminApiService adminApi,
        ICarApiService cars,
        IAdminDashboardService dashboard)
        : base(userManager, tokenService)
    {
        _adminApi = adminApi;
        _cars = cars;
        _dashboard = dashboard;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Admin Dashboard - AutoVault";

        var token = await CreateTokenAsync();
        if (token == null)
        {
            return Challenge();
        }

        var counts = await _dashboard.GetDashboardCountsAsync(cancellationToken);
        var model = new AdminDashboardViewModel
        {
            TotalMembers = await _dashboard.CountMembersInRoleAsync(AppRoles.Member, cancellationToken),
            TotalSpareParts = counts.TotalSpareParts,
            ActiveSpareParts = counts.ActiveSpareParts,
            NewContactRequests = counts.NewContactRequests,
            OpenProductQuestions = counts.OpenProductQuestions,
            RecordedSales = counts.RecordedSales,
            ActiveCampaigns = counts.ActiveCampaigns
        };

        try
        {
            model.Summary = await _adminApi.GetSummaryAsync(token, cancellationToken);
            model.CatalogSummaryAvailable = true;
        }
        catch (CarApiException)
        {
            model.ServiceUnavailable = true;
            model.CatalogSummaryAvailable = false;
        }

        return View(model);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("Users")]
    public async Task<IActionResult> Users(string? search, bool showDeleted = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Users - AutoVault";
        var usersQuery = UserManager.Users.AsQueryable();
        usersQuery = showDeleted
            ? usersQuery.Where(user => user.IsDeleted)
            : usersQuery.Where(user => !user.IsDeleted);

        var users = await usersQuery
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var models = new List<AdminUserViewModel>();
        foreach (var user in users)
        {
            if (!AdminUserSearch.Matches(
                    search,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber))
            {
                continue;
            }

            var roles = await UserManager.GetRolesAsync(user);
            models.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                Role = roles.Contains(AppRoles.SuperAdmin)
                    ? AppRoles.SuperAdmin
                    : roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.Member,
                CreatedAt = user.CreatedAt,
                IsLockedOut = await UserManager.IsLockedOutAsync(user),
                IsDeleted = user.IsDeleted,
                DeletedAt = user.DeletedAt,
                AccessFailedCount = user.AccessFailedCount,
                IsSuperAdmin = roles.Contains(AppRoles.SuperAdmin)
            });
        }

        ViewBag.CurrentUserId = UserManager.GetUserId(User);
        ViewBag.CanChangeRoles = User.IsInRole(AppRoles.SuperAdmin);
        return View(new AdminUsersPageViewModel
        {
            Search = search?.Trim(),
            ShowDeleted = showDeleted,
            Users = models
        });
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("UserDetails/{id}")]
    public async Task<IActionResult> UserDetails(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await UserManager.FindByIdAsync(id.Trim());
        if (user == null)
        {
            return NotFound();
        }

        ViewData["Title"] = "User Details - AutoVault";
        var roles = await UserManager.GetRolesAsync(user);
        var role = roles.Contains(AppRoles.SuperAdmin)
            ? AppRoles.SuperAdmin
            : roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.Member;

        var model = new AdminUserDetailsViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Role = role,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            LastPasswordChangedAt = user.LastPasswordChangedAt,
            LastPasswordResetAt = user.LastPasswordResetAt,
            IsLockedOut = await UserManager.IsLockedOutAsync(user),
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt,
            AccessFailedCount = user.AccessFailedCount
        };

        var token = await CreateTokenAsync();
        if (token != null)
        {
            try
            {
                var cars = await _cars.GetCarsByOwnerAsync(user.Id, token, cancellationToken);
                model.Listings = cars
                    .OrderByDescending(car => car.CreatedAt)
                    .Select(car => new AdminUserListingRow
                    {
                        Id = car.Id,
                        Title = $"{car.Brand?.Name} {car.Model} ({car.Year})".Trim(),
                        Status = car.Status,
                        CreatedAt = car.CreatedAt
                    })
                    .ToList();
            }
            catch (CarApiException)
            {
                model.ListingsUnavailable = true;
            }
        }
        else
        {
            model.ListingsUnavailable = true;
        }

        model.ContactRequests = (await _dashboard.GetContactRequestsForUserAsync(user.Id, cancellationToken))
            .Select(request => new AdminUserRequestRow
            {
                Id = request.Id,
                Summary = request.Summary,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            })
            .ToList();

        ViewBag.CanDeleteUsers = true;
        ViewBag.CurrentUserId = UserManager.GetUserId(User);
        return View(model);
    }

    /// <summary>
    /// Soft-deletes an account so it can no longer sign in. Historical rows are kept.
    /// Admin may delete Members only; SuperAdmin may delete Members/Admins (not the last SuperAdmin / self).
    /// </summary>
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPost("Users/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId, bool showDeleted = false, string? search = null)
    {
        var (ok, message) = await SoftDeleteUserAsync(userId);
        TempData[ok ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users), new { showDeleted, search });
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Users/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string role, bool showDeleted = false)
    {
        var (ok, message) = await ApplyRoleChangeAsync(userId, role);
        TempData[ok ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users), new { showDeleted });
    }

    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost("Users/Roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRoles(List<AdminUserRoleChangeItem>? items, bool showDeleted = false)
    {
        items ??= [];
        if (items.Count == 0)
        {
            TempData["Error"] = "No role changes were submitted.";
            return RedirectToAction(nameof(Users), new { showDeleted });
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

        return RedirectToAction(nameof(Users), new { showDeleted });
    }

    private async Task<(bool Ok, string Message)> SoftDeleteUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "User not found.");
        }

        var user = await UserManager.FindByIdAsync(userId.Trim());
        if (user == null)
        {
            return (false, "User not found.");
        }

        if (user.IsDeleted)
        {
            return (false, "That account is already deleted.");
        }

        var currentUserId = UserManager.GetUserId(User);
        if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            return (false, "You cannot delete your own account from the admin panel.");
        }

        var roles = await UserManager.GetRolesAsync(user);
        var targetIsSuperAdmin = roles.Contains(AppRoles.SuperAdmin);
        var targetIsAdmin = roles.Contains(AppRoles.Admin);
        var actorIsSuperAdmin = User.IsInRole(AppRoles.SuperAdmin);

        if (!actorIsSuperAdmin && (targetIsSuperAdmin || targetIsAdmin))
        {
            return (false, "Admins can only delete Member accounts.");
        }

        if (targetIsSuperAdmin)
        {
            var superAdmins = await UserManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            var activeSuperAdmins = superAdmins.Count(candidate => !candidate.IsDeleted);
            if (activeSuperAdmins <= 1)
            {
                return (false, "At least one SuperAdmin account must remain.");
            }
        }

        var label = string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        // Free email/username so the same address can register again.
        SoftDeletedAccountIdentity.ReleaseLoginIdentifiers(user);
        var update = await UserManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return (false, string.Join(", ", update.Errors.Select(error => error.Description)));
        }

        await UserManager.UpdateSecurityStampAsync(user);
        return (true, $"{label} was deleted and can no longer sign in. That email can be used to register again.");
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

        if (user.IsDeleted)
        {
            return (false, "Deleted accounts cannot have their roles changed.");
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
