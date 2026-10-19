using FinalProject.MVC.Commerce;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Data;

/// <summary>
/// Inserts admin-only demo sale rows when they are missing.
/// Demonstration reviewer accounts are not created. Real customer feedback is left untouched.
/// Running this again does not add duplicate sales.
/// </summary>
public static class PopularityDemoSeeder
{
    private static readonly DateTime DemoSoldAt = new(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        var products = await db.ShopProducts.AsNoTracking()
            .Select(product => new { product.Id, product.Price })
            .ToListAsync(cancellationToken);
        var protectedProducts = await db.Database.SqlQueryRaw<ProductIdRow>("""
            SELECT ProductId FROM ProductRatings WHERE IsDemo = 0
            UNION
            SELECT ProductId FROM ProductReviews WHERE IsDemo = 0
            """).ToListAsync(cancellationToken);
        var protectedIds = protectedProducts.Select(row => row.ProductId).ToHashSet();

        foreach (var product in products)
        {
            if (protectedIds.Contains(product.Id))
            {
                continue;
            }

            // Stored as a label on the sale row. It is not an account, and the column has no user foreign key.
            var recorderId = PopularityDemoPlan.ReviewerId(1);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                IF NOT EXISTS (
                    SELECT 1 FROM InPersonSales WHERE ProductId = {product.Id} AND IsDemo = 1)
                INSERT INTO InPersonSales (ProductId, Quantity, UnitPrice, RecordedByUserId, Note, SoldAt, IsDemo)
                VALUES (
                    {product.Id},
                    {PopularityDemoPlan.DemoSales(product.Id)},
                    {product.Price},
                    {recorderId},
                    {PopularityDemoPlan.DemoSaleNote},
                    {DemoSoldAt},
                    CAST(1 AS bit));
                """, cancellationToken);
        }
    }

    private sealed class ProductIdRow
    {
        public int ProductId { get; set; }
    }
}
