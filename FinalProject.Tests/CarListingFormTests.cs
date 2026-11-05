using System.ComponentModel.DataAnnotations;
using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Mappings;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApiCatalog = FinalProject.Commerce.CarFeatureCatalog;
using ApiRules = FinalProject.Commerce.CarListingRules;
using MvcCatalog = FinalProject.MVC.Commerce.CarFeatureCatalog;
using MvcRules = FinalProject.MVC.Commerce.CarListingRules;

namespace FinalProject.Tests;

public class CarListingFormTests
{
    [Fact]
    public void Feature_and_spec_catalogs_match_between_api_and_mvc()
    {
        Assert.Equal(
            ApiCatalog.All.Select(feature => feature.Key + "|" + feature.Label),
            MvcCatalog.All.Select(feature => feature.Key + "|" + feature.Label));
        Assert.Equal(ApiRules.DriveTypes, MvcRules.DriveTypes);
        Assert.Equal(ApiRules.Fuels, MvcRules.Fuels);
        Assert.Equal(ApiRules.Transmissions, MvcRules.Transmissions);
        Assert.Equal(ApiRules.SeatChoices, MvcRules.SeatChoices);
        Assert.Equal(ApiRules.EngineSizes, MvcRules.EngineSizes);
        Assert.Equal(ApiRules.HorsepowerChoices, MvcRules.HorsepowerChoices);
        Assert.Equal(ApiRules.MarketOrigins, MvcRules.MarketOrigins);
        Assert.Equal(
            ApiRules.ExteriorColors.Select(item => item.Name + item.Hex),
            MvcRules.ExteriorColors.Select(item => item.Name + item.Hex));
        Assert.Equal(FinalProject.Commerce.VehicleBodyPartCatalog.Parts, FinalProject.MVC.Commerce.VehicleBodyPartCatalog.Parts);
    }

    [Fact]
    public void Body_part_catalog_stores_selections_and_keeps_legacy_text()
    {
        Assert.Equal(
            "Hood|Front Bumper|Other:mirror",
            FinalProject.Commerce.VehicleBodyPartCatalog.Store(["Hood", "Front Bumper", "Other"], "mirror"));
        Assert.Null(FinalProject.Commerce.VehicleBodyPartCatalog.Store(Array.Empty<string>(), null));
        Assert.Equal("Hood, Front Bumper, mirror", FinalProject.Commerce.VehicleBodyPartCatalog.Display("Hood|Front Bumper|Other:mirror"));

        var legacy = FinalProject.Commerce.VehicleBodyPartCatalog.Read("left wing repaired");
        Assert.Empty(legacy.Parts);
        Assert.True(legacy.OtherSelected);
        Assert.Equal("left wing repaired", legacy.OtherText);

        var restored = FinalProject.Commerce.VehicleBodyPartCatalog.Read("Hood|Roof|Other:spoiler");
        Assert.Equal(["Hood", "Roof"], restored.Parts);
        Assert.True(restored.OtherSelected);
        Assert.Equal("spoiler", restored.OtherText);
    }

    [Fact]
    public void Color_and_engine_catalogs_store_custom_values_without_inventing_defaults()
    {
        Assert.Equal("Black|#000000", FinalProject.Commerce.VehicleColorCatalog.Store("Black", "#000000"));
        Assert.Equal("White", FinalProject.Commerce.VehicleColorCatalog.DisplayName("White"));
        Assert.Null(FinalProject.Commerce.VehicleColorCatalog.Hex("White"));
        Assert.Equal("Pearl|#F5F5F5", FinalProject.Commerce.VehicleColorCatalog.FromPosted("Pearl|#f5f5f5"));
        Assert.Contains(ValidateColor(""), result => result.ErrorMessage!.Contains("exterior color", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(FinalProject.Commerce.VehicleColorCatalog.Validate("Bad|#ZZ"), result => result.ErrorMessage!.Contains("HEX", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("2.0L", FinalProject.Commerce.VehicleEngineCatalog.Normalize("2.0L"));
        Assert.Equal("2.3L", FinalProject.Commerce.VehicleEngineCatalog.Normalize("2.3"));
        Assert.Equal("Dual-motor", FinalProject.Commerce.VehicleEngineCatalog.Normalize("Dual-motor"));
        Assert.Null(FinalProject.Commerce.VehicleEngineCatalog.Normalize(""));
    }

    private static IEnumerable<ValidationResult> ValidateColor(string color) =>
        FinalProject.Commerce.VehicleColorCatalog.Validate(color);

    [Fact]
    public void New_listing_hides_zero_defaults_and_edit_keeps_a_real_zero_mileage()
    {
        Assert.Equal(string.Empty, ListingFieldValues.Number(null, posted: false, attempted: null, "0", blankWhenZero: true));
        Assert.Equal(string.Empty, ListingFieldValues.Number(null, posted: false, attempted: null, "0.00", blankWhenZero: true));
        Assert.Equal("0", ListingFieldValues.Number(12, posted: false, attempted: null, "0", blankWhenZero: true));
        Assert.Equal("2024", ListingFieldValues.Number(null, posted: true, attempted: "2024", "0", blankWhenZero: true));
    }

    [Fact]
    public void Validation_rejects_zero_price_unknown_drive_and_unknown_features()
    {
        var zeros = ValidRequest();
        zeros.Year = 0;
        zeros.Price = 0;
        var zeroResults = Validate(zeros);
        Assert.Contains(zeroResults, result => result.ErrorMessage!.Contains("model year", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zeroResults, result => result.ErrorMessage!.Contains("price", StringComparison.OrdinalIgnoreCase));

        var specs = ValidRequest();
        specs.DriveType = "sideways";
        specs.Features = ["Spoiler"];
        var specResults = Validate(specs);
        Assert.Contains(specResults, result => result.MemberNames.Contains("DriveType"));
        Assert.Contains(specResults, result => result.MemberNames.Contains("Features"));
        Assert.Empty(Validate(ValidRequest()));
    }

    [Fact]
    public async Task Create_persists_specs_features_and_image_and_edit_keeps_them()
    {
        await using var db = CreateDb();
        var brand = db.Brands.Add(new Brand { Name = "Vault", CreatedAt = DateTime.UtcNow }).Entity;
        var category = db.Categories.Add(new Category { Name = "Coupe" }).Entity;
        db.IdentityUsers.Add(new IdentityUserReference { Id = "owner-1", Email = "owner@autovault.test" });
        await db.SaveChangesAsync();

        var service = new CarService(db, new SilentNotifications());
        var created = await service.CreateAsync("owner-1", ValidRequest(brand.Id, category.Id));

        Assert.NotNull(created);
        Assert.Equal(5, created!.Seats);
        Assert.Equal("AWD", created.DriveType);
        Assert.Equal("2.0L Turbo", created.Engine);
        Assert.Equal(248, created.Horsepower);
        Assert.Equal("km", created.MileageUnit);
        Assert.False(created.IsNew);
        Assert.Equal(["ClimateControl", "Touchscreen", "FogLights"], created.Features);
        Assert.Equal("~/assets/images/portfolio/cars/uploads/existing.png", created.Images[0].ImageUrl);

        var stored = await db.Cars.Include(car => car.Images).SingleAsync();
        Assert.Equal("ClimateControl|Touchscreen|FogLights", stored.Features);

        var edit = ValidRequest(brand.Id, category.Id);
        edit.Description = "Garage-kept coupe with a complete service history.";
        edit.FeaturesSpecified = false;
        edit.Features = null;
        edit.Price = 31000;

        var updated = await service.UpdateAsync(created.Id, edit);
        Assert.NotNull(updated);
        Assert.Equal(31000, updated!.Car.Price);
        Assert.Equal(5, updated.Car.Seats);
        Assert.Equal("AWD", updated.Car.DriveType);
        Assert.Equal("2.0L Turbo", updated.Car.Engine);
        Assert.Equal(["ClimateControl", "Touchscreen", "FogLights"], updated.Car.Features);
        Assert.Equal(248, updated.Car.Horsepower);
        Assert.Equal("km", updated.Car.MileageUnit);
        Assert.Equal("~/assets/images/portfolio/cars/uploads/existing.png", updated.Car.Images[0].ImageUrl);

        var mapped = CarApiMapper.ToCar(new FinalProject.MVC.DTOs.CarApiDto
        {
            Id = updated.Car.Id,
            Model = updated.Car.Model,
            Year = updated.Car.Year,
            Price = updated.Car.Price,
            Mileage = updated.Car.Mileage,
            FuelType = updated.Car.FuelType,
            Transmission = updated.Car.Transmission,
            Color = updated.Car.Color,
            Description = updated.Car.Description,
            Seats = updated.Car.Seats,
            DriveType = updated.Car.DriveType ?? string.Empty,
            Engine = updated.Car.Engine,
            Features = updated.Car.Features,
            Status = updated.Car.Status,
            Brand = new FinalProject.MVC.DTOs.BrandApiDto { Id = brand.Id, Name = brand.Name },
            Category = new FinalProject.MVC.DTOs.CategoryApiDto { Id = category.Id, Name = category.Name },
            Images = updated.Car.Images.Select(image => new FinalProject.MVC.DTOs.CarImageApiDto
            {
                ImageUrl = image.ImageUrl,
                IsMain = image.IsMain
            }).ToList()
        });

        Assert.Equal("Vault Test", mapped.Name);
        Assert.Equal(5, mapped.Seats);
        Assert.Equal("AWD", mapped.DriveType);
        Assert.Equal("2.0L Turbo", mapped.Engine);
        Assert.Contains("Touchscreen Display", mapped.Features);
        Assert.Contains("Fog Lights", mapped.Features);
        Assert.Equal("~/assets/images/portfolio/cars/uploads/existing.png", mapped.ImagePath);
    }

    [Fact]
    public void Listing_forms_expose_labels_native_dropdowns_and_saved_features()
    {
        var root = FindRepositoryRoot();
        var account = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_VehicleListingFields.cshtml"));
        var add = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "AddCar.cshtml"));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "EditListing.cshtml"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));
        var details = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "Details.cshtml"));
        var script = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "wwwroot", "assets", "js", "main.js"));

        Assert.Contains("Make / Brand", account, StringComparison.Ordinal);
        Assert.Contains("Enter mileage", account, StringComparison.Ordinal);
        Assert.Contains("data-combo=\"brand\"", account, StringComparison.Ordinal);
        Assert.Contains("Other model", account, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"brand-search\"", account, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"year-search\"", account, StringComparison.Ordinal);
        Assert.Contains("Select model year", account, StringComparison.Ordinal);
        Assert.Contains("Enter price", account, StringComparison.Ordinal);
        Assert.Contains("Listing Status", account, StringComparison.Ordinal);
        Assert.Contains("av-native-select", account, StringComparison.Ordinal);
        Assert.Contains("CarFeatureCatalog.All", account, StringComparison.Ordinal);
        Assert.Contains("feature.Label", account, StringComparison.Ordinal);
        Assert.Contains("Choose Photos", account, StringComparison.Ordinal);
        Assert.Contains("vehiclePhotos", account, StringComparison.Ordinal);
        Assert.Contains("Vehicle Photos", account, StringComparison.Ordinal);
        Assert.Contains("data-combo=\"engine\"", account, StringComparison.Ordinal);
        Assert.Contains("data-combo=\"horsepower\"", account, StringComparison.Ordinal);
        Assert.Contains("av-color-swatches", account, StringComparison.Ordinal);
        Assert.Contains("Car_Color_Selected", account, StringComparison.Ordinal);
        Assert.Contains("Car_Color_Name_Wrap", account, StringComparison.Ordinal);
        Assert.Contains("vehicle-photos-button", admin, StringComparison.Ordinal);
        Assert.Contains("Car_Color_Selected", admin, StringComparison.Ordinal);
        Assert.Contains("_BodyPartsPicker", account, StringComparison.Ordinal);
        Assert.Contains("_BodyPartsPicker", admin, StringComparison.Ordinal);
        var bodyParts = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_BodyPartsPicker.cshtml"));
        Assert.Contains("VehicleBodyPartCatalog.Parts", bodyParts, StringComparison.Ordinal);
        Assert.Contains("Leave unselected if you are not specifying any parts.", bodyParts, StringComparison.Ordinal);
        Assert.Contains("Front Left Door", FinalProject.Commerce.VehicleBodyPartCatalog.Parts);
        var colorScript = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "wwwroot", "js", "car-color-picker.js"));
        Assert.Contains("Custom color", colorScript, StringComparison.Ordinal);
        Assert.Contains("applyFromHexOrPicker", colorScript, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", add, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", edit, StringComparison.Ordinal);
        Assert.Contains("Seating Capacity", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"0\"", account, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"0\"", admin, StringComparison.Ordinal);
        Assert.Contains("foreach (var feature in Model.Features)", details, StringComparison.Ordinal);
        Assert.DoesNotContain("Start-Stop Technology", details, StringComparison.Ordinal);
        Assert.Contains(".not('.av-native-select')", script, StringComparison.Ordinal);
    }

    private static SaveCarRequestDto ValidRequest(int brandId = 1, int categoryId = 1) => new()
    {
        BrandId = brandId,
        CategoryId = categoryId,
        Model = "Test",
        Year = 2024,
        Price = 30000,
        Mileage = 12000,
        FuelType = "Petrol",
        Transmission = "Automatic",
        Color = "White",
        Seats = 5,
        DriveType = "awd",
        Engine = "2.0L Turbo",
        MileageUnit = "km",
        Horsepower = 248,
        Doors = 4,
        ConditionSpecified = true,
        Condition = "Used",
        FeaturesSpecified = true,
        Features = ["FogLights", "Touchscreen", "ClimateControl"],
        Description = "Well maintained coupe with a complete service history.",
        ImageUrl = "~/assets/images/portfolio/cars/uploads/existing.png",
        Status = "Active"
    };

    private static List<ValidationResult> Validate(SaveCarRequestDto request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-listing-" + Guid.NewGuid().ToString("N"))
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
