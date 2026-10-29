using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class CarImageUploadService : ICarImageUploadService
{
    private readonly IWebHostEnvironment _environment;

    public CarImageUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> ApplyAsync(
        SaveCarDto car,
        IFormFile? imageFile,
        bool requireImage,
        CancellationToken cancellationToken = default)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var error = CarImageRules.Validate(imageFile.FileName, imageFile.Length);
            if (error != null)
            {
                return error;
            }

            car.ImageUrl = await WriteAsync(imageFile, cancellationToken);
            return null;
        }

        car.ImageUrl = car.ImageUrl?.Trim() ?? string.Empty;
        if (requireImage && string.IsNullOrWhiteSpace(car.ImageUrl))
        {
            return "Choose at least one vehicle photo.";
        }

        return null;
    }

    public async Task<string?> ApplyVehiclePhotosAsync(
        SaveCarDto car,
        IEnumerable<IFormFile>? vehiclePhotos,
        bool requireImage,
        CancellationToken cancellationToken = default)
    {
        var files = (vehiclePhotos ?? Array.Empty<IFormFile>())
            .Where(file => file is { Length: > 0 })
            .ToList();

        // First selected file is primary; remaining files are gallery additions.
        var maxTotal = 1 + CarImageRules.MaxGalleryFilesPerSave;
        if (files.Count > maxTotal)
        {
            return $"You can upload up to {maxTotal} photos at a time.";
        }

        var primary = files.Count > 0 ? files[0] : null;
        var gallery = files.Skip(1).ToList();

        var primaryError = await ApplyAsync(car, primary, requireImage, cancellationToken);
        if (primaryError != null)
        {
            return primaryError;
        }

        return await SaveGalleryAsync(car.AdditionalImageUrls, gallery, cancellationToken);
    }

    public async Task<string?> SaveGalleryAsync(
        ICollection<string> destination,
        IEnumerable<IFormFile>? files,
        CancellationToken cancellationToken = default)
    {
        var selected = (files ?? Array.Empty<IFormFile>())
            .Where(file => file != null && file.Length > 0)
            .ToList();
        if (selected.Count > CarImageRules.MaxGalleryFilesPerSave)
        {
            return $"You can add up to {CarImageRules.MaxGalleryFilesPerSave} images at a time.";
        }

        foreach (var file in selected)
        {
            var error = CarImageRules.Validate(file.FileName, file.Length);
            if (error != null)
            {
                return error;
            }
        }

        var written = new List<string>();
        try
        {
            foreach (var file in selected)
            {
                var path = await WriteAsync(file, cancellationToken);
                written.Add(path);
                destination.Add(path);
            }
        }
        catch (IOException)
        {
            DeleteUploads(written);
            foreach (var path in written)
            {
                destination.Remove(path);
            }

            return "One of the gallery images could not be saved. Try again.";
        }

        return null;
    }

    public void DeleteUploads(IEnumerable<string>? urls)
    {
        if (urls == null)
        {
            return;
        }

        foreach (var url in urls)
        {
            if (!CarImageRules.TryResolveUpload(url, _environment.WebRootPath, out var physical))
            {
                continue;
            }

            if (File.Exists(physical))
            {
                File.Delete(physical);
            }
        }
    }

    private async Task<string> WriteAsync(IFormFile imageFile, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(
            _environment.WebRootPath,
            "assets",
            "images",
            "portfolio",
            "cars",
            "uploads");
        Directory.CreateDirectory(folder);
        var physical = Path.Combine(folder, fileName);
        await using (var stream = File.Create(physical))
        {
            await imageFile.CopyToAsync(stream, cancellationToken);
        }

        return CarImageRules.BuildRelativePath(fileName);
    }
}
