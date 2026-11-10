using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    SiteSettings? GetSiteSettingsSingletonReadOnly();
    Task<bool> AnySiteSettingsSingletonAsync(CancellationToken cancellationToken = default);
    Task<SiteSettings?> FindTrackedSiteSettingsSingletonAsync(CancellationToken cancellationToken = default);
    void AddSiteSettings(SiteSettings settings);
}
