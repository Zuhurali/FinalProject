using System.Globalization;
using System.Security.Claims;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Services;

/// <summary>
/// Recent Activity on the account dashboard is generated from listings, favorites,
/// and messages. This store only hides feed entries for the signed-in user via
/// Identity claims — it never deletes chats, cars, or favorites.
/// </summary>
public sealed class AccountActivityHistory
{
    public const string ClearedAtClaimType = "activity_cleared_at";
    public const string HiddenItemClaimType = "activity_hidden";

    private readonly UserManager<ApplicationUser> _userManager;

    public AccountActivityHistory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public static string BuildKey(string type, string? url, DateTime createdAt) =>
        $"{type}|{url ?? string.Empty}|{createdAt.Ticks}";

    public async Task<IReadOnlyList<AccountActivityDto>> FilterAsync(
        ApplicationUser user,
        IEnumerable<AccountActivityDto> activities,
        CancellationToken cancellationToken = default)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var clearedAt = ReadClearedAt(claims);
        var hidden = claims
            .Where(claim => claim.Type == HiddenItemClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        return activities
            .Where(activity =>
            {
                var key = ResolveKey(activity);
                if (hidden.Contains(key))
                {
                    return false;
                }

                return clearedAt == null || activity.CreatedAt > clearedAt.Value;
            })
            .ToList();
    }

    public async Task ClearAllAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var existing = claims.Where(claim => claim.Type == ClearedAtClaimType).ToList();
        foreach (var claim in existing)
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        var hidden = claims.Where(claim => claim.Type == HiddenItemClaimType).ToList();
        foreach (var claim in hidden)
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        await _userManager.AddClaimAsync(
            user,
            new Claim(
                ClearedAtClaimType,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
    }

    public async Task<bool> HideAsync(
        ApplicationUser user,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 500)
        {
            return false;
        }

        var normalized = key.Trim();
        var claims = await _userManager.GetClaimsAsync(user);
        if (claims.Any(claim =>
                claim.Type == HiddenItemClaimType
                && string.Equals(claim.Value, normalized, StringComparison.Ordinal)))
        {
            return true;
        }

        await _userManager.AddClaimAsync(user, new Claim(HiddenItemClaimType, normalized));
        return true;
    }

    public static string ResolveKey(AccountActivityDto activity) =>
        string.IsNullOrWhiteSpace(activity.Key)
            ? BuildKey(activity.Type, activity.Url, activity.CreatedAt)
            : activity.Key.Trim();

    private static DateTime? ReadClearedAt(IList<Claim> claims)
    {
        var value = claims
            .FirstOrDefault(claim => claim.Type == ClearedAtClaimType)
            ?.Value;
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var clearedAt)
            ? clearedAt.ToUniversalTime()
            : null;
    }
}
