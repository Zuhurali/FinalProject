using FinalProject.MVC.Commerce;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace FinalProject.Tests;

public class CarImageUploadTests
{
    [Fact]
    public void CarImageRules_accept_jpg_png_webp_and_reject_other_types()
    {
        Assert.Null(CarImageRules.Validate("car.jpg", 1024));
        Assert.Null(CarImageRules.Validate("car.JPEG", 1024));
        Assert.Null(CarImageRules.Validate("car.png", 1024));
        Assert.Null(CarImageRules.Validate("car.webp", 1024));
        Assert.Contains("JPG", CarImageRules.Validate("car.gif", 1024)!);
        Assert.Contains("5 MB", CarImageRules.Validate("car.jpg", CarImageRules.MaxBytes + 1)!);
        Assert.True(CarImageRules.IsAllowedExtension("photo.webp"));
        Assert.False(CarImageRules.IsAllowedExtension("photo.bmp"));
    }

    [Fact]
    public async Task Selected_local_image_overrides_url_and_writes_unique_file()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var car = new SaveCarDto { ImageUrl = "https://example.com/old.jpg" };
            await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var file = new FormFile(stream, 0, stream.Length, "vehiclePhotos", "studio.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var error = await service.ApplyAsync(car, file, requireImage: true);

            Assert.Null(error);
            Assert.StartsWith("~/assets/images/portfolio/cars/uploads/", car.ImageUrl, StringComparison.Ordinal);
            Assert.EndsWith(".png", car.ImageUrl, StringComparison.OrdinalIgnoreCase);
            var physical = Path.Combine(
                root,
                "assets",
                "images",
                "portfolio",
                "cars",
                "uploads",
                Path.GetFileName(car.ImageUrl));
            Assert.True(File.Exists(physical));
            Assert.Equal(4, new FileInfo(physical).Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_file_keeps_existing_url_on_edit()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var car = new SaveCarDto { ImageUrl = " ~/assets/images/portfolio/01.jpg " };

            var error = await service.ApplyAsync(car, imageFile: null, requireImage: true);

            Assert.Null(error);
            Assert.Equal("~/assets/images/portfolio/01.jpg", car.ImageUrl);
            Assert.DoesNotContain(
                Directory.EnumerateFiles(Path.Combine(root, "assets", "images", "portfolio", "cars", "uploads")),
                path => !path.EndsWith(".gitkeep", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Create_requires_at_least_one_photo()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var car = new SaveCarDto { ImageUrl = "   " };

            var error = await service.ApplyAsync(car, imageFile: null, requireImage: true);

            Assert.Contains("vehicle photo", error!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Vehicle_photos_use_first_file_as_primary_and_rest_as_gallery()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var car = new SaveCarDto();
            var files = new List<IFormFile>
            {
                FormFile("primary.jpg", [1, 2, 3]),
                FormFile("side.png", [4, 5, 6, 7]),
                FormFile("rear.webp", [8, 9])
            };

            var error = await service.ApplyVehiclePhotosAsync(car, files, requireImage: true);

            Assert.Null(error);
            Assert.EndsWith(".jpg", car.ImageUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, car.AdditionalImageUrls.Count);
            Assert.EndsWith(".png", car.AdditionalImageUrls[0], StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".webp", car.AdditionalImageUrls[1], StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Physical(root, car.ImageUrl)));
            Assert.All(car.AdditionalImageUrls, path => Assert.True(File.Exists(Physical(root, path))));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Vehicle_photos_empty_selection_preserves_existing_primary_on_edit()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var existing = "~/assets/images/portfolio/cars/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";
            var car = new SaveCarDto { ImageUrl = existing };

            var error = await service.ApplyVehiclePhotosAsync(car, Array.Empty<IFormFile>(), requireImage: true);

            Assert.Null(error);
            Assert.Equal(existing, car.ImageUrl);
            Assert.Empty(car.AdditionalImageUrls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Admin_and_account_forms_use_one_multi_photo_picker()
    {
        var root = FindRepositoryRoot();
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));
        var account = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_VehicleListingFields.cshtml"));
        var create = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Create.cshtml"));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Edit.cshtml"));

        foreach (var fields in new[] { admin, account })
        {
            Assert.Contains("Vehicle Photos", fields, StringComparison.Ordinal);
            Assert.Contains("Choose Photos", fields, StringComparison.Ordinal);
            Assert.Contains("name=\"vehiclePhotos\"", fields, StringComparison.Ordinal);
            Assert.Contains("multiple", fields, StringComparison.Ordinal);
            Assert.Contains("asp-for=\"Car.ImageUrl\"", fields, StringComparison.Ordinal);
            Assert.Contains("type=\"hidden\"", fields, StringComparison.Ordinal);
            Assert.DoesNotContain("name=\"imageFile\"", fields, StringComparison.Ordinal);
            Assert.DoesNotContain("name=\"galleryFiles\"", fields, StringComparison.Ordinal);
            Assert.DoesNotContain("Primary image</label>", fields, StringComparison.Ordinal);
            Assert.DoesNotContain("Car Gallery Images", fields, StringComparison.Ordinal);
            Assert.DoesNotContain(">Add Images<", fields, StringComparison.Ordinal);
        }

        Assert.Contains("enctype=\"multipart/form-data\"", create, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", edit, StringComparison.Ordinal);
        Assert.Contains("Show on homepage", admin, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"Car.IsFeatured\"", admin, StringComparison.Ordinal);
    }

    private static IFormFile FormFile(string name, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "vehiclePhotos", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private static string Physical(string root, string relative) =>
        Path.Combine(
            root,
            "assets",
            "images",
            "portfolio",
            "cars",
            "uploads",
            Path.GetFileName(relative));

    private static string CreateTempWebRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "autovault-car-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "assets", "images", "portfolio", "cars", "uploads"));
        return root;
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

    private sealed class FakeWebHost : IWebHostEnvironment
    {
        public FakeWebHost(string webRootPath)
        {
            WebRootPath = webRootPath;
            ContentRootPath = webRootPath;
            EnvironmentName = "Development";
            ApplicationName = "FinalProject.MVC";
            WebRootFileProvider = new NullFileProvider();
            ContentRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
