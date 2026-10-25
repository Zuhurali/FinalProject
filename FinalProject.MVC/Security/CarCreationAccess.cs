using System.Security.Claims;

namespace FinalProject.MVC.Security;

public static class CarCreationAccess
{
    public static bool CanCreateCars(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true
        && (user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SuperAdmin));
}
