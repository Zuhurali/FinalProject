using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class HomePageContentTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_home_requires_Admin_or_SuperAdmin_and_antiforgery()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminHomeController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminHomeController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == nameof(AdminHomeController.Index)
                && method.GetParameters().Length > 1)
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void Home_asset_rules_reject_path_traversal_and_oversized_files()
    {
        Assert.NotNull(HomePageAssetRules.ValidateUpload("x.png", "image/png", HomePageAssetRules.MaxBytes + 1));
        Assert.NotNull(HomePageAssetRules.ValidateUpload("note.txt", "text/plain", 10));
        Assert.False(HomePageAssetRules.IsSafeStoredPath("~/uploads/home/../../web.config"));
        Assert.True(HomePageAssetRules.IsSafeStoredPath(HomePageContentDefaults.HeroImagePath));
        Assert.True(HomePageAssetRules.IsSafeStoredPath("~/uploads/home/abc.webp"));
    }

    [Fact]
    public void Edit_view_model_requires_core_marketing_fields()
    {
        var model = new HomePageContentEditViewModel();
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(HomePageContentEditViewModel.HeroTitle)));
    }

    [Fact]
    public async Task EnsureSeed_and_save_persist_and_drive_public_homepage_snapshot()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new HomePageContentService(db, cache, new TestEnvironment());

        await service.EnsureSeedAsync();
        var seeded = await db.HomePageContents.AsNoTracking()
            .SingleAsync(content => content.Id == HomePageContent.SingletonId);
        Assert.Equal(HomePageContentDefaults.HeroTitle, seeded.HeroTitle);
        Assert.Equal(HomePageContentDefaults.HeroHighlight, seeded.HeroHighlight);
        Assert.Equal(HomePageContentDefaults.AboutBody, seeded.AboutBody);

        var publicBefore = service.GetPublic();
        Assert.Equal(HomePageContentDefaults.HeroImagePath, publicBefore.HeroImagePath);
        Assert.Equal(HomePageContentDefaults.Feature1Title, publicBefore.Feature1Title);

        var marker = $"Home CMS {Guid.NewGuid():N}";
        var edit = ToEditModel(publicBefore);
        edit.HeroTitle = marker;
        edit.HeroHighlight = "Autos";
        edit.AboutBody = $"{marker} about body for tests.";
        edit.Feature1Title = "Test Pricing";

        var (ok, error) = await service.SaveAsync(edit, new HomePageImageUploads());
        Assert.True(ok, error);
        cache.Remove(HomePageContentService.CacheKey);

        var publicAfter = service.GetPublic();
        Assert.Equal(marker, publicAfter.HeroTitle);
        Assert.Equal("Autos", publicAfter.HeroHighlight);
        Assert.Equal("Test Pricing", publicAfter.Feature1Title);
        Assert.Contains(marker, publicAfter.AboutBody, StringComparison.Ordinal);

        var reloaded = await db.HomePageContents.AsNoTracking()
            .SingleAsync(content => content.Id == HomePageContent.SingletonId);
        Assert.Equal(marker, reloaded.HeroTitle);

        await service.SaveAsync(ToEditModel(FromSeed()), new HomePageImageUploads());
        cache.Remove(HomePageContentService.CacheKey);
        Assert.Equal(HomePageContentDefaults.HeroTitle, service.GetPublic().HeroTitle);
    }

    private static HomePagePublicContent FromSeed()
    {
        var seed = HomePageContentService.CreateSeedEntity();
        return new HomePagePublicContent
        {
            HeroTitle = seed.HeroTitle,
            HeroHighlight = seed.HeroHighlight,
            HeroImagePath = seed.HeroImagePath,
            HeroImageAlt = seed.HeroImageAlt,
            CategoriesEyebrow = seed.CategoriesEyebrow,
            CategoriesHeadingBefore = seed.CategoriesHeadingBefore,
            CategoriesHeadingHighlight = seed.CategoriesHeadingHighlight,
            CategoriesHeadingAfter = seed.CategoriesHeadingAfter,
            PortfolioEyebrow = seed.PortfolioEyebrow,
            PortfolioHeadingBefore = seed.PortfolioHeadingBefore,
            PortfolioHeadingHighlight = seed.PortfolioHeadingHighlight,
            CampaignsEyebrow = seed.CampaignsEyebrow,
            CampaignsHeadingBefore = seed.CampaignsHeadingBefore,
            CampaignsHeadingHighlight = seed.CampaignsHeadingHighlight,
            AboutEyebrow = seed.AboutEyebrow,
            AboutHeadingBefore = seed.AboutHeadingBefore,
            AboutHeadingHighlight = seed.AboutHeadingHighlight,
            AboutBody = seed.AboutBody,
            AboutCtaLabel = seed.AboutCtaLabel,
            AboutImageMainPath = seed.AboutImageMainPath,
            AboutImageFloat1Path = seed.AboutImageFloat1Path,
            AboutImageFloat2Path = seed.AboutImageFloat2Path,
            BrandsEyebrow = seed.BrandsEyebrow,
            BrandsHeadingBefore = seed.BrandsHeadingBefore,
            BrandsHeadingHighlight = seed.BrandsHeadingHighlight,
            WhyChooseEyebrow = seed.WhyChooseEyebrow,
            WhyChooseHeadingBefore = seed.WhyChooseHeadingBefore,
            WhyChooseHeadingHighlight = seed.WhyChooseHeadingHighlight,
            WhyChooseBackgroundPath = seed.WhyChooseBackgroundPath,
            Feature1Title = seed.Feature1Title,
            Feature1Description = seed.Feature1Description,
            Feature1IconPath = seed.Feature1IconPath,
            Feature2Title = seed.Feature2Title,
            Feature2Description = seed.Feature2Description,
            Feature2IconPath = seed.Feature2IconPath,
            Feature3Title = seed.Feature3Title,
            Feature3Description = seed.Feature3Description,
            Feature3IconPath = seed.Feature3IconPath,
            Feature4Title = seed.Feature4Title,
            Feature4Description = seed.Feature4Description,
            Feature4IconPath = seed.Feature4IconPath,
            FeaturedEyebrow = seed.FeaturedEyebrow,
            FeaturedHeadingBefore = seed.FeaturedHeadingBefore,
            FeaturedHeadingHighlight = seed.FeaturedHeadingHighlight
        };
    }

    private static HomePageContentEditViewModel ToEditModel(HomePagePublicContent current) => new()
    {
        HeroTitle = current.HeroTitle,
        HeroHighlight = current.HeroHighlight,
        HeroImagePath = current.HeroImagePath,
        HeroImageAlt = current.HeroImageAlt,
        CategoriesEyebrow = current.CategoriesEyebrow,
        CategoriesHeadingBefore = current.CategoriesHeadingBefore,
        CategoriesHeadingHighlight = current.CategoriesHeadingHighlight,
        CategoriesHeadingAfter = current.CategoriesHeadingAfter,
        PortfolioEyebrow = current.PortfolioEyebrow,
        PortfolioHeadingBefore = current.PortfolioHeadingBefore,
        PortfolioHeadingHighlight = current.PortfolioHeadingHighlight,
        CampaignsEyebrow = current.CampaignsEyebrow,
        CampaignsHeadingBefore = current.CampaignsHeadingBefore,
        CampaignsHeadingHighlight = current.CampaignsHeadingHighlight,
        AboutEyebrow = current.AboutEyebrow,
        AboutHeadingBefore = current.AboutHeadingBefore,
        AboutHeadingHighlight = current.AboutHeadingHighlight,
        AboutBody = current.AboutBody,
        AboutCtaLabel = current.AboutCtaLabel,
        AboutImageMainPath = current.AboutImageMainPath,
        AboutImageFloat1Path = current.AboutImageFloat1Path,
        AboutImageFloat2Path = current.AboutImageFloat2Path,
        BrandsEyebrow = current.BrandsEyebrow,
        BrandsHeadingBefore = current.BrandsHeadingBefore,
        BrandsHeadingHighlight = current.BrandsHeadingHighlight,
        WhyChooseEyebrow = current.WhyChooseEyebrow,
        WhyChooseHeadingBefore = current.WhyChooseHeadingBefore,
        WhyChooseHeadingHighlight = current.WhyChooseHeadingHighlight,
        WhyChooseBackgroundPath = current.WhyChooseBackgroundPath,
        Feature1Title = current.Feature1Title,
        Feature1Description = current.Feature1Description,
        Feature1IconPath = current.Feature1IconPath,
        Feature2Title = current.Feature2Title,
        Feature2Description = current.Feature2Description,
        Feature2IconPath = current.Feature2IconPath,
        Feature3Title = current.Feature3Title,
        Feature3Description = current.Feature3Description,
        Feature3IconPath = current.Feature3IconPath,
        Feature4Title = current.Feature4Title,
        Feature4Description = current.Feature4Description,
        Feature4IconPath = current.Feature4IconPath,
        FeaturedEyebrow = current.FeaturedEyebrow,
        FeaturedHeadingBefore = current.FeaturedHeadingBefore,
        FeaturedHeadingHighlight = current.FeaturedHeadingHighlight
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
