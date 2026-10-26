using FinalProject.MVC.Configuration;
using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface ISiteSettingsService
{
    SitePublicSettings GetPublic();

    DealershipContactSettings GetContactSettings();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        SiteSettingsEditViewModel model,
        IFormFile? logoFile,
        IFormFile? footerLogoFile,
        IFormFile? faviconFile,
        CancellationToken cancellationToken = default);
}

public sealed class SitePublicSettings
{
    public string SiteName { get; init; } = "AutoVault";

    public DealershipContactSettings Contact { get; init; } = new();

    public string LogoPath { get; init; } = "~/assets/images/logo/logo.svg";

    public string FooterLogoPath { get; init; } = "~/assets/images/logo/footer-logo.svg";

    public string FaviconPath { get; init; } = "~/fav.svg";

    public string FooterText { get; init; } = string.Empty;

    public string CopyrightText { get; init; } = "Copyright © {year} AutoVault. All rights reserved.";

    public string? FacebookUrl { get; init; }

    public string? InstagramUrl { get; init; }

    public string? YoutubeUrl { get; init; }

    public string? TwitterUrl { get; init; }

    public string DefaultSeoTitle { get; init; } = "AutoVault Car Dealer";

    public string DefaultMetaDescription { get; init; } = string.Empty;

    public string FormatCopyright(int year) =>
        (CopyrightText ?? string.Empty).Replace("{year}", year.ToString(), StringComparison.OrdinalIgnoreCase);

    public bool HasSocialLinks =>
        !string.IsNullOrWhiteSpace(FacebookUrl)
        || !string.IsNullOrWhiteSpace(InstagramUrl)
        || !string.IsNullOrWhiteSpace(YoutubeUrl)
        || !string.IsNullOrWhiteSpace(TwitterUrl);
}
