using FinalProject.Data;
using Xunit;

namespace FinalProject.Tests;

public class DatabaseOwnershipTests
{
    [Fact]
    public void Contexts_keep_separate_histories_and_the_api_does_not_migrate_mvc_tables()
    {
        Assert.Equal("__ApiMigrationsHistory", DatabaseOwnership.ApiHistoryTable);
        Assert.Equal("__EFMigrationsHistory", DatabaseOwnership.MvcHistoryTable);
        Assert.Contains("ShopProducts", DatabaseOwnership.MvcOwnedTables);
        Assert.Contains("ProductInquiries", DatabaseOwnership.MvcOwnedTables);
        Assert.Contains("Favorites", DatabaseOwnership.ApiOwnedTables);
        Assert.DoesNotContain("ProductInquiries", DatabaseOwnership.ApiOwnedTables);
        Assert.Contains("ShopProducts", DatabaseOwnership.ApiReadOnlySharedTables);

        var root = FindRepositoryRoot();
        var apiContext = File.ReadAllText(Path.Combine(root, "AutoVault.Repository", "Data", "FinalProjectDbContext.cs"));
        var mvcContext = File.ReadAllText(Path.Combine(root, "AutoVault.Repository", "MVC", "Data", "ApplicationDbContext.cs"));
        var apiProgram = File.ReadAllText(Path.Combine(root, "FinalProject", "Program.cs"));
        var mvcProgram = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Program.cs"));

        Assert.Contains("MigrationsHistoryTable = \"__ApiMigrationsHistory\"", apiContext, StringComparison.Ordinal);
        Assert.Contains("entity.ToTable(\"ShopProducts\", table => table.ExcludeFromMigrations())", apiContext, StringComparison.Ordinal);
        Assert.Contains("entity.ToTable(\"AspNetUsers\", table => table.ExcludeFromMigrations())", apiContext, StringComparison.Ordinal);
        Assert.Contains("entity.ToTable(\"ProductCampaigns\", table => table.ExcludeFromMigrations())", apiContext, StringComparison.Ordinal);
        Assert.Contains("entity.ToTable(\"InPersonSales\", table => table.ExcludeFromMigrations())", apiContext, StringComparison.Ordinal);
        Assert.Contains("entity.ToTable(\"ShowroomPurchaseRequests\", table => table.ExcludeFromMigrations())", apiContext, StringComparison.Ordinal);
        Assert.Contains("DbSet<ProductInquiry>", mvcContext, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<Car>", mvcContext, StringComparison.Ordinal);
        Assert.Contains("MigrationsHistoryTable(FinalProjectDbContext.MigrationsHistoryTable)", apiProgram, StringComparison.Ordinal);
        Assert.Contains("MigrationsAssembly(typeof(Program).Assembly.GetName().Name)", mvcProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.Migrate(", apiProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.Migrate(", mvcProgram, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find FinalProject.slnx.");
    }
}
