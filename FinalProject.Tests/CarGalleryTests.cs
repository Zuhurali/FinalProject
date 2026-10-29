using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.MVC.Commerce;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class CarGalleryTests
{
    [Fact]
    public async Task Edit_keeps_every_existing_image_and_can_add_another()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var service = new CarService(db, new SilentNotifications());

        var updated = await service.UpdateAsync(car.Id, Request(car, gallery: true, additional:
        [
            "/assets/images/catalog/convertible/mercedes-sl-r231-trunk.jpg"
        ]));

        Assert.NotNull(updated);
        Assert.Equal(4, updated!.Car.Images.Count);
        Assert.Equal(
            ["/assets/images/catalog/convertible/mercedes-sl-class-black.jpg"],
            updated.Car.Images.Where(image => image.IsMain).Select(image => image.ImageUrl).ToArray());
        Assert.Contains(updated.Car.Images, image => image.ImageUrl.EndsWith("trunk.jpg", StringComparison.Ordinal));
        Assert.Empty(updated.RemovedImageUrls ?? []);
    }

    [Fact]
    public async Task Removing_one_image_leaves_the_other_gallery_images()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var second = car.Images.Single(image => image.DisplayOrder == 1);
        var service = new CarService(db, new SilentNotifications());

        var updated = await service.UpdateAsync(car.Id, Request(car, gallery: true, remove: [second.Id]));

        Assert.NotNull(updated);
        Assert.Equal(2, updated!.Car.Images.Count);
        Assert.DoesNotContain(updated.Car.Images, image => image.Id == second.Id);
        Assert.Contains(updated.Car.Images, image => image.ImageUrl.EndsWith("black.jpg", StringComparison.Ordinal));
        Assert.Contains(updated.Car.Images, image => image.ImageUrl.EndsWith("cabin.jpg", StringComparison.Ordinal));
        Assert.Equal([second.ImageUrl], updated.RemovedImageUrls);
    }

    [Fact]
    public async Task Changing_the_primary_image_does_not_remove_the_gallery()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var interior = car.Images.Single(image => image.DisplayOrder == 2);
        var service = new CarService(db, new SilentNotifications());
        var request = Request(car, gallery: true);
        request.PrimaryImageId = interior.Id;
        request.ImageUrl = interior.ImageUrl;

        var updated = await service.UpdateAsync(car.Id, request);

        Assert.NotNull(updated);
        Assert.Equal(3, updated!.Car.Images.Count);
        Assert.Equal(interior.ImageUrl, updated.Car.Images.Single(image => image.IsMain).ImageUrl);
        Assert.Empty(updated.RemovedImageUrls ?? []);
    }

    [Fact]
    public async Task Replacing_the_primary_file_keeps_the_other_images()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var service = new CarService(db, new SilentNotifications());
        var request = Request(car, gallery: true);
        request.PrimaryImageId = car.Images.Single(image => image.IsMain).Id;
        request.ImageUrl = "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";

        var updated = await service.UpdateAsync(car.Id, request);

        Assert.NotNull(updated);
        Assert.Equal(3, updated!.Car.Images.Count);
        Assert.Equal(request.ImageUrl, updated.Car.Images.Single(image => image.IsMain).ImageUrl);
        Assert.Contains(updated.Car.Images, image => image.ImageUrl.EndsWith("dashboard.jpg", StringComparison.Ordinal));
        Assert.Contains(updated.Car.Images, image => image.ImageUrl.EndsWith("cabin.jpg", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_normal_save_without_gallery_instructions_does_not_delete_images()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var service = new CarService(db, new SilentNotifications());
        var request = Request(car, gallery: false);
        request.Price = 91000;

        var updated = await service.UpdateAsync(car.Id, request);

        Assert.NotNull(updated);
        Assert.Equal(91000, updated!.Car.Price);
        Assert.Equal(3, updated.Car.Images.Count);
    }

    [Fact]
    public async Task Unsafe_gallery_paths_are_rejected_and_shared_files_are_not_released()
    {
        await using var db = CreateDb();
        var car = await SeedGalleryCarAsync(db);
        var other = new Car
        {
            BrandId = car.BrandId,
            CategoryId = car.CategoryId,
            Model = "Other",
            Year = 2020,
            Price = 10000,
            Mileage = 1,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Red",
            Description = "Other",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            Images =
            {
                new CarImage
                {
                    ImageUrl = car.Images.Single(image => image.DisplayOrder == 1).ImageUrl,
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        db.Cars.Add(other);
        await db.SaveChangesAsync();

        var service = new CarService(db, new SilentNotifications());
        var unsafeRequest = Request(car, gallery: true, additional: ["../secret.png"]);
        await Assert.ThrowsAsync<CarGalleryException>(() => service.UpdateAsync(car.Id, unsafeRequest));

        var shared = car.Images.Single(image => image.DisplayOrder == 1);
        var updated = await service.UpdateAsync(car.Id, Request(car, gallery: true, remove: [shared.Id]));
        Assert.NotNull(updated);
        Assert.DoesNotContain(updated!.RemovedImageUrls ?? [], url => url == shared.ImageUrl);
    }

    [Fact]
    public async Task Create_can_store_a_primary_image_and_extra_gallery_images()
    {
        await using var db = CreateDb();
        var brand = db.Brands.Add(new Brand { Name = "Gallery", CreatedAt = DateTime.UtcNow }).Entity;
        var category = db.Categories.Add(new Category { Name = "Coupe" }).Entity;
        await db.SaveChangesAsync();
        var service = new CarService(db, new SilentNotifications());

        var created = await service.CreateCatalogAsync(new SaveCarRequest
        {
            BrandId = brand.Id,
            CategoryId = category.Id,
            Model = "New",
            Year = 2024,
            Price = 20000,
            Mileage = 10,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Blue",
            Description = "New gallery car.",
            ImageUrl = "/assets/images/catalog/one.jpg",
            Status = "Active",
            GallerySpecified = true,
            AdditionalImageUrls = ["/assets/images/catalog/two.jpg", "/assets/images/catalog/three.jpg"]
        });

        Assert.NotNull(created);
        Assert.Equal(3, created!.Images.Count);
        Assert.True(created.Images.Single(image => image.DisplayOrder == 0).IsMain);
        Assert.Equal(2, created.Images.Count(image => !image.IsMain));
    }

    [Fact]
    public void Upload_rules_reject_invalid_files_and_only_resolve_managed_uploads()
    {
        Assert.Contains("JPG", CarImageRules.Validate("car.gif", 100)!);
        Assert.Null(CarImageRules.Validate("car.webp", 100));
        var root = Path.Combine(Path.GetTempPath(), "autovault-gallery-" + Guid.NewGuid().ToString("N"));
        var uploads = Path.Combine(root, "assets", "images", "portfolio", "cars", "uploads");
        Directory.CreateDirectory(uploads);
        try
        {
            var managed = Path.Combine(uploads, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg");
            File.WriteAllBytes(managed, [1, 2, 3]);
            Assert.True(CarImageRules.TryResolveUpload(
                "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg",
                root,
                out var physical));
            Assert.Equal(Path.GetFullPath(managed), physical);
            Assert.False(CarImageRules.TryResolveUpload(
                "/assets/images/catalog/convertible/mercedes-sl-class-black.jpg",
                root,
                out _));
            Assert.False(CarImageRules.TryResolveUpload(
                "~/assets/images/portfolio/cars/uploads/../secret.jpg",
                root,
                out _));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Admin_edit_form_lists_saved_photos_in_unified_vehicle_photos_control()
    {
        var root = FindRepositoryRoot();
        var fields = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Edit.cshtml"));

        Assert.Contains("Vehicle Photos", fields, StringComparison.Ordinal);
        Assert.Contains("Model.Gallery", fields, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Remove photo\"", fields, StringComparison.Ordinal);
        Assert.Contains("name=\"vehiclePhotos\"", fields, StringComparison.Ordinal);
        Assert.Contains("multiple", fields, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", edit, StringComparison.Ordinal);
    }

    private static SaveCarRequest Request(
        Car car,
        bool gallery,
        IEnumerable<string>? additional = null,
        IEnumerable<int>? remove = null) => new()
    {
        BrandId = car.BrandId,
        CategoryId = car.CategoryId,
        Model = car.Model,
        Year = car.Year,
        Price = car.Price,
        Mileage = car.Mileage,
        FuelType = car.FuelType,
        Transmission = car.Transmission,
        Color = car.Color,
        Description = car.Description,
        ImageUrl = car.Images.Single(image => image.IsMain).ImageUrl,
        Status = car.Status,
        GallerySpecified = gallery,
        PrimaryImageId = car.Images.Single(image => image.IsMain).Id,
        AdditionalImageUrls = additional?.ToList() ?? [],
        RemoveImageIds = remove?.ToList() ?? []
    };

    private static async Task<Car> SeedGalleryCarAsync(FinalProjectDbContext db)
    {
        var brand = db.Brands.Add(new Brand { Name = "Mercedes-Benz", CreatedAt = DateTime.UtcNow }).Entity;
        var category = db.Categories.Add(new Category { Name = "Convertible" }).Entity;
        await db.SaveChangesAsync();
        var car = new Car
        {
            BrandId = brand.Id,
            CategoryId = category.Id,
            Model = "SL-Class",
            Year = 2019,
            Price = 92000,
            Mileage = 18200,
            FuelType = "Petrol",
            Transmission = "Automatic",
            Color = "Black",
            Description = "Mercedes-Benz SL-Class",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            Images =
            {
                new CarImage { ImageUrl = "/assets/images/catalog/convertible/mercedes-sl-class-black.jpg", IsMain = true, DisplayOrder = 0 },
                new CarImage { ImageUrl = "/assets/images/catalog/convertible/mercedes-sl-r231-dashboard.jpg", IsMain = false, DisplayOrder = 1 },
                new CarImage { ImageUrl = "/assets/images/catalog/convertible/mercedes-sl-r231-cabin.jpg", IsMain = false, DisplayOrder = 2 }
            }
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("car-gallery-" + Guid.NewGuid().ToString("N"))
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
