using FinalProject.MVC.Commerce;
using FinalProject.MVC.Configuration;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FinalProject.MVC.Services;

public sealed class SiteSettingsService : ISiteSettingsService
{
    public const string CacheKey = "site-settings-public-v1";

    private readonly IApplicationRepository _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly DealershipContactSettings _configDefaults;
    private readonly object _gate = new();

    public SiteSettingsService(
        IApplicationRepository db,
        IMemoryCache cache,
        IWebHostEnvironment environment,
        IOptions<DealershipContactSettings> configDefaults)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
        _configDefaults = configDefaults.Value;
    }

    public SitePublicSettings GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out SitePublicSettings? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.GetSiteSettingsSingletonReadOnly();
            var publicSettings = Map(entity);
            _cache.Set(CacheKey, publicSettings, TimeSpan.FromMinutes(10));
            return publicSettings;
        }
    }

    public DealershipContactSettings GetContactSettings() => GetPublic().Contact;

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _db.AnySiteSettingsSingletonAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        var seed = CreateSeedEntity();
        _db.AddSiteSettings(seed);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        SiteSettingsEditViewModel model,
        IFormFile? logoFile,
        IFormFile? footerLogoFile,
        IFormFile? faviconFile,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedSiteSettingsSingletonAsync(cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.AddSiteSettings(entity);
        }

        var e164 = DealershipContactSettings.NormalizeE164(model.E164Phone);
        var whatsApp = DealershipContactSettings.NormalizeWhatsAppDigits(model.WhatsAppDigits, e164);
        var facebook = NormalizeOptionalUrl(model.FacebookUrl, out var facebookError);
        if (facebookError != null) return (false, facebookError);
        var instagram = NormalizeOptionalUrl(model.InstagramUrl, out var instagramError);
        if (instagramError != null) return (false, instagramError);
        var youtube = NormalizeOptionalUrl(model.YoutubeUrl, out var youtubeError);
        if (youtubeError != null) return (false, youtubeError);
        var twitter = NormalizeOptionalUrl(model.TwitterUrl, out var twitterError);
        if (twitterError != null) return (false, twitterError);

        var logoPath = model.LogoPath?.Trim() ?? entity.LogoPath;
        var footerLogoPath = model.FooterLogoPath?.Trim() ?? entity.FooterLogoPath;
        var faviconPath = model.FaviconPath?.Trim() ?? entity.FaviconPath;

        var logoUpload = await TrySaveBrandAssetAsync(logoFile, cancellationToken);
        if (logoUpload.Error != null) return (false, logoUpload.Error);
        if (logoUpload.Path != null) logoPath = logoUpload.Path;

        var footerUpload = await TrySaveBrandAssetAsync(footerLogoFile, cancellationToken);
        if (footerUpload.Error != null) return (false, footerUpload.Error);
        if (footerUpload.Path != null) footerLogoPath = footerUpload.Path;

        var faviconUpload = await TrySaveBrandAssetAsync(faviconFile, cancellationToken);
        if (faviconUpload.Error != null) return (false, faviconUpload.Error);
        if (faviconUpload.Path != null) faviconPath = faviconUpload.Path;

        var logoPathError = SiteBrandAssetRules.ValidateStoredPath(logoPath);
        if (logoPathError != null) return (false, logoPathError);
        var footerPathError = SiteBrandAssetRules.ValidateStoredPath(footerLogoPath);
        if (footerPathError != null) return (false, footerPathError);
        var faviconPathError = SiteBrandAssetRules.ValidateStoredPath(faviconPath);
        if (faviconPathError != null) return (false, faviconPathError);

        entity.SiteName = model.SiteName.Trim();
        entity.DisplayPhone = string.IsNullOrWhiteSpace(model.DisplayPhone)
            ? DealershipContactDefaults.DisplayPhone
            : model.DisplayPhone.Trim();
        entity.E164Phone = e164;
        entity.WhatsAppDigits = whatsApp;
        entity.Email = model.Email.Trim();
        entity.AddressLine = model.AddressLine.Trim();
        entity.MapsQuery = string.IsNullOrWhiteSpace(model.MapsQuery)
            ? model.AddressLine.Trim()
            : model.MapsQuery.Trim();
        entity.LogoPath = logoPath;
        entity.FooterLogoPath = footerLogoPath;
        entity.FaviconPath = faviconPath;
        entity.FooterText = model.FooterText.Trim();
        entity.CopyrightText = model.CopyrightText.Trim();
        entity.FacebookUrl = facebook;
        entity.InstagramUrl = instagram;
        entity.YoutubeUrl = youtube;
        entity.TwitterUrl = twitter;
        entity.DefaultSeoTitle = model.DefaultSeoTitle.Trim();
        entity.DefaultMetaDescription = model.DefaultMetaDescription.Trim();
        entity.CallCtaHeadline = string.IsNullOrWhiteSpace(model.CallCtaHeadline)
            ? "If you have any questions Please Call."
            : model.CallCtaHeadline.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    private SiteSettings CreateSeedEntity()
    {
        var contact = _configDefaults;
        return new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            SiteName = "AutoVault",
            DisplayPhone = string.IsNullOrWhiteSpace(contact.DisplayPhone)
                ? DealershipContactDefaults.DisplayPhone
                : contact.DisplayPhone.Trim(),
            E164Phone = DealershipContactSettings.NormalizeE164(contact.E164Phone),
            WhatsAppDigits = DealershipContactSettings.NormalizeWhatsAppDigits(
                contact.WhatsAppDigits,
                contact.E164Phone),
            Email = string.IsNullOrWhiteSpace(contact.Email)
                ? DealershipContactDefaults.Email
                : contact.Email.Trim(),
            AddressLine = string.IsNullOrWhiteSpace(contact.AddressLine)
                ? DealershipContactDefaults.AddressLine
                : contact.AddressLine.Trim(),
            MapsQuery = string.IsNullOrWhiteSpace(contact.MapsQuery)
                ? DealershipContactDefaults.MapsQuery
                : contact.MapsQuery.Trim(),
            CallCtaHeadline = HomePageContentDefaults.CallCtaHeadline,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private SitePublicSettings Map(SiteSettings? entity)
    {
        if (entity == null)
        {
            var seed = CreateSeedEntity();
            return Map(seed);
        }

        return new SitePublicSettings
        {
            SiteName = string.IsNullOrWhiteSpace(entity.SiteName) ? "AutoVault" : entity.SiteName.Trim(),
            Contact = new DealershipContactSettings
            {
                DisplayPhone = string.IsNullOrWhiteSpace(entity.DisplayPhone)
                    ? DealershipContactDefaults.DisplayPhone
                    : entity.DisplayPhone.Trim(),
                E164Phone = DealershipContactSettings.NormalizeE164(entity.E164Phone),
                WhatsAppDigits = DealershipContactSettings.NormalizeWhatsAppDigits(
                    entity.WhatsAppDigits,
                    entity.E164Phone),
                Email = string.IsNullOrWhiteSpace(entity.Email)
                    ? DealershipContactDefaults.Email
                    : entity.Email.Trim(),
                AddressLine = string.IsNullOrWhiteSpace(entity.AddressLine)
                    ? DealershipContactDefaults.AddressLine
                    : entity.AddressLine.Trim(),
                MapsQuery = string.IsNullOrWhiteSpace(entity.MapsQuery)
                    ? DealershipContactDefaults.MapsQuery
                    : entity.MapsQuery.Trim()
            },
            LogoPath = SiteBrandAssetRules.IsSafeStoredPath(entity.LogoPath)
                ? entity.LogoPath
                : "~/assets/images/logo/logo.svg",
            FooterLogoPath = SiteBrandAssetRules.IsSafeStoredPath(entity.FooterLogoPath)
                ? entity.FooterLogoPath
                : "~/assets/images/logo/footer-logo.svg",
            FaviconPath = SiteBrandAssetRules.IsSafeStoredPath(entity.FaviconPath)
                ? entity.FaviconPath
                : "~/fav.svg",
            FooterText = string.IsNullOrWhiteSpace(entity.FooterText)
                ? "Your trusted partner for quality vehicles and exceptional service. Find your perfect car with confidence at AutoVault."
                : entity.FooterText.Trim(),
            CopyrightText = string.IsNullOrWhiteSpace(entity.CopyrightText)
                ? "Copyright © {year} AutoVault. All rights reserved."
                : entity.CopyrightText.Trim(),
            FacebookUrl = entity.FacebookUrl,
            InstagramUrl = entity.InstagramUrl,
            YoutubeUrl = entity.YoutubeUrl,
            TwitterUrl = entity.TwitterUrl,
            DefaultSeoTitle = string.IsNullOrWhiteSpace(entity.DefaultSeoTitle)
                ? "AutoVault Car Dealer"
                : entity.DefaultSeoTitle.Trim(),
            DefaultMetaDescription = string.IsNullOrWhiteSpace(entity.DefaultMetaDescription)
                ? "Your trusted source for expert automotive services and quality cars. Find your perfect vehicle with our extensive inventory and professional service."
                : entity.DefaultMetaDescription.Trim(),
            CallCtaHeadline = string.IsNullOrWhiteSpace(entity.CallCtaHeadline)
                ? "If you have any questions Please Call."
                : entity.CallCtaHeadline.Trim()
        };
    }

    private async Task<(string? Path, string? Error)> TrySaveBrandAssetAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return (null, null);
        }

        var validation = SiteBrandAssetRules.ValidateUpload(file.FileName, file.ContentType, file.Length);
        if (validation != null)
        {
            return (null, validation);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(_environment.WebRootPath, "assets", "images", "site", "uploads");
        Directory.CreateDirectory(folder);
        var physical = Path.Combine(folder, fileName);
        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return ($"{SiteBrandAssetRules.UploadFolderWeb}/{fileName}", null);
    }

    private static string? NormalizeOptionalUrl(string? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "Social links must be http or https URLs.";
            return null;
        }

        return trimmed;
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}
