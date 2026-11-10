using FinalProject.MVC.Models;

namespace FinalProject.MVC.Security;

/// <summary>
/// Soft-deleted rows stay for FK history, but login identifiers must be freed
/// so the same email/username can register again.
/// </summary>
public static class SoftDeletedAccountIdentity
{
    public static void ReleaseLoginIdentifiers(ApplicationUser user)
    {
        var stamp = (user.Id ?? Guid.NewGuid().ToString("N")).Replace("-", "", StringComparison.Ordinal);
        user.Email = $"deleted-{stamp}@deleted.local";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.UserName = $"deleted_{stamp}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
    }

    public static bool IsReleasedIdentifier(string? emailOrUserName) =>
        !string.IsNullOrWhiteSpace(emailOrUserName)
        && (emailOrUserName.StartsWith("deleted-", StringComparison.OrdinalIgnoreCase)
            || emailOrUserName.StartsWith("deleted_", StringComparison.OrdinalIgnoreCase)
            || emailOrUserName.EndsWith("@deleted.local", StringComparison.OrdinalIgnoreCase));
}
