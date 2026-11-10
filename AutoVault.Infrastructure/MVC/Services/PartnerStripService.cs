using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class PartnerStripService : IPartnerStripService
{
    public const string SettingsCacheKey = "partner-strip-settings-v1";
    public const string ItemsCacheKey = "partner-strip-items-v1";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    public PartnerStripService(ApplicationDbContext db, IMemoryCache cache, IWebHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    public PartnerStripPublicSettings GetSettings()
    {
        if (_cache.TryGetValue(SettingsCacheKey, out PartnerStripPublicSettings? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(SettingsCacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.PartnerStripSettings.AsNoTracking()
                .FirstOrDefault(s => s.Id == PartnerStripSettings.SingletonId);
            var mapped = MapSettings(entity);
            _cache.Set(SettingsCacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public IReadOnlyList<PartnerStripPublicItem> GetPublishedItems()
    {
        if (_cache.TryGetValue(ItemsCacheKey, out IReadOnlyList<PartnerStripPublicItem>? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(ItemsCacheKey, out cached) && cached != null)
            {
                return cached;
            }

            // Empty published set is intentional (admin unpublished/deleted all) — do not revive seed logos.
            var items = _db.PartnerStripItems.AsNoTracking()
                .Where(item => item.IsPublished)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id)
                .ToList()
                .Select(item => new PartnerStripPublicItem
                {
                    Id = item.Id,
                    Name = string.IsNullOrWhiteSpace(item.Name) ? "Partner" : item.Name.Trim(),
                    LogoPath = PartnerStripAssetRules.CoalescePath(item.LogoPath, PartnerStripDefaults.Items[0].LogoPath),
                    LinkUrl = string.IsNullOrWhiteSpace(item.LinkUrl) ? null : item.LinkUrl.Trim()
                })
                .ToList();

            _cache.Set(ItemsCacheKey, (IReadOnlyList<PartnerStripPublicItem>)items, TimeSpan.FromMinutes(10));
            return items;
        }
    }

    public string? FindLogoPathByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var match = GetPublishedItems()
            .FirstOrDefault(item => string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        return match?.LogoPath;
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _db.PartnerStripSettings.AnyAsync(s => s.Id == PartnerStripSettings.SingletonId, cancellationToken))
        {
            _db.PartnerStripSettings.Add(new PartnerStripSettings
            {
                Id = PartnerStripSettings.SingletonId,
                Eyebrow = PartnerStripDefaults.Eyebrow,
                HeadingBefore = PartnerStripDefaults.HeadingBefore,
                HeadingHighlight = PartnerStripDefaults.HeadingHighlight,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await _db.PartnerStripItems.AnyAsync(cancellationToken))
        {
            var order = 10;
            var now = DateTime.UtcNow;
            foreach (var seed in PartnerStripDefaults.Items)
            {
                _db.PartnerStripItems.Add(new PartnerStripItem
                {
                    Name = seed.Name,
                    LogoPath = seed.LogoPath,
                    SortOrder = order,
                    IsPublished = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                order += 10;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveSettingsAsync(
        PartnerStripSettingsEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PartnerStripSettings.FirstOrDefaultAsync(
            s => s.Id == PartnerStripSettings.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = new PartnerStripSettings { Id = PartnerStripSettings.SingletonId };
            _db.PartnerStripSettings.Add(entity);
        }

        entity.Eyebrow = model.Eyebrow.Trim();
        entity.HeadingBefore = model.HeadingBefore.Trim();
        entity.HeadingHighlight = model.HeadingHighlight.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public Task<IReadOnlyList<PartnerStripItem>> GetAllItemsAsync(CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PartnerStripItem>)t.Result, cancellationToken);

    public Task<PartnerStripItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.PartnerStripItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<(bool Ok, string? Error, int? Id)> CreateItemAsync(
        PartnerStripItemEditViewModel model,
        IFormFile? logo,
        CancellationToken cancellationToken = default)
    {
        var pathResult = await ResolveLogoAsync(model.LogoPath, logo, cancellationToken);
        if (pathResult.Error != null)
        {
            return (false, pathResult.Error, null);
        }

        var sort = model.SortOrder;
        if (sort <= 0)
        {
            sort = (await _db.PartnerStripItems.Select(i => (int?)i.SortOrder).MaxAsync(cancellationToken) ?? 0) + 10;
        }

        var now = DateTime.UtcNow;
        var entity = new PartnerStripItem
        {
            Name = model.Name.Trim(),
            LogoPath = pathResult.Path,
            LinkUrl = string.IsNullOrWhiteSpace(model.LinkUrl) ? null : model.LinkUrl.Trim(),
            SortOrder = sort,
            IsPublished = model.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.PartnerStripItems.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateItemAsync(
        PartnerStripItemEditViewModel model,
        IFormFile? logo,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.PartnerStripItems.FirstOrDefaultAsync(i => i.Id == model.Id, cancellationToken);
        if (entity == null)
        {
            return (false, "Partner item not found.");
        }

        var pathResult = await ResolveLogoAsync(
            string.IsNullOrWhiteSpace(model.LogoPath) ? entity.LogoPath : model.LogoPath,
            logo,
            cancellationToken);
        if (pathResult.Error != null)
        {
            return (false, pathResult.Error);
        }

        entity.Name = model.Name.Trim();
        entity.LogoPath = pathResult.Path;
        entity.LinkUrl = string.IsNullOrWhiteSpace(model.LinkUrl) ? null : model.LinkUrl.Trim();
        entity.SortOrder = model.SortOrder;
        entity.IsPublished = model.IsPublished;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public async Task<bool> DeleteItemAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PartnerStripItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _db.PartnerStripItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return true;
    }

    public async Task<bool> SetItemPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PartnerStripItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        entity.IsPublished = isPublished;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return true;
    }

    public async Task<(bool Ok, string? Error)> MoveItemAsync(int id, int direction, CancellationToken cancellationToken = default)
    {
        if (direction is not (1 or -1))
        {
            return (false, "Invalid move direction.");
        }

        var items = await _db.PartnerStripItems.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync(cancellationToken);
        var index = items.FindIndex(i => i.Id == id);
        if (index < 0)
        {
            return (false, "Partner item not found.");
        }

        var target = index + direction;
        if (target < 0 || target >= items.Count)
        {
            return (false, "Already at the edge of the list.");
        }

        var now = DateTime.UtcNow;
        (items[index].SortOrder, items[target].SortOrder) = (items[target].SortOrder, items[index].SortOrder);
        items[index].UpdatedAt = now;
        items[target].UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    private async Task<(string Path, string? Error)> ResolveLogoAsync(
        string currentPath,
        IFormFile? logo,
        CancellationToken cancellationToken)
    {
        var path = currentPath?.Trim() ?? string.Empty;
        var upload = await CmsAssetUpload.TrySaveAsync(
            _environment,
            PartnerStripAssetRules.UploadFolderRelative,
            PartnerStripAssetRules.UploadFolderWeb,
            logo,
            cancellationToken);
        if (upload.Error != null)
        {
            return (path, upload.Error);
        }

        if (upload.Path != null)
        {
            path = upload.Path;
        }

        var pathError = PartnerStripAssetRules.ValidateStoredPath(path);
        return pathError != null ? (path, pathError) : (path, null);
    }

    private static PartnerStripPublicSettings MapSettings(PartnerStripSettings? entity)
    {
        entity ??= new PartnerStripSettings
        {
            Eyebrow = PartnerStripDefaults.Eyebrow,
            HeadingBefore = PartnerStripDefaults.HeadingBefore,
            HeadingHighlight = PartnerStripDefaults.HeadingHighlight
        };

        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new PartnerStripPublicSettings
        {
            Eyebrow = Text(entity.Eyebrow, PartnerStripDefaults.Eyebrow),
            HeadingBefore = Text(entity.HeadingBefore, PartnerStripDefaults.HeadingBefore),
            HeadingHighlight = Text(entity.HeadingHighlight, PartnerStripDefaults.HeadingHighlight)
        };
    }

    private void InvalidateCache()
    {
        _cache.Remove(SettingsCacheKey);
        _cache.Remove(ItemsCacheKey);
    }
}

public sealed class CurrentPartnerStrip : ICurrentPartnerStrip
{
    private readonly Lazy<PartnerStripPublicSettings> _settings;
    private readonly Lazy<IReadOnlyList<PartnerStripPublicItem>> _items;

    public CurrentPartnerStrip(IPartnerStripService partners)
    {
        _settings = new Lazy<PartnerStripPublicSettings>(partners.GetSettings);
        _items = new Lazy<IReadOnlyList<PartnerStripPublicItem>>(partners.GetPublishedItems);
    }

    public PartnerStripPublicSettings Settings => _settings.Value;
    public IReadOnlyList<PartnerStripPublicItem> Items => _items.Value;
}
