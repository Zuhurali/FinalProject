using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Mappings;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FinalProject.Tests;

/// <summary>
/// SQL Server integration checks for recent persistence fixes.
/// Read-only against production/dev rows except additive create/delete of a disposable partner.
/// </summary>
[Collection("CampaignSql")]
public class SqlPersistenceVerificationTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Lookups_categories_match_sql_categories_table()
    {
        await using var db = CreateApiDb();
        var fromTable = await db.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new { category.Id, category.Name, category.IconPath })
            .ToListAsync();
        Assert.NotEmpty(fromTable);

        var lookups = await new CarService(db, new SilentNotifications()).GetLookupsAsync();
        Assert.Equal(fromTable.Count, lookups.Categories.Count);
        Assert.Equal(
            fromTable.Select(category => category.Id).ToList(),
            lookups.Categories.Select(category => category.Id).ToList());
        Assert.Equal(
            fromTable.Select(category => category.Name).ToList(),
            lookups.Categories.Select(category => category.Name).ToList());
        Assert.Equal(
            fromTable.Select(category => category.IconPath ?? string.Empty).ToList(),
            lookups.Categories.Select(category => category.IconPath ?? string.Empty).ToList());
        Assert.Contains(
            fromTable,
            category => category.Name.Equals("Sedan", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(category.IconPath));
    }

    [Fact]
    public async Task Sql_category_names_and_slugs_match_for_home_and_cars_filters()
    {
        await using var db = CreateApiDb();
        var names = await db.Categories.AsNoTracking()
            .Select(category => category.Name)
            .ToListAsync();
        Assert.NotEmpty(names);

        foreach (var name in names)
        {
            Assert.True(CarCategories.Matches(name, name), name);
            Assert.True(CarCategories.Matches(name, CarCategories.Slug(name)), name);
            Assert.Equal("0 Cars", CarApiMapper.CountLabel(Array.Empty<Car>(), name));
        }

        // Legacy alias used by older home links must still resolve to SQL "Sedan".
        Assert.Contains(names, name => name.Equals("Sedan", StringComparison.OrdinalIgnoreCase));
        Assert.True(CarCategories.Matches("Sedan", "Sedans"));
    }

    [Fact]
    public async Task Partner_strip_published_items_match_sql_without_seed_injection()
    {
        await using var db = CreateMvcDb();
        var env = new TestEnvironment();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var partners = new PartnerStripService(db, cache, env);

        var sqlPublished = await db.PartnerStripItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();

        cache.Remove(PartnerStripService.ItemsCacheKey);
        var publicItems = partners.GetPublishedItems();

        Assert.Equal(sqlPublished.Count, publicItems.Count);
        Assert.Equal(sqlPublished.Select(item => item.Id).ToList(), publicItems.Select(item => item.Id).ToList());
        Assert.Equal(sqlPublished.Select(item => item.Name.Trim()).ToList(), publicItems.Select(item => item.Name).ToList());

        // Additive create/delete only — does not mutate existing partner rows.
        var created = await partners.CreateItemAsync(new PartnerStripItemEditViewModel
        {
            Name = $"SqlCheck-{Guid.NewGuid():N}"[..24],
            LogoPath = "~/assets/images/brand/01-w.svg",
            SortOrder = 9900,
            IsPublished = true
        }, logo: null);
        Assert.True(created.Ok, created.Error);
        try
        {
            cache.Remove(PartnerStripService.ItemsCacheKey);
            Assert.Contains(partners.GetPublishedItems(), item => item.Id == created.Id);
            Assert.True(await db.PartnerStripItems.AsNoTracking()
                .AnyAsync(item => item.Id == created.Id && item.IsPublished));
        }
        finally
        {
            Assert.True(await partners.DeleteItemAsync(created.Id!.Value));
            cache.Remove(PartnerStripService.ItemsCacheKey);
            Assert.DoesNotContain(partners.GetPublishedItems(), item => item.Id == created.Id);
        }
    }

    [Fact]
    public void Empty_published_partner_strip_does_not_revive_hardcoded_seed_logos()
    {
        // Isolated InMemory — does not touch FinalProjectDb partner rows.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("partner-empty-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new ApplicationDbContext(options);
        db.PartnerStripSettings.Add(new PartnerStripSettings
        {
            Id = PartnerStripSettings.SingletonId,
            Eyebrow = "Partners",
            HeadingBefore = "Browse by",
            HeadingHighlight = "Company",
            UpdatedAt = DateTime.UtcNow
        });
        db.PartnerStripItems.Add(new PartnerStripItem
        {
            Name = "HiddenCo",
            LogoPath = "~/assets/images/brand/01-w.svg",
            SortOrder = 10,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var partners = new PartnerStripService(db, new MemoryCache(new MemoryCacheOptions()), new TestEnvironment());
        var published = partners.GetPublishedItems();
        Assert.Empty(published);
        Assert.DoesNotContain(published, item => item.Name.Equals("Jeep", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IsFeatured_persists_in_sql_and_drives_api_plus_premium_order()
    {
        await using var db = CreateApiDb();
        var ids = await db.Cars.AsNoTracking()
            .Where(car => car.Status == "Active")
            .OrderBy(car => car.Id)
            .Select(car => car.Id)
            .Take(2)
            .ToListAsync();
        Assert.True(ids.Count >= 2);

        var featuredId = ids[0];
        var otherId = ids[1];
        var priorFeatured = await db.Cars.AsNoTracking()
            .Where(car => car.Id == featuredId)
            .Select(car => car.IsFeatured)
            .SingleAsync();
        var priorOther = await db.Cars.AsNoTracking()
            .Where(car => car.Id == otherId)
            .Select(car => car.IsFeatured)
            .SingleAsync();

        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Cars SET IsFeatured = CAST(1 AS bit) WHERE Id = {featuredId};
                UPDATE Cars SET IsFeatured = CAST(0 AS bit) WHERE Id = {otherId};
                """);

            var service = new CarService(db, new SilentNotifications());
            var featured = await service.GetByIdAsync(featuredId);
            var other = await service.GetByIdAsync(otherId);
            Assert.NotNull(featured);
            Assert.NotNull(other);
            Assert.True(featured!.IsFeatured);
            Assert.False(other!.IsFeatured);

            var mapped = new[]
            {
                CarApiMapper.ToCar(ToApiDto(featured)),
                CarApiMapper.ToCar(ToApiDto(other))
            };
            var premium = HomeCarShowcase.PremiumCars(mapped);
            Assert.Equal(new[] { featuredId }, premium.Select(car => car.Id));
            Assert.True(premium[0].IsFeatured);
            Assert.DoesNotContain(premium, car => car.Id == otherId);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Cars SET IsFeatured = {priorFeatured} WHERE Id = {featuredId};
                UPDATE Cars SET IsFeatured = {priorOther} WHERE Id = {otherId};
                """);
        }
    }

    [Fact]
    public async Task Public_answered_inquiries_are_scoped_to_product_and_strip_contact_pii()
    {
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive);
        var email = $"pii-{Guid.NewGuid():N}@example.com";
        var (ok, error) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = product.Id,
            CustomerName = "Public Q",
            Email = email,
            Phone = "+994501112233",
            Message = "Is this part still available for pickup this week?"
        });
        Assert.True(ok, error);

        var inquiry = await db.ProductInquiries.AsNoTracking()
            .SingleAsync(item => item.Email == email);
        try
        {
            Assert.True((await catalog.SaveInquiryReplyAsync(
                inquiry.Id,
                "Yes, we have it at the Baku counter.",
                "admin-sql-check")).Ok);

            var publicRows = await catalog.GetAnsweredInquiriesForProductAsync(product.Id);
            var row = Assert.Single(publicRows, item => item.Id == inquiry.Id);
            Assert.Equal(product.Id, row.ProductId);
            Assert.True(row.HasReply);
            Assert.Equal(string.Empty, row.Email);
            Assert.Equal(string.Empty, row.Phone);

            var otherProduct = await db.ShopProducts.AsNoTracking()
                .FirstAsync(item => item.IsActive && item.Id != product.Id);
            var otherRows = await catalog.GetAnsweredInquiriesForProductAsync(otherProduct.Id);
            Assert.DoesNotContain(otherRows, item => item.Id == inquiry.Id);
        }
        finally
        {
            await catalog.DeleteInquiryAsync(inquiry.Id);
        }
    }

    [Fact]
    public async Task Review_and_rating_reject_ids_absent_from_shopproducts_sql()
    {
        await using var db = CreateApiDb();
        var missingId = int.MaxValue - 91;
        Assert.False(await db.ShopProducts.AsNoTracking().AnyAsync(item => item.Id == missingId));

        var ratings = new ProductRatingService(db);
        var reviews = new ProductReviewService(db);
        var userId = await db.IdentityUsers.AsNoTracking().Select(user => user.Id).FirstAsync();

        Assert.Null(await ratings.GetProductRatingsAsync(missingId));
        Assert.Null(await ratings.SaveRatingAsync(missingId, userId, 4));
        Assert.Null(await reviews.GetReviewsAsync(missingId, userId, includeHidden: false));
        var (created, _) = await reviews.SaveReviewAsync(missingId, null, userId, "Missing product must not persist.");
        Assert.Null(created);
    }

    [Fact]
    public async Task Sql_has_sale_idempotency_index_and_product_favorite_fk()
    {
        await using var db = CreateApiDb();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var command = conn.CreateCommand();
        command.CommandText = """
            SELECT
              CASE WHEN COL_LENGTH('InPersonSales', 'IdempotencyKey') IS NULL THEN 0 ELSE 1 END,
              CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_InPersonSales_IdempotencyKey') THEN 1 ELSE 0 END,
              CASE WHEN EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProductFavorites_ShopProducts_ProductId') THEN 1 ELSE 0 END;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }

    [Fact]
    public void HomeController_source_does_not_keep_stale_catalog_cache()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "HomeController.cs"));
        Assert.DoesNotContain("CatalogCache", source, StringComparison.Ordinal);
        Assert.Contains("CarApiUnavailable", source, StringComparison.Ordinal);
        Assert.Contains("GetLookupsAsync", source, StringComparison.Ordinal);
        Assert.Contains("HomeCategories", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CarsController_source_resolves_categories_from_lookups()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "Index.cshtml"));
        Assert.Contains("lookups.Categories", controller, StringComparison.Ordinal);
        Assert.Contains("CarCategories.Matches", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("CarCategories.All", controller, StringComparison.Ordinal);
        Assert.Contains("Model.Categories", view, StringComparison.Ordinal);
        Assert.DoesNotContain("CarCategories.All", view, StringComparison.Ordinal);
    }

    private static CarApiDto ToApiDto(CarResponse car) => new()
    {
        Id = car.Id,
        Model = car.Model,
        Year = car.Year,
        Price = car.Price,
        Mileage = car.Mileage,
        FuelType = car.FuelType,
        Transmission = car.Transmission,
        Color = car.Color,
        Description = car.Description,
        IsNew = car.IsNew,
        IsFeatured = car.IsFeatured,
        Status = car.Status,
        Brand = new BrandApiDto { Id = car.Brand.Id, Name = car.Brand.Name },
        Category = new CategoryApiDto { Id = car.Category.Id, Name = car.Category.Name },
        Images = car.Images.Select(image => new CarImageApiDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            IsMain = image.IsMain,
            DisplayOrder = image.DisplayOrder
        }).ToList()
    };

    private static FinalProjectDbContext CreateApiDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static ApplicationDbContext CreateMvcDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "FinalProject.Tests", "wwwroot");
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class SilentNotifications : INotificationService
    {
        public Task<NotificationListResponse> GetForUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationUnreadCountsResponse> GetUnreadCountsAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationResponse?> CreateMessageNotificationAsync(
            string receiverId,
            string senderDisplayName,
            int conversationId,
            int messageId,
            string preview,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NotificationResponse>> CreatePriceDropNotificationsAsync(
            int carId,
            string carName,
            decimal previousPrice,
            decimal newPrice,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationResponse>>(Array.Empty<NotificationResponse>());

        public Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
