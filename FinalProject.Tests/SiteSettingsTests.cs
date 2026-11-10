using FinalProject.MVC.Repositories.Implementations;
using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Configuration;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class SiteSettingsTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_settings_require_Admin_or_SuperAdmin_and_antiforgery()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminSettingsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminSettingsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == nameof(AdminSettingsController.Index)
                && method.GetParameters().Length > 0)
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void Brand_asset_rules_reject_path_traversal_and_oversized_files()
    {
        Assert.NotNull(SiteBrandAssetRules.ValidateUpload("x.png", "image/png", SiteBrandAssetRules.MaxBytes + 1));
        Assert.NotNull(SiteBrandAssetRules.ValidateUpload("note.txt", "text/plain", 10));
        Assert.False(SiteBrandAssetRules.IsSafeStoredPath("~/assets/images/site/uploads/../../web.config"));
        Assert.True(SiteBrandAssetRules.IsSafeStoredPath("~/assets/images/logo/logo.svg"));
        Assert.True(SiteBrandAssetRules.IsSafeStoredPath("~/assets/images/site/uploads/abc.png"));
    }

    [Fact]
    public async Task EnsureSeed_and_save_persist_to_sql_server_and_drive_public_snapshot()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var env = new TestEnvironment();
        var config = Options.Create(new DealershipContactSettings
        {
            DisplayPhone = DealershipContactDefaults.DisplayPhone,
            E164Phone = DealershipContactDefaults.E164Phone,
            WhatsAppDigits = DealershipContactDefaults.WhatsAppDigits,
            Email = DealershipContactDefaults.Email,
            AddressLine = DealershipContactDefaults.AddressLine,
            MapsQuery = DealershipContactDefaults.MapsQuery
        });
        var service = new SiteSettingsService(new ApplicationRepository(db), cache, env, config);

        var beforeSeed = await db.SiteSettings.AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.Id == SiteSettings.SingletonId);
        var emailBeforeSeed = beforeSeed?.Email;
        SiteSettings? restoreFrom = null;

        try
        {
            await service.EnsureSeedAsync();
            var seeded = await db.SiteSettings.AsNoTracking()
                .SingleAsync(settings => settings.Id == SiteSettings.SingletonId);
            restoreFrom = beforeSeed ?? seeded;
            if (beforeSeed != null)
            {
                Assert.Equal(emailBeforeSeed, seeded.Email);
            }

            var marker = $"AutoVault CMS {Guid.NewGuid():N}";
            var (ok, error) = await service.SaveAsync(new SiteSettingsEditViewModel
            {
                SiteName = "AutoVault Test",
                DisplayPhone = "+994 50 111 22 33",
                E164Phone = "+994501112233",
                WhatsAppDigits = "994501112233",
                Email = "cms-settings@example.com",
                AddressLine = "Test Address 1",
                MapsQuery = "Test Address 1, Baku",
                LogoPath = "~/assets/images/logo/logo.svg",
                FooterLogoPath = "~/assets/images/logo/footer-logo.svg",
                FaviconPath = "~/fav.svg",
                FooterText = marker,
                CopyrightText = "Copyright © {year} AutoVault Test",
                FacebookUrl = "https://facebook.com/autovault",
                InstagramUrl = null,
                YoutubeUrl = null,
                TwitterUrl = null,
                DefaultSeoTitle = "AutoVault Test SEO",
                DefaultMetaDescription = "CMS meta description for tests.",
                CallCtaHeadline = "Please Call Us Now."
            }, null, null, null);

            Assert.True(ok, error);
            cache.Remove(SiteSettingsService.CacheKey);

            var publicSettings = service.GetPublic();
            Assert.Equal("AutoVault Test", publicSettings.SiteName);
            Assert.Equal("cms-settings@example.com", publicSettings.Contact.Email);
            Assert.Equal(marker, publicSettings.FooterText);
            Assert.Equal("https://facebook.com/autovault", publicSettings.FacebookUrl);
            Assert.Equal("Please Call Us Now.", publicSettings.CallCtaHeadline);
            Assert.Equal("tel:+994501112233", publicSettings.Contact.TelHref);
            Assert.Contains("2026", publicSettings.FormatCopyright(2026), StringComparison.Ordinal);

            var reloaded = await db.SiteSettings.AsNoTracking()
                .SingleAsync(settings => settings.Id == SiteSettings.SingletonId);
            Assert.Equal(marker, reloaded.FooterText);
            Assert.Equal("cms-settings@example.com", reloaded.Email);
        }
        finally
        {
            if (restoreFrom != null)
            {
                cache.Remove(SiteSettingsService.CacheKey);
                var (restored, restoreError) = await service.SaveAsync(ToEdit(restoreFrom), null, null, null);
                if (!restored)
                {
                    throw new InvalidOperationException(restoreError ?? "Could not restore the original SiteSettings row.");
                }
            }
        }
    }

    private static SiteSettingsEditViewModel ToEdit(SiteSettings settings) => new()
    {
        SiteName = settings.SiteName,
        DisplayPhone = settings.DisplayPhone,
        E164Phone = settings.E164Phone,
        WhatsAppDigits = settings.WhatsAppDigits,
        Email = settings.Email,
        AddressLine = settings.AddressLine,
        MapsQuery = settings.MapsQuery,
        LogoPath = settings.LogoPath,
        FooterLogoPath = settings.FooterLogoPath,
        FaviconPath = settings.FaviconPath,
        FooterText = settings.FooterText,
        CopyrightText = settings.CopyrightText,
        FacebookUrl = settings.FacebookUrl,
        InstagramUrl = settings.InstagramUrl,
        YoutubeUrl = settings.YoutubeUrl,
        TwitterUrl = settings.TwitterUrl,
        DefaultSeoTitle = settings.DefaultSeoTitle,
        DefaultMetaDescription = settings.DefaultMetaDescription,
        CallCtaHeadline = settings.CallCtaHeadline
    };

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.MVC";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
