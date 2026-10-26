using FinalProject.MVC.Configuration;

namespace FinalProject.MVC.Services.Interfaces;

/// <summary>Request-scoped site settings snapshot for views and controllers.</summary>
public interface ICurrentSiteSettings
{
    SitePublicSettings Current { get; }

    DealershipContactSettings Contact { get; }
}
