using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class ShopMarketingContentService : IShopMarketingContentService
{
    public const string CacheKey = "shop-marketing-content-public-v3";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    public ShopMarketingContentService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IWebHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    public ShopMarketingPublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out ShopMarketingPublicContent? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.ShopMarketingContents.AsNoTracking()
                .FirstOrDefault(content => content.Id == ShopMarketingContent.SingletonId);
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.ShopMarketingContents.AnyAsync(c => c.Id == ShopMarketingContent.SingletonId, cancellationToken))
        {
            return;
        }

        _db.ShopMarketingContents.Add(CreateSeedEntity());
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        ShopMarketingContentEditViewModel model,
        ShopMarketingImageUploads uploads,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ShopMarketingContents.FirstOrDefaultAsync(
            c => c.Id == ShopMarketingContent.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.ShopMarketingContents.Add(entity);
        }

        async Task<(string Path, string? Error)> ResolveAsync(string current, string modelPath, IFormFile? file)
        {
            var path = string.IsNullOrWhiteSpace(modelPath) ? current : modelPath.Trim();
            var upload = await CmsAssetUpload.TrySaveAsync(
                _environment,
                ShopMarketingAssetRules.UploadFolderRelative,
                ShopMarketingAssetRules.UploadFolderWeb,
                file,
                cancellationToken);
            if (upload.Error != null) return (path, upload.Error);
            if (upload.Path != null) path = upload.Path;
            var pathError = ShopMarketingAssetRules.ValidateStoredPath(path);
            return pathError != null ? (path, pathError) : (path, null);
        }

        var hero = await ResolveAsync(entity.HeroImagePath, model.HeroImagePath, uploads.HeroImage);
        if (hero.Error != null) return (false, hero.Error);
        var usedCars = await ResolveAsync(entity.UsedCarsPromoImagePath, model.UsedCarsPromoImagePath, uploads.UsedCarsPromoImage);
        if (usedCars.Error != null) return (false, usedCars.Error);
        var tip1 = await ResolveAsync(entity.VisitTip1ImagePath, model.VisitTip1ImagePath, uploads.VisitTip1Image);
        if (tip1.Error != null) return (false, tip1.Error);
        var tip2 = await ResolveAsync(entity.VisitTip2ImagePath, model.VisitTip2ImagePath, uploads.VisitTip2Image);
        if (tip2.Error != null) return (false, tip2.Error);

        ApplyModel(entity, model, hero.Path, usedCars.Path, tip1.Path, tip2.Path);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public static ShopMarketingContent CreateSeedEntity()
    {
        var entity = new ShopMarketingContent { Id = ShopMarketingContent.SingletonId };
        ApplyModel(
            entity,
            FromDefaults(),
            ShopMarketingContentDefaults.HeroImagePath,
            ShopMarketingContentDefaults.UsedCarsPromoImagePath,
            ShopMarketingContentDefaults.VisitTip1ImagePath,
            ShopMarketingContentDefaults.VisitTip2ImagePath);
        entity.UpdatedAt = DateTime.UtcNow;
        return entity;
    }

    private static ShopMarketingContentEditViewModel FromDefaults() => new()
    {
        HeroEyebrow = ShopMarketingContentDefaults.HeroEyebrow,
        HeroTitle = ShopMarketingContentDefaults.HeroTitle,
        HeroText = ShopMarketingContentDefaults.HeroText,
        HeroImagePath = ShopMarketingContentDefaults.HeroImagePath,
        HeroImageAlt = ShopMarketingContentDefaults.HeroImageAlt,
        SeoTitle = ShopMarketingContentDefaults.SeoTitle,
        SeoDescription = ShopMarketingContentDefaults.SeoDescription,
        CategoriesEyebrow = ShopMarketingContentDefaults.CategoriesEyebrow,
        CategoriesHeadingBefore = ShopMarketingContentDefaults.CategoriesHeadingBefore,
        CategoriesHeadingHighlight = ShopMarketingContentDefaults.CategoriesHeadingHighlight,
        CatalogEyebrow = ShopMarketingContentDefaults.CatalogEyebrow,
        CatalogHeadingBefore = ShopMarketingContentDefaults.CatalogHeadingBefore,
        CatalogHeadingHighlight = ShopMarketingContentDefaults.CatalogHeadingHighlight,
        CatalogIntro = ShopMarketingContentDefaults.CatalogIntro,
        WhyChooseEyebrow = ShopMarketingContentDefaults.WhyChooseEyebrow,
        WhyChooseHeadingBefore = ShopMarketingContentDefaults.WhyChooseHeadingBefore,
        WhyChooseHeadingHighlight = ShopMarketingContentDefaults.WhyChooseHeadingHighlight,
        Why1Title = ShopMarketingContentDefaults.Why1Title,
        Why1Description = ShopMarketingContentDefaults.Why1Description,
        Why2Title = ShopMarketingContentDefaults.Why2Title,
        Why2Description = ShopMarketingContentDefaults.Why2Description,
        Why3Title = ShopMarketingContentDefaults.Why3Title,
        Why3Description = ShopMarketingContentDefaults.Why3Description,
        Why4Title = ShopMarketingContentDefaults.Why4Title,
        Why4Description = ShopMarketingContentDefaults.Why4Description,
        VisitEyebrow = ShopMarketingContentDefaults.VisitEyebrow,
        VisitHeadingBefore = ShopMarketingContentDefaults.VisitHeadingBefore,
        VisitHeadingHighlight = ShopMarketingContentDefaults.VisitHeadingHighlight,
        VisitTip1Title = ShopMarketingContentDefaults.VisitTip1Title,
        VisitTip2Title = ShopMarketingContentDefaults.VisitTip2Title,
        FaqSectionEyebrow = ShopMarketingContentDefaults.FaqSectionEyebrow,
        FaqSectionHeadingBefore = ShopMarketingContentDefaults.FaqSectionHeadingBefore,
        FaqSectionHeadingHighlight = ShopMarketingContentDefaults.FaqSectionHeadingHighlight,
        ContactFormTitle = ShopMarketingContentDefaults.ContactFormTitle,
        BrandStripEyebrow = ShopMarketingContentDefaults.BrandStripEyebrow,
        BrandStripHeadingBefore = ShopMarketingContentDefaults.BrandStripHeadingBefore,
        BrandStripHeadingHighlight = ShopMarketingContentDefaults.BrandStripHeadingHighlight,
        UsedCarsPromoTag = ShopMarketingContentDefaults.UsedCarsPromoTag,
        UsedCarsPromoTitle = ShopMarketingContentDefaults.UsedCarsPromoTitle,
        UsedCarsPromoImagePath = ShopMarketingContentDefaults.UsedCarsPromoImagePath,
        VisitTip1Meta = ShopMarketingContentDefaults.VisitTip1Meta,
        VisitTip2Meta = ShopMarketingContentDefaults.VisitTip2Meta,
        VisitTip1ImagePath = ShopMarketingContentDefaults.VisitTip1ImagePath,
        VisitTip2ImagePath = ShopMarketingContentDefaults.VisitTip2ImagePath,
        HeroCtaBrowseParts = ShopMarketingContentDefaults.HeroCtaBrowseParts,
        HeroCtaBrowseCars = ShopMarketingContentDefaults.HeroCtaBrowseCars,
        HeroCtaVisit = ShopMarketingContentDefaults.HeroCtaVisit
    };

    private static void ApplyModel(
        ShopMarketingContent entity,
        ShopMarketingContentEditViewModel model,
        string heroPath,
        string usedCarsPath,
        string tip1Path,
        string tip2Path)
    {
        entity.HeroEyebrow = model.HeroEyebrow.Trim();
        entity.HeroTitle = model.HeroTitle.Trim();
        entity.HeroText = model.HeroText.Trim();
        entity.HeroImagePath = heroPath;
        entity.HeroImageAlt = model.HeroImageAlt.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.CategoriesEyebrow = model.CategoriesEyebrow.Trim();
        entity.CategoriesHeadingBefore = model.CategoriesHeadingBefore.Trim();
        entity.CategoriesHeadingHighlight = model.CategoriesHeadingHighlight.Trim();
        entity.CatalogEyebrow = model.CatalogEyebrow.Trim();
        entity.CatalogHeadingBefore = model.CatalogHeadingBefore.Trim();
        entity.CatalogHeadingHighlight = model.CatalogHeadingHighlight.Trim();
        entity.CatalogIntro = model.CatalogIntro.Trim();
        entity.WhyChooseEyebrow = model.WhyChooseEyebrow.Trim();
        entity.WhyChooseHeadingBefore = model.WhyChooseHeadingBefore.Trim();
        entity.WhyChooseHeadingHighlight = model.WhyChooseHeadingHighlight.Trim();
        entity.Why1Title = model.Why1Title.Trim();
        entity.Why1Description = model.Why1Description.Trim();
        entity.Why2Title = model.Why2Title.Trim();
        entity.Why2Description = model.Why2Description.Trim();
        entity.Why3Title = model.Why3Title.Trim();
        entity.Why3Description = model.Why3Description.Trim();
        entity.Why4Title = model.Why4Title.Trim();
        entity.Why4Description = model.Why4Description.Trim();
        entity.VisitEyebrow = model.VisitEyebrow.Trim();
        entity.VisitHeadingBefore = model.VisitHeadingBefore.Trim();
        entity.VisitHeadingHighlight = model.VisitHeadingHighlight.Trim();
        entity.VisitTip1Title = model.VisitTip1Title.Trim();
        entity.VisitTip2Title = model.VisitTip2Title.Trim();
        entity.FaqSectionEyebrow = model.FaqSectionEyebrow.Trim();
        entity.FaqSectionHeadingBefore = model.FaqSectionHeadingBefore.Trim();
        entity.FaqSectionHeadingHighlight = model.FaqSectionHeadingHighlight.Trim();
        entity.ContactFormTitle = model.ContactFormTitle.Trim();
        entity.BrandStripEyebrow = model.BrandStripEyebrow.Trim();
        entity.BrandStripHeadingBefore = model.BrandStripHeadingBefore.Trim();
        entity.BrandStripHeadingHighlight = model.BrandStripHeadingHighlight.Trim();
        entity.UsedCarsPromoTag = model.UsedCarsPromoTag.Trim();
        entity.UsedCarsPromoTitle = model.UsedCarsPromoTitle.Trim();
        entity.UsedCarsPromoImagePath = usedCarsPath;
        entity.VisitTip1Meta = model.VisitTip1Meta.Trim();
        entity.VisitTip2Meta = model.VisitTip2Meta.Trim();
        entity.VisitTip1ImagePath = tip1Path;
        entity.VisitTip2ImagePath = tip2Path;
        entity.HeroCtaBrowseParts = model.HeroCtaBrowseParts.Trim();
        entity.HeroCtaBrowseCars = model.HeroCtaBrowseCars.Trim();
        entity.HeroCtaVisit = model.HeroCtaVisit.Trim();
    }

    private static ShopMarketingPublicContent Map(ShopMarketingContent? entity)
    {
        entity ??= CreateSeedEntity();
        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new ShopMarketingPublicContent
        {
            HeroEyebrow = Text(entity.HeroEyebrow, ShopMarketingContentDefaults.HeroEyebrow),
            HeroTitle = Text(entity.HeroTitle, ShopMarketingContentDefaults.HeroTitle),
            HeroText = Text(entity.HeroText, ShopMarketingContentDefaults.HeroText),
            HeroImagePath = ShopMarketingAssetRules.CoalescePath(entity.HeroImagePath, ShopMarketingContentDefaults.HeroImagePath),
            HeroImageAlt = Text(entity.HeroImageAlt, ShopMarketingContentDefaults.HeroImageAlt),
            SeoTitle = Text(entity.SeoTitle, ShopMarketingContentDefaults.SeoTitle),
            SeoDescription = Text(entity.SeoDescription, ShopMarketingContentDefaults.SeoDescription),
            CategoriesEyebrow = Text(entity.CategoriesEyebrow, ShopMarketingContentDefaults.CategoriesEyebrow),
            CategoriesHeadingBefore = Text(entity.CategoriesHeadingBefore, ShopMarketingContentDefaults.CategoriesHeadingBefore),
            CategoriesHeadingHighlight = Text(entity.CategoriesHeadingHighlight, ShopMarketingContentDefaults.CategoriesHeadingHighlight),
            CatalogEyebrow = Text(entity.CatalogEyebrow, ShopMarketingContentDefaults.CatalogEyebrow),
            CatalogHeadingBefore = Text(entity.CatalogHeadingBefore, ShopMarketingContentDefaults.CatalogHeadingBefore),
            CatalogHeadingHighlight = Text(entity.CatalogHeadingHighlight, ShopMarketingContentDefaults.CatalogHeadingHighlight),
            CatalogIntro = Text(entity.CatalogIntro, ShopMarketingContentDefaults.CatalogIntro),
            WhyChooseEyebrow = Text(entity.WhyChooseEyebrow, ShopMarketingContentDefaults.WhyChooseEyebrow),
            WhyChooseHeadingBefore = Text(entity.WhyChooseHeadingBefore, ShopMarketingContentDefaults.WhyChooseHeadingBefore),
            WhyChooseHeadingHighlight = Text(entity.WhyChooseHeadingHighlight, ShopMarketingContentDefaults.WhyChooseHeadingHighlight),
            Why1Title = Text(entity.Why1Title, ShopMarketingContentDefaults.Why1Title),
            Why1Description = Text(entity.Why1Description, ShopMarketingContentDefaults.Why1Description),
            Why2Title = Text(entity.Why2Title, ShopMarketingContentDefaults.Why2Title),
            Why2Description = Text(entity.Why2Description, ShopMarketingContentDefaults.Why2Description),
            Why3Title = Text(entity.Why3Title, ShopMarketingContentDefaults.Why3Title),
            Why3Description = Text(entity.Why3Description, ShopMarketingContentDefaults.Why3Description),
            Why4Title = Text(entity.Why4Title, ShopMarketingContentDefaults.Why4Title),
            Why4Description = Text(entity.Why4Description, ShopMarketingContentDefaults.Why4Description),
            VisitEyebrow = Text(entity.VisitEyebrow, ShopMarketingContentDefaults.VisitEyebrow),
            VisitHeadingBefore = Text(entity.VisitHeadingBefore, ShopMarketingContentDefaults.VisitHeadingBefore),
            VisitHeadingHighlight = Text(entity.VisitHeadingHighlight, ShopMarketingContentDefaults.VisitHeadingHighlight),
            VisitTip1Title = Text(entity.VisitTip1Title, ShopMarketingContentDefaults.VisitTip1Title),
            VisitTip2Title = Text(entity.VisitTip2Title, ShopMarketingContentDefaults.VisitTip2Title),
            FaqSectionEyebrow = Text(entity.FaqSectionEyebrow, ShopMarketingContentDefaults.FaqSectionEyebrow),
            FaqSectionHeadingBefore = Text(entity.FaqSectionHeadingBefore, ShopMarketingContentDefaults.FaqSectionHeadingBefore),
            FaqSectionHeadingHighlight = Text(entity.FaqSectionHeadingHighlight, ShopMarketingContentDefaults.FaqSectionHeadingHighlight),
            ContactFormTitle = Text(entity.ContactFormTitle, ShopMarketingContentDefaults.ContactFormTitle),
            BrandStripEyebrow = Text(entity.BrandStripEyebrow, ShopMarketingContentDefaults.BrandStripEyebrow),
            BrandStripHeadingBefore = Text(entity.BrandStripHeadingBefore, ShopMarketingContentDefaults.BrandStripHeadingBefore),
            BrandStripHeadingHighlight = Text(entity.BrandStripHeadingHighlight, ShopMarketingContentDefaults.BrandStripHeadingHighlight),
            UsedCarsPromoTag = Text(entity.UsedCarsPromoTag, ShopMarketingContentDefaults.UsedCarsPromoTag),
            UsedCarsPromoTitle = Text(entity.UsedCarsPromoTitle, ShopMarketingContentDefaults.UsedCarsPromoTitle),
            UsedCarsPromoImagePath = ShopMarketingAssetRules.CoalescePath(entity.UsedCarsPromoImagePath, ShopMarketingContentDefaults.UsedCarsPromoImagePath),
            VisitTip1Meta = Text(entity.VisitTip1Meta, ShopMarketingContentDefaults.VisitTip1Meta),
            VisitTip2Meta = Text(entity.VisitTip2Meta, ShopMarketingContentDefaults.VisitTip2Meta),
            VisitTip1ImagePath = ShopMarketingAssetRules.CoalescePath(entity.VisitTip1ImagePath, ShopMarketingContentDefaults.VisitTip1ImagePath),
            VisitTip2ImagePath = ShopMarketingAssetRules.CoalescePath(entity.VisitTip2ImagePath, ShopMarketingContentDefaults.VisitTip2ImagePath),
            HeroCtaBrowseParts = Text(entity.HeroCtaBrowseParts, ShopMarketingContentDefaults.HeroCtaBrowseParts),
            HeroCtaBrowseCars = Text(entity.HeroCtaBrowseCars, ShopMarketingContentDefaults.HeroCtaBrowseCars),
            HeroCtaVisit = Text(entity.HeroCtaVisit, ShopMarketingContentDefaults.HeroCtaVisit)
        };
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}

public sealed class CurrentShopMarketingContent : ICurrentShopMarketingContent
{
    private readonly Lazy<ShopMarketingPublicContent> _current;

    public CurrentShopMarketingContent(IShopMarketingContentService shopMarketing)
    {
        _current = new Lazy<ShopMarketingPublicContent>(shopMarketing.GetPublic);
    }

    public ShopMarketingPublicContent Current => _current.Value;
}
