using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class HomePageContentService : IHomePageContentService
{
    public const string CacheKey = "home-page-content-public-v1";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    public HomePageContentService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IWebHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    public HomePagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out HomePagePublicContent? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.HomePageContents.AsNoTracking()
                .FirstOrDefault(content => content.Id == HomePageContent.SingletonId);
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _db.HomePageContents.AnyAsync(
            content => content.Id == HomePageContent.SingletonId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        _db.HomePageContents.Add(CreateSeedEntity());
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        HomePageContentEditViewModel model,
        HomePageImageUploads uploads,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.HomePageContents.FirstOrDefaultAsync(
            content => content.Id == HomePageContent.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.HomePageContents.Add(entity);
        }

        async Task<(string Path, string? Error)> ResolvePathAsync(
            string current,
            string modelPath,
            IFormFile? file)
        {
            var path = string.IsNullOrWhiteSpace(modelPath) ? current : modelPath.Trim();
            var upload = await TrySaveAsync(file, cancellationToken);
            if (upload.Error != null)
            {
                return (path, upload.Error);
            }

            if (upload.Path != null)
            {
                path = upload.Path;
            }

            var pathError = HomePageAssetRules.ValidateStoredPath(path);
            return pathError != null ? (path, pathError) : (path, null);
        }

        var hero = await ResolvePathAsync(entity.HeroImagePath, model.HeroImagePath, uploads.HeroImage);
        if (hero.Error != null) return (false, hero.Error);
        var aboutMain = await ResolvePathAsync(entity.AboutImageMainPath, model.AboutImageMainPath, uploads.AboutImageMain);
        if (aboutMain.Error != null) return (false, aboutMain.Error);
        var aboutFloat1 = await ResolvePathAsync(entity.AboutImageFloat1Path, model.AboutImageFloat1Path, uploads.AboutImageFloat1);
        if (aboutFloat1.Error != null) return (false, aboutFloat1.Error);
        var aboutFloat2 = await ResolvePathAsync(entity.AboutImageFloat2Path, model.AboutImageFloat2Path, uploads.AboutImageFloat2);
        if (aboutFloat2.Error != null) return (false, aboutFloat2.Error);
        var whyBg = await ResolvePathAsync(entity.WhyChooseBackgroundPath, model.WhyChooseBackgroundPath, uploads.WhyChooseBackground);
        if (whyBg.Error != null) return (false, whyBg.Error);
        var f1 = await ResolvePathAsync(entity.Feature1IconPath, model.Feature1IconPath, uploads.Feature1Icon);
        if (f1.Error != null) return (false, f1.Error);
        var f2 = await ResolvePathAsync(entity.Feature2IconPath, model.Feature2IconPath, uploads.Feature2Icon);
        if (f2.Error != null) return (false, f2.Error);
        var f3 = await ResolvePathAsync(entity.Feature3IconPath, model.Feature3IconPath, uploads.Feature3Icon);
        if (f3.Error != null) return (false, f3.Error);
        var f4 = await ResolvePathAsync(entity.Feature4IconPath, model.Feature4IconPath, uploads.Feature4Icon);
        if (f4.Error != null) return (false, f4.Error);

        entity.HeroTitle = model.HeroTitle.Trim();
        entity.HeroHighlight = model.HeroHighlight.Trim();
        entity.HeroImagePath = hero.Path;
        entity.HeroImageAlt = model.HeroImageAlt.Trim();
        entity.CategoriesEyebrow = model.CategoriesEyebrow.Trim();
        entity.CategoriesHeadingBefore = model.CategoriesHeadingBefore.Trim();
        entity.CategoriesHeadingHighlight = model.CategoriesHeadingHighlight.Trim();
        entity.CategoriesHeadingAfter = model.CategoriesHeadingAfter?.Trim() ?? string.Empty;
        entity.PortfolioEyebrow = model.PortfolioEyebrow.Trim();
        entity.PortfolioHeadingBefore = model.PortfolioHeadingBefore.Trim();
        entity.PortfolioHeadingHighlight = model.PortfolioHeadingHighlight.Trim();
        entity.CampaignsEyebrow = model.CampaignsEyebrow.Trim();
        entity.CampaignsHeadingBefore = model.CampaignsHeadingBefore.Trim();
        entity.CampaignsHeadingHighlight = model.CampaignsHeadingHighlight.Trim();
        entity.AboutEyebrow = model.AboutEyebrow.Trim();
        entity.AboutHeadingBefore = model.AboutHeadingBefore.Trim();
        entity.AboutHeadingHighlight = model.AboutHeadingHighlight.Trim();
        entity.AboutBody = model.AboutBody.Trim();
        entity.AboutCtaLabel = model.AboutCtaLabel.Trim();
        entity.AboutImageMainPath = aboutMain.Path;
        entity.AboutImageFloat1Path = aboutFloat1.Path;
        entity.AboutImageFloat2Path = aboutFloat2.Path;
        entity.BrandsEyebrow = model.BrandsEyebrow.Trim();
        entity.BrandsHeadingBefore = model.BrandsHeadingBefore.Trim();
        entity.BrandsHeadingHighlight = model.BrandsHeadingHighlight.Trim();
        entity.WhyChooseEyebrow = model.WhyChooseEyebrow.Trim();
        entity.WhyChooseHeadingBefore = model.WhyChooseHeadingBefore.Trim();
        entity.WhyChooseHeadingHighlight = model.WhyChooseHeadingHighlight.Trim();
        entity.WhyChooseBackgroundPath = whyBg.Path;
        entity.Feature1Title = model.Feature1Title.Trim();
        entity.Feature1Description = model.Feature1Description.Trim();
        entity.Feature1IconPath = f1.Path;
        entity.Feature2Title = model.Feature2Title.Trim();
        entity.Feature2Description = model.Feature2Description.Trim();
        entity.Feature2IconPath = f2.Path;
        entity.Feature3Title = model.Feature3Title.Trim();
        entity.Feature3Description = model.Feature3Description.Trim();
        entity.Feature3IconPath = f3.Path;
        entity.Feature4Title = model.Feature4Title.Trim();
        entity.Feature4Description = model.Feature4Description.Trim();
        entity.Feature4IconPath = f4.Path;
        entity.FeaturedEyebrow = model.FeaturedEyebrow.Trim();
        entity.FeaturedHeadingBefore = model.FeaturedHeadingBefore.Trim();
        entity.FeaturedHeadingHighlight = model.FeaturedHeadingHighlight.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public static HomePageContent CreateSeedEntity() => new()
    {
        Id = HomePageContent.SingletonId,
        HeroTitle = HomePageContentDefaults.HeroTitle,
        HeroHighlight = HomePageContentDefaults.HeroHighlight,
        HeroImagePath = HomePageContentDefaults.HeroImagePath,
        HeroImageAlt = HomePageContentDefaults.HeroImageAlt,
        CategoriesEyebrow = HomePageContentDefaults.CategoriesEyebrow,
        CategoriesHeadingBefore = HomePageContentDefaults.CategoriesHeadingBefore,
        CategoriesHeadingHighlight = HomePageContentDefaults.CategoriesHeadingHighlight,
        CategoriesHeadingAfter = HomePageContentDefaults.CategoriesHeadingAfter,
        PortfolioEyebrow = HomePageContentDefaults.PortfolioEyebrow,
        PortfolioHeadingBefore = HomePageContentDefaults.PortfolioHeadingBefore,
        PortfolioHeadingHighlight = HomePageContentDefaults.PortfolioHeadingHighlight,
        CampaignsEyebrow = HomePageContentDefaults.CampaignsEyebrow,
        CampaignsHeadingBefore = HomePageContentDefaults.CampaignsHeadingBefore,
        CampaignsHeadingHighlight = HomePageContentDefaults.CampaignsHeadingHighlight,
        AboutEyebrow = HomePageContentDefaults.AboutEyebrow,
        AboutHeadingBefore = HomePageContentDefaults.AboutHeadingBefore,
        AboutHeadingHighlight = HomePageContentDefaults.AboutHeadingHighlight,
        AboutBody = HomePageContentDefaults.AboutBody,
        AboutCtaLabel = HomePageContentDefaults.AboutCtaLabel,
        AboutImageMainPath = HomePageContentDefaults.AboutImageMainPath,
        AboutImageFloat1Path = HomePageContentDefaults.AboutImageFloat1Path,
        AboutImageFloat2Path = HomePageContentDefaults.AboutImageFloat2Path,
        BrandsEyebrow = HomePageContentDefaults.BrandsEyebrow,
        BrandsHeadingBefore = HomePageContentDefaults.BrandsHeadingBefore,
        BrandsHeadingHighlight = HomePageContentDefaults.BrandsHeadingHighlight,
        WhyChooseEyebrow = HomePageContentDefaults.WhyChooseEyebrow,
        WhyChooseHeadingBefore = HomePageContentDefaults.WhyChooseHeadingBefore,
        WhyChooseHeadingHighlight = HomePageContentDefaults.WhyChooseHeadingHighlight,
        WhyChooseBackgroundPath = HomePageContentDefaults.WhyChooseBackgroundPath,
        Feature1Title = HomePageContentDefaults.Feature1Title,
        Feature1Description = HomePageContentDefaults.FeatureDescription,
        Feature1IconPath = HomePageContentDefaults.Feature1IconPath,
        Feature2Title = HomePageContentDefaults.Feature2Title,
        Feature2Description = HomePageContentDefaults.FeatureDescription,
        Feature2IconPath = HomePageContentDefaults.Feature2IconPath,
        Feature3Title = HomePageContentDefaults.Feature3Title,
        Feature3Description = HomePageContentDefaults.FeatureDescription,
        Feature3IconPath = HomePageContentDefaults.Feature3IconPath,
        Feature4Title = HomePageContentDefaults.Feature4Title,
        Feature4Description = HomePageContentDefaults.FeatureDescription,
        Feature4IconPath = HomePageContentDefaults.Feature4IconPath,
        FeaturedEyebrow = HomePageContentDefaults.FeaturedEyebrow,
        FeaturedHeadingBefore = HomePageContentDefaults.FeaturedHeadingBefore,
        FeaturedHeadingHighlight = HomePageContentDefaults.FeaturedHeadingHighlight,
        UpdatedAt = DateTime.UtcNow
    };

    private static HomePagePublicContent Map(HomePageContent? entity)
    {
        entity ??= CreateSeedEntity();
        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new HomePagePublicContent
        {
            HeroTitle = Text(entity.HeroTitle, HomePageContentDefaults.HeroTitle),
            HeroHighlight = Text(entity.HeroHighlight, HomePageContentDefaults.HeroHighlight),
            HeroImagePath = HomePageAssetRules.CoalescePath(entity.HeroImagePath, HomePageContentDefaults.HeroImagePath),
            HeroImageAlt = Text(entity.HeroImageAlt, HomePageContentDefaults.HeroImageAlt),
            CategoriesEyebrow = Text(entity.CategoriesEyebrow, HomePageContentDefaults.CategoriesEyebrow),
            CategoriesHeadingBefore = Text(entity.CategoriesHeadingBefore, HomePageContentDefaults.CategoriesHeadingBefore),
            CategoriesHeadingHighlight = Text(entity.CategoriesHeadingHighlight, HomePageContentDefaults.CategoriesHeadingHighlight),
            CategoriesHeadingAfter = entity.CategoriesHeadingAfter?.Trim() ?? HomePageContentDefaults.CategoriesHeadingAfter,
            PortfolioEyebrow = Text(entity.PortfolioEyebrow, HomePageContentDefaults.PortfolioEyebrow),
            PortfolioHeadingBefore = Text(entity.PortfolioHeadingBefore, HomePageContentDefaults.PortfolioHeadingBefore),
            PortfolioHeadingHighlight = Text(entity.PortfolioHeadingHighlight, HomePageContentDefaults.PortfolioHeadingHighlight),
            CampaignsEyebrow = Text(entity.CampaignsEyebrow, HomePageContentDefaults.CampaignsEyebrow),
            CampaignsHeadingBefore = Text(entity.CampaignsHeadingBefore, HomePageContentDefaults.CampaignsHeadingBefore),
            CampaignsHeadingHighlight = Text(entity.CampaignsHeadingHighlight, HomePageContentDefaults.CampaignsHeadingHighlight),
            AboutEyebrow = Text(entity.AboutEyebrow, HomePageContentDefaults.AboutEyebrow),
            AboutHeadingBefore = Text(entity.AboutHeadingBefore, HomePageContentDefaults.AboutHeadingBefore),
            AboutHeadingHighlight = Text(entity.AboutHeadingHighlight, HomePageContentDefaults.AboutHeadingHighlight),
            AboutBody = Text(entity.AboutBody, HomePageContentDefaults.AboutBody),
            AboutCtaLabel = Text(entity.AboutCtaLabel, HomePageContentDefaults.AboutCtaLabel),
            AboutImageMainPath = HomePageAssetRules.CoalescePath(entity.AboutImageMainPath, HomePageContentDefaults.AboutImageMainPath),
            AboutImageFloat1Path = HomePageAssetRules.CoalescePath(entity.AboutImageFloat1Path, HomePageContentDefaults.AboutImageFloat1Path),
            AboutImageFloat2Path = HomePageAssetRules.CoalescePath(entity.AboutImageFloat2Path, HomePageContentDefaults.AboutImageFloat2Path),
            BrandsEyebrow = Text(entity.BrandsEyebrow, HomePageContentDefaults.BrandsEyebrow),
            BrandsHeadingBefore = Text(entity.BrandsHeadingBefore, HomePageContentDefaults.BrandsHeadingBefore),
            BrandsHeadingHighlight = Text(entity.BrandsHeadingHighlight, HomePageContentDefaults.BrandsHeadingHighlight),
            WhyChooseEyebrow = Text(entity.WhyChooseEyebrow, HomePageContentDefaults.WhyChooseEyebrow),
            WhyChooseHeadingBefore = Text(entity.WhyChooseHeadingBefore, HomePageContentDefaults.WhyChooseHeadingBefore),
            WhyChooseHeadingHighlight = Text(entity.WhyChooseHeadingHighlight, HomePageContentDefaults.WhyChooseHeadingHighlight),
            WhyChooseBackgroundPath = HomePageAssetRules.CoalescePath(entity.WhyChooseBackgroundPath, HomePageContentDefaults.WhyChooseBackgroundPath),
            Feature1Title = Text(entity.Feature1Title, HomePageContentDefaults.Feature1Title),
            Feature1Description = Text(entity.Feature1Description, HomePageContentDefaults.FeatureDescription),
            Feature1IconPath = HomePageAssetRules.CoalescePath(entity.Feature1IconPath, HomePageContentDefaults.Feature1IconPath),
            Feature2Title = Text(entity.Feature2Title, HomePageContentDefaults.Feature2Title),
            Feature2Description = Text(entity.Feature2Description, HomePageContentDefaults.FeatureDescription),
            Feature2IconPath = HomePageAssetRules.CoalescePath(entity.Feature2IconPath, HomePageContentDefaults.Feature2IconPath),
            Feature3Title = Text(entity.Feature3Title, HomePageContentDefaults.Feature3Title),
            Feature3Description = Text(entity.Feature3Description, HomePageContentDefaults.FeatureDescription),
            Feature3IconPath = HomePageAssetRules.CoalescePath(entity.Feature3IconPath, HomePageContentDefaults.Feature3IconPath),
            Feature4Title = Text(entity.Feature4Title, HomePageContentDefaults.Feature4Title),
            Feature4Description = Text(entity.Feature4Description, HomePageContentDefaults.FeatureDescription),
            Feature4IconPath = HomePageAssetRules.CoalescePath(entity.Feature4IconPath, HomePageContentDefaults.Feature4IconPath),
            FeaturedEyebrow = Text(entity.FeaturedEyebrow, HomePageContentDefaults.FeaturedEyebrow),
            FeaturedHeadingBefore = Text(entity.FeaturedHeadingBefore, HomePageContentDefaults.FeaturedHeadingBefore),
            FeaturedHeadingHighlight = Text(entity.FeaturedHeadingHighlight, HomePageContentDefaults.FeaturedHeadingHighlight)
        };
    }

    private async Task<(string? Path, string? Error)> TrySaveAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return (null, null);
        }

        var validation = HomePageAssetRules.ValidateUpload(file.FileName, file.ContentType, file.Length);
        if (validation != null)
        {
            return (null, validation);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(_environment.WebRootPath, HomePageAssetRules.UploadFolderRelative);
        Directory.CreateDirectory(folder);
        var physical = Path.Combine(folder, fileName);
        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return ($"{HomePageAssetRules.UploadFolderWeb}/{fileName}", null);
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}

public sealed class CurrentHomePageContent : ICurrentHomePageContent
{
    private readonly Lazy<HomePagePublicContent> _current;

    public CurrentHomePageContent(IHomePageContentService homePage)
    {
        _current = new Lazy<HomePagePublicContent>(homePage.GetPublic);
    }

    public HomePagePublicContent Current => _current.Value;
}
