using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    PartnerStripSettings? GetPartnerStripSettingsSingletonReadOnly();
    Task<bool> AnyPartnerStripSettingsSingletonAsync(CancellationToken cancellationToken = default);
    Task<PartnerStripSettings?> FindTrackedPartnerStripSettingsSingletonAsync(CancellationToken cancellationToken = default);
    void AddPartnerStripSettings(PartnerStripSettings settings);

    IReadOnlyList<PartnerStripItem> GetPublishedPartnerStripItemsReadOnly();
    Task<bool> AnyPartnerStripItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerStripItem>> GetAllPartnerStripItemsReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<PartnerStripItem?> GetPartnerStripItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int?> GetMaxPartnerStripItemSortOrderAsync(CancellationToken cancellationToken = default);
    void AddPartnerStripItem(PartnerStripItem item);
    Task<PartnerStripItem?> FindTrackedPartnerStripItemAsync(int id, CancellationToken cancellationToken = default);
    void RemovePartnerStripItem(PartnerStripItem item);
    Task<IReadOnlyList<PartnerStripItem>> GetPartnerStripItemsOrderedAsync(CancellationToken cancellationToken = default);
}
