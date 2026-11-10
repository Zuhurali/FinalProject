using FinalProject.MVC.Commerce;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class FaqPageContentService : IFaqPageContentService
{
    public const string CacheKey = "faq-page-content-v1";
    private readonly IApplicationRepository _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    public FaqPageContentService(IApplicationRepository db, IMemoryCache cache, IWebHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    public FaqPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out FaqPagePublicContent? cached) && cached != null) return cached;
        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null) return cached;
            var entity = _db.GetFaqPageContentSingletonReadOnly();
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyFaqPageContentSingletonAsync(cancellationToken)) return;
        _db.AddFaqPageContent(CreateSeed());
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        FaqPageContentEditViewModel model,
        IFormFile? banner,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedFaqPageContentSingletonAsync(cancellationToken);
        if (entity == null)
        {
            entity = CreateSeed();
            _db.AddFaqPageContent(entity);
        }

        var path = string.IsNullOrWhiteSpace(model.BannerImagePath) ? entity.BannerImagePath : model.BannerImagePath.Trim();
        var upload = await CmsAssetUpload.TrySaveAsync(
            _environment, FaqPageAssetRules.UploadFolderRelative, FaqPageAssetRules.UploadFolderWeb, banner, cancellationToken);
        if (upload.Error != null) return (false, upload.Error);
        if (upload.Path != null) path = upload.Path;
        var pathError = FaqPageAssetRules.ValidateStoredPath(path);
        if (pathError != null) return (false, pathError);

        entity.BreadcrumbTitle = model.BreadcrumbTitle.Trim();
        entity.Eyebrow = model.Eyebrow.Trim();
        entity.HeadingBefore = model.HeadingBefore.Trim();
        entity.HeadingHighlight = model.HeadingHighlight.Trim();
        entity.BannerImagePath = path;
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
        return (true, null);
    }

    public static FaqPageContent CreateSeed() => new()
    {
        Id = FaqPageContent.SingletonId,
        BreadcrumbTitle = FaqPageContentDefaults.BreadcrumbTitle,
        Eyebrow = FaqPageContentDefaults.Eyebrow,
        HeadingBefore = FaqPageContentDefaults.HeadingBefore,
        HeadingHighlight = FaqPageContentDefaults.HeadingHighlight,
        BannerImagePath = FaqPageContentDefaults.BannerImagePath,
        SeoTitle = FaqPageContentDefaults.SeoTitle,
        SeoDescription = FaqPageContentDefaults.SeoDescription,
        UpdatedAt = DateTime.UtcNow
    };

    private static FaqPagePublicContent Map(FaqPageContent? entity)
    {
        entity ??= CreateSeed();
        static string T(string? v, string f) => string.IsNullOrWhiteSpace(v) ? f : v.Trim();
        return new FaqPagePublicContent
        {
            BreadcrumbTitle = T(entity.BreadcrumbTitle, FaqPageContentDefaults.BreadcrumbTitle),
            Eyebrow = T(entity.Eyebrow, FaqPageContentDefaults.Eyebrow),
            HeadingBefore = T(entity.HeadingBefore, FaqPageContentDefaults.HeadingBefore),
            HeadingHighlight = T(entity.HeadingHighlight, FaqPageContentDefaults.HeadingHighlight),
            BannerImagePath = FaqPageAssetRules.CoalescePath(entity.BannerImagePath, FaqPageContentDefaults.BannerImagePath),
            SeoTitle = T(entity.SeoTitle, FaqPageContentDefaults.SeoTitle),
            SeoDescription = T(entity.SeoDescription, FaqPageContentDefaults.SeoDescription)
        };
    }
}

public sealed class CurrentFaqPageContent : ICurrentFaqPageContent
{
    private readonly Lazy<FaqPagePublicContent> _current;
    public CurrentFaqPageContent(IFaqPageContentService service) => _current = new(service.GetPublic);
    public FaqPagePublicContent Current => _current.Value;
}

public sealed class TeamPageContentService : ITeamPageContentService
{
    public const string CacheKey = "team-page-content-v1";
    private readonly IApplicationRepository _db;
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public TeamPageContentService(IApplicationRepository db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public TeamPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out TeamPagePublicContent? cached) && cached != null) return cached;
        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null) return cached;
            var entity = _db.GetTeamPageContentSingletonReadOnly();
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyTeamPageContentSingletonAsync(cancellationToken)) return;
        _db.AddTeamPageContent(CreateSeed());
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(TeamPageContentEditViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedTeamPageContentSingletonAsync(cancellationToken);
        if (entity == null)
        {
            entity = CreateSeed();
            _db.AddTeamPageContent(entity);
        }

        entity.BreadcrumbTitle = model.BreadcrumbTitle.Trim();
        entity.Eyebrow = model.Eyebrow.Trim();
        entity.HeadingBefore = model.HeadingBefore.Trim();
        entity.HeadingHighlight = model.HeadingHighlight.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
        return (true, null);
    }

    public static TeamPageContent CreateSeed() => new()
    {
        Id = TeamPageContent.SingletonId,
        BreadcrumbTitle = TeamPageContentDefaults.BreadcrumbTitle,
        Eyebrow = TeamPageContentDefaults.Eyebrow,
        HeadingBefore = TeamPageContentDefaults.HeadingBefore,
        HeadingHighlight = TeamPageContentDefaults.HeadingHighlight,
        SeoTitle = TeamPageContentDefaults.SeoTitle,
        SeoDescription = TeamPageContentDefaults.SeoDescription,
        UpdatedAt = DateTime.UtcNow
    };

    private static TeamPagePublicContent Map(TeamPageContent? entity)
    {
        entity ??= CreateSeed();
        static string T(string? v, string f) => string.IsNullOrWhiteSpace(v) ? f : v.Trim();
        return new TeamPagePublicContent
        {
            BreadcrumbTitle = T(entity.BreadcrumbTitle, TeamPageContentDefaults.BreadcrumbTitle),
            Eyebrow = T(entity.Eyebrow, TeamPageContentDefaults.Eyebrow),
            HeadingBefore = T(entity.HeadingBefore, TeamPageContentDefaults.HeadingBefore),
            HeadingHighlight = T(entity.HeadingHighlight, TeamPageContentDefaults.HeadingHighlight),
            SeoTitle = T(entity.SeoTitle, TeamPageContentDefaults.SeoTitle),
            SeoDescription = T(entity.SeoDescription, TeamPageContentDefaults.SeoDescription)
        };
    }
}

public sealed class CurrentTeamPageContent : ICurrentTeamPageContent
{
    private readonly Lazy<TeamPagePublicContent> _current;
    public CurrentTeamPageContent(ITeamPageContentService service) => _current = new(service.GetPublic);
    public TeamPagePublicContent Current => _current.Value;
}

public sealed class DealerPageContentService : IDealerPageContentService
{
    public const string CacheKey = "dealer-page-content-v1";
    private readonly IApplicationRepository _db;
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public DealerPageContentService(IApplicationRepository db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public DealerPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out DealerPagePublicContent? cached) && cached != null) return cached;
        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null) return cached;
            var entity = _db.GetDealerPageContentSingletonReadOnly();
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyDealerPageContentSingletonAsync(cancellationToken)) return;
        _db.AddDealerPageContent(CreateSeed());
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(DealerPageContentEditViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedDealerPageContentSingletonAsync(cancellationToken);
        if (entity == null)
        {
            entity = CreateSeed();
            _db.AddDealerPageContent(entity);
        }

        entity.BreadcrumbTitle = model.BreadcrumbTitle.Trim();
        entity.PageTitle = model.PageTitle.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
        return (true, null);
    }

    public static DealerPageContent CreateSeed() => new()
    {
        Id = DealerPageContent.SingletonId,
        BreadcrumbTitle = DealerPageContentDefaults.BreadcrumbTitle,
        PageTitle = DealerPageContentDefaults.PageTitle,
        SeoTitle = DealerPageContentDefaults.SeoTitle,
        SeoDescription = DealerPageContentDefaults.SeoDescription,
        UpdatedAt = DateTime.UtcNow
    };

    private static DealerPagePublicContent Map(DealerPageContent? entity)
    {
        entity ??= CreateSeed();
        static string T(string? v, string f) => string.IsNullOrWhiteSpace(v) ? f : v.Trim();
        return new DealerPagePublicContent
        {
            BreadcrumbTitle = T(entity.BreadcrumbTitle, DealerPageContentDefaults.BreadcrumbTitle),
            PageTitle = T(entity.PageTitle, DealerPageContentDefaults.PageTitle),
            SeoTitle = T(entity.SeoTitle, DealerPageContentDefaults.SeoTitle),
            SeoDescription = T(entity.SeoDescription, DealerPageContentDefaults.SeoDescription)
        };
    }
}

public sealed class CurrentDealerPageContent : ICurrentDealerPageContent
{
    private readonly Lazy<DealerPagePublicContent> _current;
    public CurrentDealerPageContent(IDealerPageContentService service) => _current = new(service.GetPublic);
    public DealerPagePublicContent Current => _current.Value;
}

public sealed class CalculatorPageContentService : ICalculatorPageContentService
{
    public const string CacheKey = "calculator-page-content-v1";
    private readonly IApplicationRepository _db;
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public CalculatorPageContentService(IApplicationRepository db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public CalculatorPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out CalculatorPagePublicContent? cached) && cached != null) return cached;
        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null) return cached;
            var entity = _db.GetCalculatorPageContentSingletonReadOnly();
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyCalculatorPageContentSingletonAsync(cancellationToken)) return;
        _db.AddCalculatorPageContent(CreateSeed());
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(CalculatorPageContentEditViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedCalculatorPageContentSingletonAsync(cancellationToken);
        if (entity == null)
        {
            entity = CreateSeed();
            _db.AddCalculatorPageContent(entity);
        }

        entity.BreadcrumbTitle = model.BreadcrumbTitle.Trim();
        entity.PageTitle = model.PageTitle.Trim();
        entity.IntroText = model.IntroText.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
        return (true, null);
    }

    public static CalculatorPageContent CreateSeed() => new()
    {
        Id = CalculatorPageContent.SingletonId,
        BreadcrumbTitle = CalculatorPageContentDefaults.BreadcrumbTitle,
        PageTitle = CalculatorPageContentDefaults.PageTitle,
        IntroText = CalculatorPageContentDefaults.IntroText,
        SeoTitle = CalculatorPageContentDefaults.SeoTitle,
        SeoDescription = CalculatorPageContentDefaults.SeoDescription,
        UpdatedAt = DateTime.UtcNow
    };

    private static CalculatorPagePublicContent Map(CalculatorPageContent? entity)
    {
        entity ??= CreateSeed();
        static string T(string? v, string f) => string.IsNullOrWhiteSpace(v) ? f : v.Trim();
        return new CalculatorPagePublicContent
        {
            BreadcrumbTitle = T(entity.BreadcrumbTitle, CalculatorPageContentDefaults.BreadcrumbTitle),
            PageTitle = T(entity.PageTitle, CalculatorPageContentDefaults.PageTitle),
            IntroText = T(entity.IntroText, CalculatorPageContentDefaults.IntroText),
            SeoTitle = T(entity.SeoTitle, CalculatorPageContentDefaults.SeoTitle),
            SeoDescription = T(entity.SeoDescription, CalculatorPageContentDefaults.SeoDescription)
        };
    }
}

public sealed class CurrentCalculatorPageContent : ICurrentCalculatorPageContent
{
    private readonly Lazy<CalculatorPagePublicContent> _current;
    public CurrentCalculatorPageContent(ICalculatorPageContentService service) => _current = new(service.GetPublic);
    public CalculatorPagePublicContent Current => _current.Value;
}
