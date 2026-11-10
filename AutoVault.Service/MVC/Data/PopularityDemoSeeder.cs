using FinalProject.MVC.Commerce;
using FinalProject.MVC.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinalProject.MVC.Data;

/// <summary>
/// Removes confirmed simulated popularity sales. Does not create demo rows and never
/// deletes genuine recorded sales (<c>IsDemo = 0</c>).
/// </summary>
public static class PopularityDemoSeeder
{
    /// <summary>
    /// Deletes only rows that are marked IsDemo and match the known demo note or demo recorder ids.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var repository = services.GetRequiredService<IApplicationRepository>();
        var demoNote = PopularityDemoPlan.DemoSaleNote;
        var demoRecorderPrefix = "demo-popularity-reviewer-";

        var removed = await repository.DeleteDemoPopularitySalesAsync(
            demoNote,
            demoRecorderPrefix,
            cancellationToken);

        if (removed > 0)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("PopularityDemoCleanup");
            logger?.LogInformation("Removed {Count} confirmed demo in-store sale row(s).", removed);
        }
    }
}
