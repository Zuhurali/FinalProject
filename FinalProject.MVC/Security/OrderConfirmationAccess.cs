using System.Security.Claims;

namespace FinalProject.MVC.Security;

/// <summary>
/// Authorization for legacy order confirmation pages. Online checkout is disabled;
/// any remaining Orders rows must not be enumerable by id alone.
/// </summary>
public static class OrderConfirmationAccess
{
    public static bool CanView(string? orderUserId, ClaimsPrincipal? viewer)
    {
        if (viewer?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (viewer.IsInRole(AppRoles.Admin) || viewer.IsInRole(AppRoles.SuperAdmin))
        {
            return true;
        }

        var viewerId = viewer.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(orderUserId) || string.IsNullOrWhiteSpace(viewerId))
        {
            return false;
        }

        return string.Equals(orderUserId, viewerId, StringComparison.Ordinal);
    }
}
