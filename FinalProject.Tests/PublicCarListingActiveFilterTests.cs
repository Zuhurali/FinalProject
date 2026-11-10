using FinalProject.Repositories.Implementations;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.MVC.DTOs;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApiCarPublicListing = FinalProject.Commerce.CarPublicListing;
using MvcCarPublicListing = FinalProject.MVC.Commerce.CarPublicListing;

namespace FinalProject.Tests;

public class PublicCarListingActiveFilterTests
{
    [Fact]
    public void Public_mvc_query_helpers_force_Active_status()
    {
        var created = MvcCarPublicListing.CreateActiveQuery(page: 2, pageSize: 6);
        Assert.Equal(MvcCarPublicListing.ActiveStatus, created.Status);
        Assert.Equal("latest", created.Sort);
        Assert.Equal(2, created.Page);
        Assert.Equal(6, created.PageSize);

        var withBrand = MvcCarPublicListing.WithActiveStatus(new CarApiQueryDto { BrandId = 3, Status = "Draft" });
        Assert.Equal(MvcCarPublicListing.ActiveStatus, withBrand.Status);
        Assert.Equal(3, withBrand.BrandId);
    }

    [Fact]
    public void CarsController_public_paths_request_Active_status()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var mapper = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "MVC", "Mappings", "CarApiMapper.cs"));
        var brand = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "BrandController.cs"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "AdminCarsController.cs"));

        Assert.Contains("CarPublicListing.WithActiveStatus", controller, StringComparison.Ordinal);
        Assert.Contains("CarPublicListing.CreateActiveQuery()", controller, StringComparison.Ordinal);
        Assert.Contains("CarPublicListing.CreateSoldQuery()", controller, StringComparison.Ordinal);
        Assert.Contains("CarPublicListing.CreateActiveQuery()", mapper, StringComparison.Ordinal);
        Assert.Contains("CarPublicListing.WithActiveStatus", brand, StringComparison.Ordinal);

        // Admin listing must keep its own optional Status filter (Draft/Sold visible when chosen).
        Assert.Contains("Status = model.Status", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CarPublicListing", admin, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryAsync_with_Active_status_excludes_Draft_and_Sold()
    {
        await using var db = CreateDb();
        var active = await SeedCarAsync(db, "ActiveOne", "Active");
        await SeedCarAsync(db, "DraftOne", "Draft");
        await SeedCarAsync(db, "SoldOne", "Sold");
        var service = new CarService(new FinalProjectRepository(db), new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Status = ApiCarPublicListing.ActiveStatus,
            Page = 1,
            PageSize = 50
        });

        Assert.Single(page.Items);
        Assert.Equal(active.Id, page.Items[0].Id);
        Assert.All(page.Items, item => Assert.Equal("Active", item.Status, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryAsync_with_Sold_status_returns_only_sold_cars()
    {
        await using var db = CreateDb();
        await SeedCarAsync(db, "ActiveOne", "Active");
        await SeedCarAsync(db, "DraftOne", "Draft");
        var sold = await SeedCarAsync(db, "SoldOne", "Sold");
        var service = new CarService(new FinalProjectRepository(db), new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto
        {
            Status = ApiCarPublicListing.SoldStatus,
            Page = 1,
            PageSize = 50
        });

        Assert.Single(page.Items);
        Assert.Equal(sold.Id, page.Items[0].Id);
        Assert.Equal("Sold", page.Items[0].Status, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryAsync_without_status_still_returns_all_for_admin_style_queries()
    {
        await using var db = CreateDb();
        await SeedCarAsync(db, "ActiveOne", "Active");
        await SeedCarAsync(db, "DraftOne", "Draft");
        var service = new CarService(new FinalProjectRepository(db), new SilentNotifications());

        var page = await service.QueryAsync(new CarListQueryDto { Page = 1, PageSize = 50 });

        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, item => item.Status == "Draft");
    }

    [Fact]
    public void Api_CarsController_normalizes_anonymous_list_status_to_Active_or_Sold()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject", "Controllers", "CarsController.cs"));
        Assert.Contains("NormalizeAnonymousListStatus", controller, StringComparison.Ordinal);
        Assert.Contains("!User.IsInRole(\"Admin\")", controller, StringComparison.Ordinal);
        Assert.Contains("!User.IsInRole(\"SuperAdmin\")", controller, StringComparison.Ordinal);
        Assert.Equal("Sold", ApiCarPublicListing.NormalizeAnonymousListStatus("Sold"));
        Assert.Equal("Active", ApiCarPublicListing.NormalizeAnonymousListStatus("Draft"));
        Assert.Equal("Active", ApiCarPublicListing.NormalizeAnonymousListStatus(null));
        Assert.True(ApiCarPublicListing.IsPubliclyListed("Sold"));
        Assert.False(ApiCarPublicListing.IsPubliclyListed("Draft"));
    }

    [Fact]
    public async Task Explore_latest_and_most_viewed_exclude_non_active_cars()
    {
        await using var db = CreateDb();
        var active = await SeedCarAsync(db, "ActiveExplore", "Active");
        var draft = await SeedCarAsync(db, "DraftExplore", "Draft");
        var discovery = new CarDiscoveryService(new FinalProjectRepository(db));
        var when = DateTime.UtcNow;

        Assert.True(await discovery.RecordViewAsync(active.Id, when));
        Assert.False(await discovery.RecordViewAsync(draft.Id, when));

        var latest = await discovery.GetLatestArrivalsAsync(page: 1, pageSize: 10);
        Assert.Contains(latest.Items, item => item.Id == active.Id);
        Assert.DoesNotContain(latest.Items, item => item.Id == draft.Id);

        var viewed = await discovery.GetMostViewedAsync("all", page: 1, pageSize: 10, utcNow: when);
        Assert.Contains(viewed.Items, item => item.Car.Id == active.Id);
        Assert.DoesNotContain(viewed.Items, item => item.Car.Id == draft.Id);
    }

    [Fact]
    public async Task Suggestions_omit_non_active_car_matches()
    {
        await using var db = CreateDb();
        await SeedCarAsync(db, "CamryActive", "Active", description: "Toyota Camry Active Listing");
        await SeedCarAsync(db, "CamryDraft", "Draft", description: "Toyota Camry Draft Listing");
        var service = new CarService(new FinalProjectRepository(db), new SilentNotifications());

        var suggestions = await service.GetSuggestionsAsync("Camry", limit: 10);

        Assert.Contains(suggestions, item => item.Text.Contains("Active Listing", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(suggestions, item => item.Text.Contains("Draft Listing", StringComparison.OrdinalIgnoreCase));
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("public-active-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static async Task<Car> SeedCarAsync(
        FinalProjectDbContext db,
        string model,
        string status,
        string? description = null)
    {
        var brand = await db.Brands.FirstOrDefaultAsync()
            ?? db.Brands.Add(new Brand { Name = "Vault", CreatedAt = DateTime.UtcNow }).Entity;
        var category = await db.Categories.FirstOrDefaultAsync()
            ?? db.Categories.Add(new Category { Name = "Sedan" }).Entity;
        await db.SaveChangesAsync();

        var car = new Car
        {
            BrandId = brand.Id,
            CategoryId = category.Id,
            Model = model,
            Year = 2024,
            Price = 12000,
            Mileage = 1000,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = description ?? model,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
    }

    private sealed class SilentNotifications : INotificationService
    {
        public Task<NotificationListResponseDto> GetForUserAsync(string userId, int take = 50, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationUnreadCountsResponseDto> GetUnreadCountsAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NotificationResponseDto?> CreateMessageNotificationAsync(
            string receiverId,
            string senderDisplayName,
            int conversationId,
            int messageId,
            string preview,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NotificationResponseDto>> CreatePriceDropNotificationsAsync(
            int carId,
            string carName,
            decimal previousPrice,
            decimal newPrice,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationResponseDto>>(Array.Empty<NotificationResponseDto>());

        public Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
