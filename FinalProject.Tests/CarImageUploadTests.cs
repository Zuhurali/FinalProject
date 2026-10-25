using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models.Api;
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
            var file = new FormFile(stream, 0, stream.Length, "imageFile", "studio.png")
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
    public async Task Create_requires_url_or_file()
    {
        var root = CreateTempWebRoot();
        try
        {
            var service = new CarImageUploadService(new FakeWebHost(root));
            var car = new SaveCarDto { ImageUrl = "   " };

            var error = await service.ApplyAsync(car, imageFile: null, requireImage: true);

            Assert.Contains("path or URL", error!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Admin_car_forms_include_url_input_and_choose_file_button()
    {
        var root = FindRepositoryRoot();
        var fields = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));
        var create = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Create.cshtml"));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Edit.cshtml"));

        Assert.Contains("asp-for=\"Car.ImageUrl\"", fields, StringComparison.Ordinal);
        Assert.Contains("Choose File", fields, StringComparison.Ordinal);
        Assert.Contains("name=\"imageFile\"", fields, StringComparison.Ordinal);
        Assert.Contains("accept=\".jpg,.jpeg,.png,.webp", fields, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", create, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", edit, StringComparison.Ordinal);
        Assert.Contains("car-image-preview", fields, StringComparison.Ordinal);
    }

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
