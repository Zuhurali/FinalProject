using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public SiteSettings? GetSiteSettingsSingletonReadOnly() =>
        _db.SiteSettings.AsNoTracking()
            .FirstOrDefault(settings => settings.Id == SiteSettings.SingletonId);

    public Task<bool> AnySiteSettingsSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.SiteSettings.AnyAsync(settings => settings.Id == SiteSettings.SingletonId, cancellationToken);

    public Task<SiteSettings?> FindTrackedSiteSettingsSingletonAsync(CancellationToken cancellationToken = default) =>
        _db.SiteSettings.FirstOrDefaultAsync(settings => settings.Id == SiteSettings.SingletonId, cancellationToken);

    public void AddSiteSettings(SiteSettings settings) => _db.SiteSettings.Add(settings);
}
