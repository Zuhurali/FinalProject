using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public Task<bool> AnyPublishedShopFaqItemsAsync(CancellationToken cancellationToken = default) =>
        _db.FaqItems.AnyAsync(item => item.ShowOnShop, cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> GetPublishedFaqItemsAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> GetShopPublishedFaqItemsAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems.AsNoTracking()
            .Where(item => item.IsPublished && item.ShowOnShop)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> GetAllFaqItemsAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems.AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<FaqItem?> GetFaqItemByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.FaqItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<FaqItem?> FindTrackedFaqItemAsync(int id, CancellationToken cancellationToken = default) =>
        _db.FaqItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<int?> GetMaxFaqSortOrderAsync(CancellationToken cancellationToken = default) =>
        _db.FaqItems.Select(item => (int?)item.SortOrder).MaxAsync(cancellationToken);

    public void AddFaqItem(FaqItem item) => _db.FaqItems.Add(item);

    public void RemoveFaqItem(FaqItem item) => _db.FaqItems.Remove(item);

    public async Task<IReadOnlyList<FaqItem>> GetFaqItemsOrderedForMoveAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
}
