using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject.MVC.Services;

public sealed class LegalPageContentService : ILegalPageContentService
{
    public const string CacheKey = "legal-page-content-public-v1";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public LegalPageContentService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public LegalPagePublicContent GetPublic()
    {
        if (_cache.TryGetValue(CacheKey, out LegalPagePublicContent? cached) && cached != null)
        {
            return cached;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var entity = _db.LegalPageContents.AsNoTracking()
                .FirstOrDefault(content => content.Id == LegalPageContent.SingletonId);
            var mapped = Map(entity);
            _cache.Set(CacheKey, mapped, TimeSpan.FromMinutes(10));
            return mapped;
        }
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.LegalPageContents.AnyAsync(c => c.Id == LegalPageContent.SingletonId, cancellationToken))
        {
            return;
        }

        _db.LegalPageContents.Add(CreateSeedEntity());
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(
        LegalPageContentEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.LegalPageContents.FirstOrDefaultAsync(
            c => c.Id == LegalPageContent.SingletonId,
            cancellationToken);
        if (entity == null)
        {
            entity = CreateSeedEntity();
            _db.LegalPageContents.Add(entity);
        }

        entity.PrivacyTitle = model.PrivacyTitle.Trim();
        entity.PrivacyBody = model.PrivacyBody.Trim();
        entity.PrivacySeoTitle = model.PrivacySeoTitle.Trim();
        entity.PrivacySeoDescription = model.PrivacySeoDescription.Trim();
        entity.TermsTitle = model.TermsTitle.Trim();
        entity.TermsBody = model.TermsBody.Trim();
        entity.TermsSeoTitle = model.TermsSeoTitle.Trim();
        entity.TermsSeoDescription = model.TermsSeoDescription.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache();
        return (true, null);
    }

    public static LegalPageContent CreateSeedEntity() => new()
    {
        Id = LegalPageContent.SingletonId,
        PrivacyTitle = LegalPageContentDefaults.PrivacyTitle,
        PrivacyBody = LegalPageContentDefaults.PrivacyBody,
        PrivacySeoTitle = LegalPageContentDefaults.PrivacySeoTitle,
        PrivacySeoDescription = LegalPageContentDefaults.PrivacySeoDescription,
        TermsTitle = LegalPageContentDefaults.TermsTitle,
        TermsBody = LegalPageContentDefaults.TermsBody,
        TermsSeoTitle = LegalPageContentDefaults.TermsSeoTitle,
        TermsSeoDescription = LegalPageContentDefaults.TermsSeoDescription,
        UpdatedAt = DateTime.UtcNow
    };

    private static LegalPagePublicContent Map(LegalPageContent? entity)
    {
        entity ??= CreateSeedEntity();
        static string Text(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        return new LegalPagePublicContent
        {
            PrivacyTitle = Text(entity.PrivacyTitle, LegalPageContentDefaults.PrivacyTitle),
            PrivacyBody = Text(entity.PrivacyBody, LegalPageContentDefaults.PrivacyBody),
            PrivacySeoTitle = Text(entity.PrivacySeoTitle, LegalPageContentDefaults.PrivacySeoTitle),
            PrivacySeoDescription = Text(entity.PrivacySeoDescription, LegalPageContentDefaults.PrivacySeoDescription),
            TermsTitle = Text(entity.TermsTitle, LegalPageContentDefaults.TermsTitle),
            TermsBody = Text(entity.TermsBody, LegalPageContentDefaults.TermsBody),
            TermsSeoTitle = Text(entity.TermsSeoTitle, LegalPageContentDefaults.TermsSeoTitle),
            TermsSeoDescription = Text(entity.TermsSeoDescription, LegalPageContentDefaults.TermsSeoDescription)
        };
    }

    private void InvalidateCache() => _cache.Remove(CacheKey);
}

public sealed class CurrentLegalPageContent : ICurrentLegalPageContent
{
    private readonly Lazy<LegalPagePublicContent> _current;

    public CurrentLegalPageContent(ILegalPageContentService legalPage)
    {
        _current = new Lazy<LegalPagePublicContent>(legalPage.GetPublic);
    }

    public LegalPagePublicContent Current => _current.Value;
}
