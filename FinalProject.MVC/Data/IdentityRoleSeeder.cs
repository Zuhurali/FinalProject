using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Data;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

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
}
