using FinalProject.MVC.DTOs;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarImageUploadService
{
    Task<string?> ApplyAsync(
        SaveCarDto car,
        IFormFile? imageFile,
        bool requireImage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unified multi-photo upload: the first non-empty file becomes the primary
    /// (<see cref="SaveCarDto.ImageUrl"/>); remaining files are appended as gallery paths.
    /// </summary>
    Task<string?> ApplyVehiclePhotosAsync(
        SaveCarDto car,
        IEnumerable<IFormFile>? vehiclePhotos,
        bool requireImage,
        CancellationToken cancellationToken = default);

    Task<string?> SaveGalleryAsync(
        ICollection<string> destination,
        IEnumerable<IFormFile>? files,
        CancellationToken cancellationToken = default);

    void DeleteUploads(IEnumerable<string>? urls);
}
