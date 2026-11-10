using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinalProject.MVC.Data;

/// <summary>
/// Removes only explicitly allow-listed development throwaway accounts.
/// Both Identity id and email must match; SuperAdmin/Admin accounts are never removed.
/// </summary>
public static class ExactDevAccountRemover
{
    private static readonly (string Id, string Email)[] ExactAccounts =
    [
        ("f1c1172d-3056-42c2-90b9-9895438db2a6", "test@gmail.com"),
        ("da218635-c871-400a-8af5-c456174d0a1a", "testt@gmail.com")
    ];

    public static async Task RemoveAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ExactDevAccountRemover");
        var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        foreach (var (id, email) in ExactAccounts)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                continue;
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Skipped allow-listed id {UserId}: email no longer matches the expected development account.",
                    id);
                continue;
            }

            if (await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin) ||
                await userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                logger.LogWarning("Skipped {Email}: elevated roles are never removed by this cleanup.", email);
                continue;
            }

            // Favorites live in the shared database but outside the MVC Identity context.
            await using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Favorites WHERE UserId = @userId";
                command.Parameters.AddWithValue("@userId", id);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                logger.LogInformation("Removed development test account {Email}.", email);
            }
            else
            {
                logger.LogWarning(
                    "Could not remove development test account {Email}: {Errors}",
                    email,
                    string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
