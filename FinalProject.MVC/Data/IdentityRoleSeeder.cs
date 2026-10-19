using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinalProject.MVC.Data;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var environment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityRoleSeeder");

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var created = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create the {roleName} role: {string.Join(", ", created.Errors.Select(error => error.Description))}");
                }
            }
        }

        var legacyRole = await roleManager.FindByNameAsync(AppRoles.LegacyUser);
        var memberRole = await roleManager.FindByNameAsync(AppRoles.Member);
        if (legacyRole != null && memberRole != null)
        {
            var legacyUsers = await userManager.GetUsersInRoleAsync(AppRoles.LegacyUser);
            foreach (var user in legacyUsers)
            {
                if (!await userManager.IsInRoleAsync(user, AppRoles.Member))
                {
                    await userManager.AddToRoleAsync(user, AppRoles.Member);
                }

                await userManager.RemoveFromRoleAsync(user, AppRoles.LegacyUser);
            }
        }

        var email = configuration["AdminSeed:SuperAdminEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var password = configuration["AdminSeed:SuperAdminPassword"];
        var resetPassword = configuration.GetValue("AdminSeed:ResetSuperAdminPassword", false);
        var superAdmin = await userManager.FindByEmailAsync(email);
        if (superAdmin == null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(superAdmin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create the configured SuperAdmin account: " +
                    string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
        else if (resetPassword)
        {
            await ResetExistingSuperAdminPasswordAsync(
                userManager,
                environment,
                logger,
                superAdmin,
                password);
        }

        if (!superAdmin.EmailConfirmed)
        {
            superAdmin.EmailConfirmed = true;
            await userManager.UpdateAsync(superAdmin);
        }

        if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
        {
            await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
        }
    }

    /// <summary>
    /// Development-only one-shot password reset. Uses Identity's reset token API.
    /// Enable only through User Secrets: AdminSeed:ResetSuperAdminPassword = true.
    /// </summary>
    private static async Task ResetExistingSuperAdminPasswordAsync(
        UserManager<ApplicationUser> userManager,
        IHostEnvironment environment,
        ILogger logger,
        ApplicationUser superAdmin,
        string? password)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogWarning(
                "Ignored AdminSeed:ResetSuperAdminPassword because the environment is {Environment}.",
                environment.EnvironmentName);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminSeed:ResetSuperAdminPassword is true, but AdminSeed:SuperAdminPassword is empty. " +
                "Set both values in User Secrets, then run the MVC project once.");
        }

        if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
        {
            throw new InvalidOperationException(
                $"Refusing to reset the password for {superAdmin.Email}: the account is not SuperAdmin.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(superAdmin);
        var reset = await userManager.ResetPasswordAsync(superAdmin, token, password);
        if (!reset.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to reset the SuperAdmin password: " +
                string.Join(", ", reset.Errors.Select(error => error.Description)));
        }

        if (await userManager.IsLockedOutAsync(superAdmin))
        {
            await userManager.SetLockoutEndDateAsync(superAdmin, null);
        }

        logger.LogWarning(
            "Development reset applied for SuperAdmin {Email}. " +
            "Sign in with that email or username, then set AdminSeed:ResetSuperAdminPassword to false " +
            "and remove AdminSeed:SuperAdminPassword from User Secrets.",
            superAdmin.Email);
    }
}
