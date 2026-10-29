using System.ComponentModel.DataAnnotations;
using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using MvcCommerce = FinalProject.MVC.Commerce;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class CarListingSelectionTests
{
    [Fact]
    public void Catalog_keeps_models_with_their_own_make()
    {
        Assert.True(VerifiedCatalogSeeder.KnownModels.Count > 80);
        Assert.Contains(VerifiedCatalogSeeder.KnownModels, item => item.Brand == "BMW" && item.Model == "M340i");
        Assert.Contains(VerifiedCatalogSeeder.KnownModels, item => item.Brand == "Audi" && item.Model == "A3 Sportback");
        Assert.True(VerifiedCatalogSeeder.BelongsToAnotherBrand("M340i", "Audi"));
        Assert.False(VerifiedCatalogSeeder.BelongsToAnotherBrand("M340i", "BMW"));
        Assert.False(VerifiedCatalogSeeder.BelongsToAnotherBrand("Studio One", "Audi"));
    }

    [Fact]
    public void Model_year_accepts_1900_through_the_newest_supported_year()
    {
        Assert.Empty(CarListingRules.ValidateYear(1900));
        Assert.Empty(CarListingRules.ValidateYear(2026));
        Assert.Empty(CarListingRules.ValidateYear(CarListingRules.MaxModelYear));
        Assert.NotEmpty(CarListingRules.ValidateYear(1899));
        Assert.NotEmpty(CarListingRules.ValidateYear(CarListingRules.MaxModelYear + 1));

        var request = Request(2026, "A3 Sportback");
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), results, true));

        request.Year = CarListingRules.MaxModelYear + 1;
        results.Clear();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        Assert.Contains(results, result => result.MemberNames.Contains("Year"));
    }

    [Fact]
    public async Task Saving_another_makes_known_model_is_rejected_and_a_custom_model_is_kept()
    {
        await using var db = CreateDb();
        var bmw = db.Brands.Add(new Brand { Name = "BMW", CreatedAt = DateTime.UtcNow }).Entity;
        var audi = db.Brands.Add(new Brand { Name = "Audi", CreatedAt = DateTime.UtcNow }).Entity;
        var category = db.Categories.Add(new Category { Name = "Sedan" }).Entity;
        await db.SaveChangesAsync();
        var service = new CarService(db, new SilentNotifications());

        var wrong = await Assert.ThrowsAsync<CarListingException>(() =>
            service.CreateCatalogAsync(Request(2024, "M340i", audi.Id, category.Id)));
        Assert.Contains("another make", wrong.Message, StringComparison.OrdinalIgnoreCase);

        var custom = await service.CreateCatalogAsync(Request(2024, "Studio One", audi.Id, category.Id));
        Assert.NotNull(custom);
        Assert.Equal("Studio One", custom!.Model);

        var owned = await service.CreateCatalogAsync(Request(2020, "OneOff Coupe", bmw.Id, category.Id));
        Assert.NotNull(owned);
        var reused = await Assert.ThrowsAsync<CarListingException>(() =>
            service.CreateCatalogAsync(Request(2021, "OneOff Coupe", audi.Id, category.Id)));
        Assert.Contains("another make", reused.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lookups_offer_only_the_selected_makes_models()
    {
        await using var db = CreateDb();
        var bmw = db.Brands.Add(new Brand { Name = "BMW", CreatedAt = DateTime.UtcNow }).Entity;
        var audi = db.Brands.Add(new Brand { Name = "Audi", CreatedAt = DateTime.UtcNow }).Entity;
        db.Categories.Add(new Category { Name = "Sedan" });
        db.Cars.Add(new Car
        {
            Brand = bmw,
            Category = db.Categories.Local.Single(),
            Model = "OneOff Coupe",
            Year = 2020,
            Price = 1000,
            Mileage = 1,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Blue",
            Description = "Custom",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var lookups = await new CarService(db, new SilentNotifications()).GetLookupsAsync();
        var bmwModels = lookups.Models!.Where(item => item.BrandId == bmw.Id).Select(item => item.Name).ToList();
        var audiModels = lookups.Models!.Where(item => item.BrandId == audi.Id).Select(item => item.Name).ToList();

        Assert.Contains("M340i", bmwModels);
        Assert.Contains("OneOff Coupe", bmwModels);
        Assert.DoesNotContain("A3 Sportback", bmwModels);
        Assert.Contains("A3 Sportback", audiModels);
        Assert.DoesNotContain("M340i", audiModels);
    }

    [Fact]
    public void Vehicle_types_use_existing_categories_without_new_ids()
    {
        Assert.Equal(MvcCommerce.VehicleTypeCatalog.Passenger, MvcCommerce.VehicleTypeCatalog.For("Sedan"));
        Assert.Equal(MvcCommerce.VehicleTypeCatalog.Passenger, MvcCommerce.VehicleTypeCatalog.For("Minivans"));
        Assert.Equal(MvcCommerce.VehicleTypeCatalog.Commercial, MvcCommerce.VehicleTypeCatalog.For("Pickup Truck"));
        Assert.Equal(MvcCommerce.VehicleTypeCatalog.Motorcycle, MvcCommerce.VehicleTypeCatalog.For("Motorcycle"));
    }

    [Fact]
    public void Public_pages_drop_social_icons_and_the_listing_form_guides_the_selection()
    {
        var root = FindRepositoryRoot();
        var header = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderThree.cshtml"));
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"));
        var form = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_VehicleListingFields.cshtml"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));

        Assert.DoesNotContain("x.svg", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fa-facebook", layout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fa-whatsapp", layout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"vehicle-type\"", form, StringComparison.Ordinal);
        Assert.Contains("VehicleTypeCatalog.Types", form, StringComparison.Ordinal);
        Assert.Contains("data-combo=\"model\"", form, StringComparison.Ordinal);
        Assert.Contains("vehiclePhotos", form, StringComparison.Ordinal);
        Assert.Contains("id=\"Car_Year\"", form, StringComparison.Ordinal);
        Assert.Contains("class=\"input-form av-native-select\"", form, StringComparison.Ordinal);
        Assert.Contains("modelYear >= 1900", form, StringComparison.Ordinal);
        Assert.Contains("id=\"Car_CategoryId\"", form, StringComparison.Ordinal);
        Assert.Contains("av-native-select", form, StringComparison.Ordinal);
        Assert.DoesNotContain("data-combo=\"year\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("data-combo=\"category\"", form, StringComparison.Ordinal);
        Assert.Contains("vehicle-photos-preview", admin, StringComparison.Ordinal);
        Assert.Contains("av-vehicle-photos-grid", admin, StringComparison.Ordinal);
        Assert.Contains("av-vehicle-photos-grid", form, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(auto-fill, minmax(140px, 160px))", admin, StringComparison.Ordinal);
        Assert.Contains("object-fit: cover", admin, StringComparison.Ordinal);
        Assert.Contains("width: 28px", admin, StringComparison.Ordinal);
        Assert.Contains("Remove photo", admin, StringComparison.Ordinal);
    }

    private static SaveCarRequest Request(int year, string model, int brandId = 1, int categoryId = 1) => new()
    {
        BrandId = brandId,
        CategoryId = categoryId,
        Model = model,
        Year = year,
        Price = 12000,
        Mileage = 10,
        FuelType = "Petrol",
        Transmission = "Automatic",
        Color = "Blue",
        Description = "Listing",
        ImageUrl = "/assets/images/catalog/bmw/m340i.jpg",
        Status = "Active"
    };

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-selection-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "FinalProject.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
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
