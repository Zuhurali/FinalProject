using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public PartnerStripSettings? GetPartnerStripSettingsSingletonReadOnly() =>
        _db.PartnerStripSettings.AsNoTracking()
            .FirstOrDefault(s => s.Id == PartnerStripSettings.SingletonId);

    public Task<bool> AnyPartnerStripSettingsSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.PartnerStripSettings.AnyAsync(s => s.Id == PartnerStripSettings.SingletonId, cancellationToken);

    public Task<PartnerStripSettings?> FindTrackedPartnerStripSettingsSingletonAsync(
        CancellationToken cancellationToken = default) =>
        _db.PartnerStripSettings.FirstOrDefaultAsync(
            s => s.Id == PartnerStripSettings.SingletonId,
            cancellationToken);

    public void AddPartnerStripSettings(PartnerStripSettings settings) => _db.PartnerStripSettings.Add(settings);

    public IReadOnlyList<PartnerStripItem> GetPublishedPartnerStripItemsReadOnly() =>
        _db.PartnerStripItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToList();

    public Task<bool> AnyPartnerStripItemsAsync(CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<PartnerStripItem>> GetAllPartnerStripItemsReadOnlyAsync(
        CancellationToken cancellationToken = default) =>
        await _db.PartnerStripItems.AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<PartnerStripItem?> GetPartnerStripItemByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<int?> GetMaxPartnerStripItemSortOrderAsync(CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.Select(i => (int?)i.SortOrder).MaxAsync(cancellationToken);

    public void AddPartnerStripItem(PartnerStripItem item) => _db.PartnerStripItems.Add(item);

    public Task<PartnerStripItem?> FindTrackedPartnerStripItemAsync(int id, CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public void RemovePartnerStripItem(PartnerStripItem item) => _db.PartnerStripItems.Remove(item);

    public async Task<IReadOnlyList<PartnerStripItem>> GetPartnerStripItemsOrderedAsync(
        CancellationToken cancellationToken = default) =>
        await _db.PartnerStripItems.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync(cancellationToken);
}
