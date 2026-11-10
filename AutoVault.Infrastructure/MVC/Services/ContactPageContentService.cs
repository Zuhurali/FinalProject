using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class ContactPageContentService : IContactPageContentService
{
    public const string CacheKey = "contact-page-content-public-v1";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public ContactPageContentService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public ContactPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out ContactPagePublicContent? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.ContactPageContents.AsNoTracking()
                .FirstOrDefault(content => content.Id == ContactPageContent.SingletonId);
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.ContactPageContents.AnyAsync(c => c.Id == ContactPageContent.SingletonId, cancellationToken))
        {
            return;
        }

        _db.ContactPageContents.Add(CreateSeedEntity());
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        ContactPageContentEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.MapEmbedUrl)
            || !(model.MapEmbedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || model.MapEmbedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Map embed URL must start with http:// or https://.");
        }

        var entity = await _db.ContactPageContents.FirstOrDefaultAsync(
            c => c.Id == ContactPageContent.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.ContactPageContents.Add(entity);
        }

        entity.PageTitle = model.PageTitle.Trim();
        entity.SeoTitle = model.SeoTitle.Trim();
        entity.SeoDescription = model.SeoDescription.Trim();
        entity.FormTitle = model.FormTitle.Trim();
        entity.FormIntro = model.FormIntro.Trim();
        entity.LocationCardTitle = model.LocationCardTitle.Trim();
        entity.PhoneCardTitle = model.PhoneCardTitle.Trim();
        entity.EmailCardTitle = model.EmailCardTitle.Trim();
        entity.MapEmbedUrl = model.MapEmbedUrl.Trim();
        entity.MapTitle = model.MapTitle.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public static ContactPageContent CreateSeedEntity() => new()
    {
        Id = ContactPageContent.SingletonId,
        PageTitle = ContactPageContentDefaults.PageTitle,
        SeoTitle = ContactPageContentDefaults.SeoTitle,
        SeoDescription = ContactPageContentDefaults.SeoDescription,
        FormTitle = ContactPageContentDefaults.FormTitle,
        FormIntro = ContactPageContentDefaults.FormIntro,
        LocationCardTitle = ContactPageContentDefaults.LocationCardTitle,
        PhoneCardTitle = ContactPageContentDefaults.PhoneCardTitle,
        EmailCardTitle = ContactPageContentDefaults.EmailCardTitle,
        MapEmbedUrl = ContactPageContentDefaults.MapEmbedUrl,
        MapTitle = ContactPageContentDefaults.MapTitle,
        UpdatedAt = DateTime.UtcNow
    };

    private static ContactPagePublicContent Map(ContactPageContent? entity)
    {
        entity ??= CreateSeedEntity();
        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new ContactPagePublicContent
        {
            PageTitle = Text(entity.PageTitle, ContactPageContentDefaults.PageTitle),
            SeoTitle = Text(entity.SeoTitle, ContactPageContentDefaults.SeoTitle),
            SeoDescription = Text(entity.SeoDescription, ContactPageContentDefaults.SeoDescription),
            FormTitle = Text(entity.FormTitle, ContactPageContentDefaults.FormTitle),
            FormIntro = Text(entity.FormIntro, ContactPageContentDefaults.FormIntro),
            LocationCardTitle = Text(entity.LocationCardTitle, ContactPageContentDefaults.LocationCardTitle),
            PhoneCardTitle = Text(entity.PhoneCardTitle, ContactPageContentDefaults.PhoneCardTitle),
            EmailCardTitle = Text(entity.EmailCardTitle, ContactPageContentDefaults.EmailCardTitle),
            MapEmbedUrl = Text(entity.MapEmbedUrl, ContactPageContentDefaults.MapEmbedUrl),
            MapTitle = Text(entity.MapTitle, ContactPageContentDefaults.MapTitle)
        };
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}

public sealed class CurrentContactPageContent : ICurrentContactPageContent
{
    private readonly Lazy<ContactPagePublicContent> _current;

    public CurrentContactPageContent(IContactPageContentService contactPage)
    {
        _current = new Lazy<ContactPagePublicContent>(contactPage.GetPublic);
    }

    public ContactPagePublicContent Current => _current.Value;
}
