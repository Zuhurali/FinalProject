using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Mappings;
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
public class PartnerAndPageChromePersistenceTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_partner_and_page_chrome_controllers_require_Admin_or_SuperAdmin()
    {
        foreach (var controller in new[]
                 {
                     typeof(AdminPartnersController),
                     typeof(AdminFaqPageController),
                     typeof(AdminTeamPageController),
                     typeof(AdminDealerPageController),
                     typeof(AdminCalculatorPageController)
                 })
        {
            Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
            var post = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.GetCustomAttribute<HttpPostAttribute>() != null);
            Assert.NotNull(post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }

    [Fact]
    public async Task Partner_strip_settings_and_additive_item_persist_on_sql_server()
    {
        await using var db = CreateDb();
        var env = new TestEnvironment();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var partners = new PartnerStripService(db, cache, env);
        await partners.EnsureSeedAsync();

        var originalSettings = partners.GetSettings();
        var marker = $"Partners {Guid.NewGuid():N}"[..20];
        Assert.True((await partners.SaveSettingsAsync(new PartnerStripSettingsEditViewModel
        {
            Eyebrow = marker,
            HeadingBefore = originalSettings.HeadingBefore,
            HeadingHighlight = originalSettings.HeadingHighlight
        })).Ok);
        cache.Remove(PartnerStripService.SettingsCacheKey);
        Assert.Equal(marker, partners.GetSettings().Eyebrow);

        var created = await partners.CreateItemAsync(new PartnerStripItemEditViewModel
        {
            Name = $"TestCo-{Guid.NewGuid():N}"[..24],
            LogoPath = PartnerStripDefaults.Items[0].LogoPath,
            SortOrder = 9990,
            IsPublished = true
        }, logo: null);
        Assert.True(created.Ok, created.Error);
        Assert.NotNull(created.Id);

        try
        {
            cache.Remove(PartnerStripService.ItemsCacheKey);
            Assert.Contains(partners.GetPublishedItems(), item => item.Id == created.Id);
            Assert.True(await db.PartnerStripItems.AsNoTracking()
                .AnyAsync(item => item.Id == created.Id && item.IsPublished));
        }
        finally
        {
            Assert.True(await partners.DeleteItemAsync(created.Id!.Value));
            Assert.True((await partners.SaveSettingsAsync(new PartnerStripSettingsEditViewModel
            {
                Eyebrow = originalSettings.Eyebrow,
                HeadingBefore = originalSettings.HeadingBefore,
                HeadingHighlight = originalSettings.HeadingHighlight
            })).Ok);
            cache.Remove(PartnerStripService.SettingsCacheKey);
            cache.Remove(PartnerStripService.ItemsCacheKey);
        }
    }

    [Fact]
    public async Task Page_chrome_and_legal_saves_drive_public_snapshots()
    {
        await using var db = CreateDb();
        var env = new TestEnvironment();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var faqPage = new FaqPageContentService(db, cache, env);
        await faqPage.EnsureSeedAsync();
        var faqSeed = FaqPageContentService.CreateSeed();
        Assert.True((await faqPage.SaveAsync(ToFaqEdit(faqSeed), banner: null)).Ok);
        var faqEdit = ToFaqEdit(faqPage.GetPublic());
        faqEdit.Eyebrow = $"FAQ {Guid.NewGuid():N}"[..16];
        Assert.True((await faqPage.SaveAsync(faqEdit, banner: null)).Ok);
        cache.Remove(FaqPageContentService.CacheKey);
        Assert.Equal(faqEdit.Eyebrow, faqPage.GetPublic().Eyebrow);
        Assert.True((await faqPage.SaveAsync(ToFaqEdit(faqSeed), banner: null)).Ok);

        var teamPage = new TeamPageContentService(db, cache);
        await teamPage.EnsureSeedAsync();
        var teamSeed = TeamPageContentService.CreateSeed();
        Assert.True((await teamPage.SaveAsync(ToTeamEdit(teamSeed))).Ok);
        var teamEdit = ToTeamEdit(teamPage.GetPublic());
        teamEdit.Eyebrow = $"Team {Guid.NewGuid():N}"[..16];
        Assert.True((await teamPage.SaveAsync(teamEdit)).Ok);
        cache.Remove(TeamPageContentService.CacheKey);
        Assert.Equal(teamEdit.Eyebrow, teamPage.GetPublic().Eyebrow);
        Assert.True((await teamPage.SaveAsync(ToTeamEdit(teamSeed))).Ok);

        var dealerPage = new DealerPageContentService(db, cache);
        await dealerPage.EnsureSeedAsync();
        var dealerSeed = DealerPageContentService.CreateSeed();
        Assert.True((await dealerPage.SaveAsync(ToDealerEdit(dealerSeed))).Ok);
        var dealerEdit = ToDealerEdit(dealerPage.GetPublic());
        dealerEdit.PageTitle = $"Dealers {Guid.NewGuid():N}"[..20];
        Assert.True((await dealerPage.SaveAsync(dealerEdit)).Ok);
        cache.Remove(DealerPageContentService.CacheKey);
        Assert.Equal(dealerEdit.PageTitle, dealerPage.GetPublic().PageTitle);
        Assert.True((await dealerPage.SaveAsync(ToDealerEdit(dealerSeed))).Ok);

        var calcPage = new CalculatorPageContentService(db, cache);
        await calcPage.EnsureSeedAsync();
        var calcSeed = CalculatorPageContentService.CreateSeed();
        Assert.True((await calcPage.SaveAsync(ToCalcEdit(calcSeed))).Ok);
        var calcEdit = ToCalcEdit(calcPage.GetPublic());
        calcEdit.PageTitle = $"Loan {Guid.NewGuid():N}"[..16];
        Assert.True((await calcPage.SaveAsync(calcEdit)).Ok);
        cache.Remove(CalculatorPageContentService.CacheKey);
        Assert.Equal(calcEdit.PageTitle, calcPage.GetPublic().PageTitle);
        Assert.True((await calcPage.SaveAsync(ToCalcEdit(calcSeed))).Ok);

        var legal = new LegalPageContentService(db, cache);
        await legal.EnsureSeedAsync();
        var legalSeed = LegalPageContentService.CreateSeedEntity();
        Assert.True((await legal.SaveAsync(ToLegalEdit(legalSeed))).Ok);
        var legalEdit = ToLegalEdit(legal.GetPublic());
        legalEdit.PrivacyTitle = $"Privacy {Guid.NewGuid():N}"[..20];
        Assert.True((await legal.SaveAsync(legalEdit)).Ok);
        cache.Remove(LegalPageContentService.CacheKey);
        Assert.Equal(legalEdit.PrivacyTitle, legal.GetPublic().PrivacyTitle);
        Assert.True((await legal.SaveAsync(ToLegalEdit(legalSeed))).Ok);
    }

    [Fact]
    public void CountLabel_matches_unknown_admin_category_by_name()
    {
        var cars = new[]
        {
            new Car { Category = "Wagon", Name = "A" },
            new Car { Category = "Wagon", Name = "B" },
            new Car { Category = "Sedan", Name = "C" }
        };

        Assert.Equal("2 Cars", CarApiMapper.CountLabel(cars, "Wagon"));
        Assert.Equal("1 Car", CarApiMapper.CountLabel(cars, "Sedan"));
        Assert.Equal("0 Cars", CarApiMapper.CountLabel(cars, "Missing"));
    }

    [Fact]
    public void CarCategories_Matches_accepts_sql_names_slugs_and_legacy_aliases()
    {
        Assert.True(CarCategories.Matches("Sedan", "Sedans"));
        Assert.True(CarCategories.Matches("Sedan", "Sedan"));
        Assert.True(CarCategories.Matches("Sports Car", "Sports-Car"));
        Assert.True(CarCategories.Matches("Wagon", "Wagon"));
        Assert.True(CarCategories.Matches("Wagon", "wagon"));
        Assert.False(CarCategories.Matches("Sedan", "Wagon"));
    }

    private static FaqPageContentEditViewModel ToFaqEdit(FaqPageContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        Eyebrow = entity.Eyebrow,
        HeadingBefore = entity.HeadingBefore,
        HeadingHighlight = entity.HeadingHighlight,
        BannerImagePath = entity.BannerImagePath,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static FaqPageContentEditViewModel ToFaqEdit(FaqPagePublicContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        Eyebrow = entity.Eyebrow,
        HeadingBefore = entity.HeadingBefore,
        HeadingHighlight = entity.HeadingHighlight,
        BannerImagePath = entity.BannerImagePath,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static TeamPageContentEditViewModel ToTeamEdit(TeamPageContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        Eyebrow = entity.Eyebrow,
        HeadingBefore = entity.HeadingBefore,
        HeadingHighlight = entity.HeadingHighlight,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static TeamPageContentEditViewModel ToTeamEdit(TeamPagePublicContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        Eyebrow = entity.Eyebrow,
        HeadingBefore = entity.HeadingBefore,
        HeadingHighlight = entity.HeadingHighlight,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static DealerPageContentEditViewModel ToDealerEdit(DealerPageContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        PageTitle = entity.PageTitle,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static DealerPageContentEditViewModel ToDealerEdit(DealerPagePublicContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        PageTitle = entity.PageTitle,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static CalculatorPageContentEditViewModel ToCalcEdit(CalculatorPageContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        PageTitle = entity.PageTitle,
        IntroText = entity.IntroText,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static CalculatorPageContentEditViewModel ToCalcEdit(CalculatorPagePublicContent entity) => new()
    {
        BreadcrumbTitle = entity.BreadcrumbTitle,
        PageTitle = entity.PageTitle,
        IntroText = entity.IntroText,
        SeoTitle = entity.SeoTitle,
        SeoDescription = entity.SeoDescription
    };

    private static LegalPageContentEditViewModel ToLegalEdit(LegalPageContent entity) => new()
    {
        PrivacyTitle = entity.PrivacyTitle,
        PrivacyBody = entity.PrivacyBody,
        PrivacySeoTitle = entity.PrivacySeoTitle,
        PrivacySeoDescription = entity.PrivacySeoDescription,
        TermsTitle = entity.TermsTitle,
        TermsBody = entity.TermsBody,
        TermsSeoTitle = entity.TermsSeoTitle,
        TermsSeoDescription = entity.TermsSeoDescription
    };

    private static LegalPageContentEditViewModel ToLegalEdit(LegalPagePublicContent entity) => new()
    {
        PrivacyTitle = entity.PrivacyTitle,
        PrivacyBody = entity.PrivacyBody,
        PrivacySeoTitle = entity.PrivacySeoTitle,
        PrivacySeoDescription = entity.PrivacySeoDescription,
        TermsTitle = entity.TermsTitle,
        TermsBody = entity.TermsBody,
        TermsSeoTitle = entity.TermsSeoTitle,
        TermsSeoDescription = entity.TermsSeoDescription
    };

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "FinalProject.Tests", "wwwroot");
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
