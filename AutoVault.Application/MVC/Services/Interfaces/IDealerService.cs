using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IDealerService
{
    Task<IReadOnlyList<Dealer>> GetPublishedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dealer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Dealer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        DealerEditViewModel model,
        DealerImageUploads uploads,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateAsync(
        DealerEditViewModel model,
        DealerImageUploads uploads,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> MoveAsync(int id, int direction, CancellationToken cancellationToken = default);
}
