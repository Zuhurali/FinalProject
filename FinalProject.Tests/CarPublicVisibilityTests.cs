using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FinalProject.Commerce;
using FinalProject.Controllers;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class CarPublicVisibilityTests
{
    [Theory]
    [InlineData("Active", true)]
    [InlineData("active", true)]
    [InlineData("Sold", true)]
    [InlineData("sold", true)]
    [InlineData("Draft", false)]
    [InlineData(null, false)]
    public void Public_listing_helper_recognizes_Active_and_Sold_archive(string? status, bool expected)
    {
        Assert.Equal(expected, CarPublicListing.IsPubliclyListed(status));
    }

    [Fact]
    public async Task Anonymous_get_by_id_hides_draft_but_allows_sold_archive()
    {
        await using var db = CreateDb();
        var draft = await SeedCarAsync(db, "DraftHide", "Draft");
        var sold = await SeedCarAsync(db, "SoldShow", "Sold");
        var active = await SeedCarAsync(db, "ActiveShow", "Active");
        var controller = CreateCarsController(db, principal: null);

        Assert.IsType<NotFoundResult>((await controller.GetById(draft.Id, CancellationToken.None)).Result);

        var soldOk = Assert.IsType<OkObjectResult>((await controller.GetById(sold.Id, CancellationToken.None)).Result);
        Assert.Equal(sold.Id, Assert.IsType<CarResponse>(soldOk.Value).Id);

        var ok = Assert.IsType<OkObjectResult>((await controller.GetById(active.Id, CancellationToken.None)).Result);
        var payload = Assert.IsType<CarResponse>(ok.Value);
        Assert.Equal(active.Id, payload.Id);
    }

    [Fact]
    public async Task Admin_get_by_id_can_load_draft_for_management()
    {
        await using var db = CreateDb();
        var draft = await SeedCarAsync(db, "DraftAdmin", "Draft");
        var admin = Principal("admin-1", "Admin");
        var controller = CreateCarsController(db, admin);

        var ok = Assert.IsType<OkObjectResult>((await controller.GetById(draft.Id, CancellationToken.None)).Result);
        var payload = Assert.IsType<CarResponse>(ok.Value);
        Assert.Equal(draft.Id, payload.Id);
        Assert.Equal("Draft", payload.Status);
    }

    [Fact]
    public async Task Owner_get_by_id_can_load_own_sold_listing()
    {
        await using var db = CreateDb();
        db.IdentityUsers.Add(new IdentityUserReference { Id = "owner-1" });
        await db.SaveChangesAsync();
        var sold = await SeedCarAsync(db, "SoldOwned", "Sold", ownerId: "owner-1");
        var controller = CreateCarsController(db, Principal("owner-1", "Member"));

        var ok = Assert.IsType<OkObjectResult>((await controller.GetById(sold.Id, CancellationToken.None)).Result);
        Assert.Equal(sold.Id, Assert.IsType<CarResponse>(ok.Value).Id);
    }

    [Fact]
    public async Task Record_view_and_favorite_require_Active_car()
    {
        await using var db = CreateDb();
        db.IdentityUsers.Add(new IdentityUserReference { Id = "member-1" });
        await db.SaveChangesAsync();
        var draft = await SeedCarAsync(db, "DraftFav", "Draft");
        var active = await SeedCarAsync(db, "ActiveFav", "Active");
        var discovery = new CarDiscoveryService(db);
        var market = new MarketplaceService(db, new ConfigurationBuilder().Build(), new SilentNotifications());

        Assert.False(await discovery.RecordViewAsync(draft.Id));
        Assert.True(await discovery.RecordViewAsync(active.Id));
        Assert.Null(await market.AddFavoriteAsync("member-1", draft.Id));
        Assert.NotNull(await market.AddFavoriteAsync("member-1", active.Id));

        var favorites = await market.GetFavoritesAsync("member-1");
        Assert.Single(favorites);
        Assert.Equal(active.Id, favorites[0].Car.Id);
    }

    [Fact]
    public void Unsafe_primary_image_url_fails_validation()
    {
        var request = new SaveCarRequest
        {
            BrandId = 1,
            CategoryId = 1,
            Model = "X",
            Year = 2024,
            Price = 1000,
            Mileage = 1,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = "Test",
            ImageUrl = "..\\evil.png",
            Status = "Active"
        };

        var results = request.Validate(new ValidationContext(request)).ToList();
        Assert.Contains(results, item => item.MemberNames.Contains(nameof(SaveCarRequest.ImageUrl)));
    }

    [Fact]
    public void Account_edit_listing_manages_gallery_and_deletes_removed_uploads()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var account = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "AccountController.cs"));
        var fields = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_VehicleListingFields.cshtml"));
        var cars = File.ReadAllText(Path.Combine(root, "FinalProject", "Controllers", "CarsController.cs"));

        Assert.Contains("GallerySpecified = true", account, StringComparison.Ordinal);
        Assert.Contains("DeleteUploads(update.RemovedImageUrls)", account, StringComparison.Ordinal);
        Assert.Contains("Car.RemoveImageIds", fields, StringComparison.Ordinal);
        Assert.Contains("Car.PrimaryImageId", fields, StringComparison.Ordinal);
        Assert.Contains("IsPubliclyListed(car.Status)", cars, StringComparison.Ordinal);
        Assert.Contains("CanManage(car.OwnerId)", cars, StringComparison.Ordinal);
    }

    private static CarsController CreateCarsController(FinalProjectDbContext db, ClaimsPrincipal? principal)
    {
        var cars = new CarService(db, new SilentNotifications());
        var discovery = new CarDiscoveryService(db);
        var controller = new CarsController(cars, discovery)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
        return controller;
    }

    private static ClaimsPrincipal Principal(string userId, string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userId),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test"));
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-visibility-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static async Task<Car> SeedCarAsync(
        FinalProjectDbContext db,
        string model,
        string status,
        string? ownerId = null)
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
            Price = 15000,
            Mileage = 1000,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = model,
            Status = status,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            Images =
            {
                new CarImage
                {
                    ImageUrl = "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg",
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
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
