using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Data;

/// <summary>
/// Inserts locked demonstration accounts and admin-only demo sale rows.
/// No ratings or reviews are created; real customer feedback is left untouched.
/// Running this again does not add duplicates.
/// </summary>
public static class PopularityDemoSeeder
{
    private static readonly DateTime OpinionAt = new(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DemoSoldAt = new(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        await EnsureReviewersAsync(users);

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

    private static async Task EnsureReviewersAsync(UserManager<ApplicationUser> users)
    {
        for (var number = 1; number <= PopularityDemoPlan.ReviewerCount; number++)
        {
            var id = PopularityDemoPlan.ReviewerId(number);
            if (await users.FindByIdAsync(id) != null)
            {
                continue;
            }

            var created = new ApplicationUser
            {
                Id = id,
                UserName = id,
                Email = $"{id}@example.com",
                EmailConfirmed = false,
                FirstName = "Demo",
                LastName = $"Reviewer {number}",
                CreatedAt = OpinionAt,
                LockoutEnabled = true,
                LockoutEnd = DateTimeOffset.MaxValue
            };
            var result = await users.CreateAsync(created, $"Demo{number:00}pass");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create a locked demonstration reviewer: " +
                    string.Join(", ", result.Errors.Select(error => error.Description)));
            }

            await users.SetLockoutEnabledAsync(created, true);
            await users.SetLockoutEndDateAsync(created, DateTimeOffset.MaxValue);
        }
    }

    private sealed class ProductIdRow
    {
        public int ProductId { get; set; }
    }
}
