using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Security;

/// <summary>
/// Drops an authenticated session when the store says the email is not confirmed.
/// Sign-in already rejects that case; this closes cookies issued before the check.
/// Confirmed members and staff are left untouched. Nothing is written back to the user.
/// </summary>
public sealed class UnconfirmedAccountMiddleware
{
    private readonly RequestDelegate _next;

    public UnconfirmedAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(context.User);
            if (user == null || !user.EmailConfirmed)
            {
                await signIn.SignOutAsync();
                context.User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity());
            }
        }

        await _next(context);
    }
}
