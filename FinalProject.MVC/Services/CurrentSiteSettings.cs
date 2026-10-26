using FinalProject.MVC.Configuration;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class CurrentSiteSettings : ICurrentSiteSettings
{
    private readonly Lazy<SitePublicSettings> _current;

    public CurrentSiteSettings(ISiteSettingsService siteSettings)
    {
        _current = new Lazy<SitePublicSettings>(siteSettings.GetPublic);
    }

    public SitePublicSettings Current => _current.Value;

    public DealershipContactSettings Contact => Current.Contact;
}
