using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FinalProject.MVC.Data;

/// <summary>
/// Ensures Identity roles exist and optionally seeds or resets configured accounts.
/// Existing accounts are never recreated. Passwords change only when the matching
/// reset flag is true and a non-empty password is supplied from configuration.
/// </summary>
public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IEnumerable<(string Email, string Password, string Role, bool ResetPassword)> accounts)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>()
                             .CreateLogger("IdentityRoleSeeder");

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}': " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                logger.LogInformation("Created role {Role}.", roleName);
            }
        }

        var legacyUsers = await userManager.GetUsersInRoleAsync(AppRoles.LegacyUser);
        foreach (var legacy in legacyUsers)
        {
            if (!await userManager.IsInRoleAsync(legacy, AppRoles.Member))
            {
                await userManager.AddToRoleAsync(legacy, AppRoles.Member);
            }

            await userManager.RemoveFromRoleAsync(legacy, AppRoles.LegacyUser);
        }

        foreach (var (email, password, role, resetPassword) in accounts)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                if (resetPassword)
                {
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        logger.LogWarning(
                            "Password reset requested for {Email}, but no password was supplied. The existing password was left unchanged.",
                            email);
                    }
                    else
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                        var resetResult = await userManager.ResetPasswordAsync(existing, token, password);
                        if (!resetResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Could not reset password for '{email}': " +
                                string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                        }

                        await userManager.UpdateSecurityStampAsync(existing);
                        logger.LogInformation("Password reset applied for existing account {Email}.", email);
                    }
                }

                if (!await userManager.IsInRoleAsync(existing, role))
                {
                    await userManager.AddToRoleAsync(existing, role);
                    await userManager.UpdateSecurityStampAsync(existing);
                    logger.LogInformation("Assigned role {Role} to existing account {Email}.", role, email);
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Skipping {Role} account {Email}: no initial password supplied.", role, email);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var alreadyExists = createResult.Errors.Any(e =>
                    e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName");
                if (alreadyExists)
                {
                    logger.LogInformation("Concurrent seed detected for {Email} — account already exists, skipping.", email);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Could not create {role} account '{email}': " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Created {Role} account {Email}.", role, email);
        }
    }
}
