using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IFaqService
{
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> GetPublishedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> GetShopPublishedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FaqItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        FaqItemEditViewModel model,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateAsync(
        FaqItemEditViewModel model,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> MoveAsync(int id, int direction, CancellationToken cancellationToken = default);
}
