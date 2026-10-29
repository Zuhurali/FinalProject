using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class AboutPageContentService : IAboutPageContentService
{
    public const string CacheKey = "about-page-content-public-v1";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    public AboutPageContentService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IWebHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    public AboutPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out AboutPagePublicContent? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.AboutPageContents.AsNoTracking()
                .FirstOrDefault(content => content.Id == AboutPageContent.SingletonId);
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _db.AboutPageContents.AnyAsync(
            content => content.Id == AboutPageContent.SingletonId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        _db.AboutPageContents.Add(CreateSeedEntity());
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        AboutPageContentEditViewModel model,
        AboutPageImageUploads uploads,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AboutPageContents.FirstOrDefaultAsync(
            content => content.Id == AboutPageContent.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.AboutPageContents.Add(entity);
        }

        async Task<(string Path, string? Error)> ResolveAsync(string current, string modelPath, IFormFile? file)
        {
            var path = string.IsNullOrWhiteSpace(modelPath) ? current : modelPath.Trim();
            var upload = await CmsAssetUpload.TrySaveAsync(
                _environment,
                AboutPageAssetRules.UploadFolderRelative,
                AboutPageAssetRules.UploadFolderWeb,
                file,
                cancellationToken);
            if (upload.Error != null)
            {
                return (path, upload.Error);
            }

            if (upload.Path != null)
            {
                path = upload.Path;
            }

            var pathError = AboutPageAssetRules.ValidateStoredPath(path);
            return pathError != null ? (path, pathError) : (path, null);
        }

        var left = await ResolveAsync(entity.ImageLeftPath, model.ImageLeftPath, uploads.ImageLeft);
        if (left.Error != null) return (false, left.Error);
        var right = await ResolveAsync(entity.ImageRightPath, model.ImageRightPath, uploads.ImageRight);
        if (right.Error != null) return (false, right.Error);
        var s1 = await ResolveAsync(entity.Showcase1ImagePath, model.Showcase1ImagePath, uploads.Showcase1Image);
        if (s1.Error != null) return (false, s1.Error);
        var s2 = await ResolveAsync(entity.Showcase2ImagePath, model.Showcase2ImagePath, uploads.Showcase2Image);
        if (s2.Error != null) return (false, s2.Error);
        var s3 = await ResolveAsync(entity.Showcase3ImagePath, model.Showcase3ImagePath, uploads.Showcase3Image);
        if (s3.Error != null) return (false, s3.Error);
        var s4 = await ResolveAsync(entity.Showcase4ImagePath, model.Showcase4ImagePath, uploads.Showcase4Image);
        if (s4.Error != null) return (false, s4.Error);

        entity.PageTitle = model.PageTitle.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.BreadcrumbTitle = model.BreadcrumbTitle.Trim();
        entity.Eyebrow = model.Eyebrow.Trim();
        entity.HeadingBefore = model.HeadingBefore.Trim();
        entity.HeadingHighlight = model.HeadingHighlight.Trim();
        entity.Body = model.Body.Trim();
        entity.ImageLeftPath = left.Path;
        entity.ImageRightPath = right.Path;
        entity.CallCtaLabel = model.CallCtaLabel.Trim();
        entity.ShowcaseEyebrow = model.ShowcaseEyebrow.Trim();
        entity.ShowcaseTitle = model.ShowcaseTitle.Trim();
        entity.ShowcaseDescription = model.ShowcaseDescription.Trim();
        entity.Showcase1Name = model.Showcase1Name.Trim();
        entity.Showcase1Descriptor = model.Showcase1Descriptor.Trim();
        entity.Showcase1ImagePath = s1.Path;
        entity.Showcase2Name = model.Showcase2Name.Trim();
        entity.Showcase2Descriptor = model.Showcase2Descriptor.Trim();
        entity.Showcase2ImagePath = s2.Path;
        entity.Showcase3Name = model.Showcase3Name.Trim();
        entity.Showcase3Descriptor = model.Showcase3Descriptor.Trim();
        entity.Showcase3ImagePath = s3.Path;
        entity.Showcase4Name = model.Showcase4Name.Trim();
        entity.Showcase4Descriptor = model.Showcase4Descriptor.Trim();
        entity.Showcase4ImagePath = s4.Path;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public static AboutPageContent CreateSeedEntity() => new()
    {
        Id = AboutPageContent.SingletonId,
        PageTitle = AboutPageContentDefaults.PageTitle,
        SeoTitle = AboutPageContentDefaults.SeoTitle,
        SeoDescription = AboutPageContentDefaults.SeoDescription,
        BreadcrumbTitle = AboutPageContentDefaults.BreadcrumbTitle,
        Eyebrow = AboutPageContentDefaults.Eyebrow,
        HeadingBefore = AboutPageContentDefaults.HeadingBefore,
        HeadingHighlight = AboutPageContentDefaults.HeadingHighlight,
        Body = AboutPageContentDefaults.Body,
        ImageLeftPath = AboutPageContentDefaults.ImageLeftPath,
        ImageRightPath = AboutPageContentDefaults.ImageRightPath,
        CallCtaLabel = AboutPageContentDefaults.CallCtaLabel,
        ShowcaseEyebrow = AboutPageContentDefaults.ShowcaseEyebrow,
        ShowcaseTitle = AboutPageContentDefaults.ShowcaseTitle,
        ShowcaseDescription = AboutPageContentDefaults.ShowcaseDescription,
        Showcase1Name = AboutPageContentDefaults.Showcase1Name,
        Showcase1Descriptor = AboutPageContentDefaults.Showcase1Descriptor,
        Showcase1ImagePath = AboutPageContentDefaults.Showcase1ImagePath,
        Showcase2Name = AboutPageContentDefaults.Showcase2Name,
        Showcase2Descriptor = AboutPageContentDefaults.Showcase2Descriptor,
        Showcase2ImagePath = AboutPageContentDefaults.Showcase2ImagePath,
        Showcase3Name = AboutPageContentDefaults.Showcase3Name,
        Showcase3Descriptor = AboutPageContentDefaults.Showcase3Descriptor,
        Showcase3ImagePath = AboutPageContentDefaults.Showcase3ImagePath,
        Showcase4Name = AboutPageContentDefaults.Showcase4Name,
        Showcase4Descriptor = AboutPageContentDefaults.Showcase4Descriptor,
        Showcase4ImagePath = AboutPageContentDefaults.Showcase4ImagePath,
        UpdatedAt = DateTime.UtcNow
    };

    private static AboutPagePublicContent Map(AboutPageContent? entity)
    {
        entity ??= CreateSeedEntity();
        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new AboutPagePublicContent
        {
            PageTitle = Text(entity.PageTitle, AboutPageContentDefaults.PageTitle),
            SeoTitle = Text(entity.SeoTitle, AboutPageContentDefaults.SeoTitle),
            SeoDescription = Text(entity.SeoDescription, AboutPageContentDefaults.SeoDescription),
            BreadcrumbTitle = Text(entity.BreadcrumbTitle, AboutPageContentDefaults.BreadcrumbTitle),
            Eyebrow = Text(entity.Eyebrow, AboutPageContentDefaults.Eyebrow),
            HeadingBefore = Text(entity.HeadingBefore, AboutPageContentDefaults.HeadingBefore),
            HeadingHighlight = Text(entity.HeadingHighlight, AboutPageContentDefaults.HeadingHighlight),
            Body = Text(entity.Body, AboutPageContentDefaults.Body),
            ImageLeftPath = AboutPageAssetRules.CoalescePath(entity.ImageLeftPath, AboutPageContentDefaults.ImageLeftPath),
            ImageRightPath = AboutPageAssetRules.CoalescePath(entity.ImageRightPath, AboutPageContentDefaults.ImageRightPath),
            CallCtaLabel = Text(entity.CallCtaLabel, AboutPageContentDefaults.CallCtaLabel),
            ShowcaseEyebrow = Text(entity.ShowcaseEyebrow, AboutPageContentDefaults.ShowcaseEyebrow),
            ShowcaseTitle = Text(entity.ShowcaseTitle, AboutPageContentDefaults.ShowcaseTitle),
            ShowcaseDescription = Text(entity.ShowcaseDescription, AboutPageContentDefaults.ShowcaseDescription),
            Showcase1Name = Text(entity.Showcase1Name, AboutPageContentDefaults.Showcase1Name),
            Showcase1Descriptor = Text(entity.Showcase1Descriptor, AboutPageContentDefaults.Showcase1Descriptor),
            Showcase1ImagePath = AboutPageAssetRules.CoalescePath(entity.Showcase1ImagePath, AboutPageContentDefaults.Showcase1ImagePath),
            Showcase2Name = Text(entity.Showcase2Name, AboutPageContentDefaults.Showcase2Name),
            Showcase2Descriptor = Text(entity.Showcase2Descriptor, AboutPageContentDefaults.Showcase2Descriptor),
            Showcase2ImagePath = AboutPageAssetRules.CoalescePath(entity.Showcase2ImagePath, AboutPageContentDefaults.Showcase2ImagePath),
            Showcase3Name = Text(entity.Showcase3Name, AboutPageContentDefaults.Showcase3Name),
            Showcase3Descriptor = Text(entity.Showcase3Descriptor, AboutPageContentDefaults.Showcase3Descriptor),
            Showcase3ImagePath = AboutPageAssetRules.CoalescePath(entity.Showcase3ImagePath, AboutPageContentDefaults.Showcase3ImagePath),
            Showcase4Name = Text(entity.Showcase4Name, AboutPageContentDefaults.Showcase4Name),
            Showcase4Descriptor = Text(entity.Showcase4Descriptor, AboutPageContentDefaults.Showcase4Descriptor),
            Showcase4ImagePath = AboutPageAssetRules.CoalescePath(entity.Showcase4ImagePath, AboutPageContentDefaults.Showcase4ImagePath)
        };
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}

public sealed class CurrentAboutPageContent : ICurrentAboutPageContent
{
    private readonly Lazy<AboutPagePublicContent> _current;

    public CurrentAboutPageContent(IAboutPageContentService aboutPage)
    {
        _current = new Lazy<AboutPagePublicContent>(aboutPage.GetPublic);
    }

    public AboutPagePublicContent Current => _current.Value;
}
