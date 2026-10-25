using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarImageUploadService
{
    Task<string?> ApplyAsync(
        SaveCarDto car,
        IFormFile? imageFile,
        bool requireImage,
        CancellationToken cancellationToken = default);

    Task<string?> SaveGalleryAsync(
        ICollection<string> destination,
        IEnumerable<IFormFile>? files,
        CancellationToken cancellationToken = default);

    void DeleteUploads(IEnumerable<string>? urls);
}
