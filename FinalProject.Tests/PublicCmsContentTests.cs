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
public class PublicCmsContentTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_cms_controllers_require_Admin_or_SuperAdmin_and_antiforgery()
    {
        foreach (var controller in new[]
                 {
                     typeof(AdminAboutController),
                     typeof(AdminContactController),
                     typeof(AdminLegalController),
                     typeof(AdminShopMarketingController),
                     typeof(AdminTeamsController),
                     typeof(AdminDealersController)
                 })
        {
            Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
            var post = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.GetCustomAttribute<HttpPostAttribute>() != null);
            Assert.NotNull(post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }

    [Fact]
    public async Task About_contact_legal_shop_seed_and_save_drive_public_snapshots()
    {
        await using var db = CreateDb();
        var env = new TestEnvironment();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var about = new AboutPageContentService(db, cache, env);
        await about.EnsureSeedAsync();
        // Shared SQL Server DB: restore seed so prior suite runs do not leave mutated singletons.
        Assert.True((await about.SaveAsync(ToAboutEditFromSeed(), new AboutPageImageUploads())).Ok);
        cache.Remove(AboutPageContentService.CacheKey);
        Assert.Equal(AboutPageContentDefaults.Eyebrow, about.GetPublic().Eyebrow);

        var aboutEdit = ToAboutEdit(about.GetPublic());
        aboutEdit.Eyebrow = $"About {Guid.NewGuid():N}";
        var (aboutOk, aboutErr) = await about.SaveAsync(aboutEdit, new AboutPageImageUploads());
        Assert.True(aboutOk, aboutErr);
        cache.Remove(AboutPageContentService.CacheKey);
        Assert.Equal(aboutEdit.Eyebrow, about.GetPublic().Eyebrow);

        var contact = new ContactPageContentService(db, cache);
        await contact.EnsureSeedAsync();
        Assert.True((await contact.SaveAsync(ToContactEditFromSeed())).Ok);
        cache.Remove(ContactPageContentService.CacheKey);
        var contactPublic = contact.GetPublic();
        var contactEdit = ToContactEdit(contactPublic);
        contactEdit.FormTitle = $"Touch {Guid.NewGuid():N}";
        Assert.True((await contact.SaveAsync(contactEdit)).Ok);
        cache.Remove(ContactPageContentService.CacheKey);
        Assert.Equal(contactEdit.FormTitle, contact.GetPublic().FormTitle);

        var legal = new LegalPageContentService(db, cache);
        await legal.EnsureSeedAsync();
        Assert.Contains("privacy", legal.GetPublic().PrivacyBody, StringComparison.OrdinalIgnoreCase);

        var shop = new ShopMarketingContentService(db, cache, env);
        await shop.EnsureSeedAsync();
        Assert.True((await shop.SaveAsync(ToShopEditFromSeed(), new ShopMarketingImageUploads())).Ok);
        cache.Remove(ShopMarketingContentService.CacheKey);
        Assert.Equal(ShopMarketingContentDefaults.HeroTitle, shop.GetPublic().HeroTitle);
        Assert.Equal(ShopMarketingContentDefaults.CatalogIntro, shop.GetPublic().CatalogIntro);
        Assert.Equal(ShopMarketingContentDefaults.Why1Title, shop.GetPublic().Why1Title);

        Assert.True((await about.SaveAsync(ToAboutEditFromSeed(), new AboutPageImageUploads())).Ok);
        cache.Remove(AboutPageContentService.CacheKey);
        Assert.True((await contact.SaveAsync(ToContactEditFromSeed())).Ok);
        cache.Remove(ContactPageContentService.CacheKey);
        Assert.Equal(AboutPageContentDefaults.Eyebrow, about.GetPublic().Eyebrow);
    }

    private static AboutPageContentEditViewModel ToAboutEditFromSeed()
    {
        var seed = AboutPageContentService.CreateSeedEntity();
        return new AboutPageContentEditViewModel
        {
            PageTitle = seed.PageTitle,
            SeoTitle = seed.SeoTitle,
            SeoDescription = seed.SeoDescription,
            BreadcrumbTitle = seed.BreadcrumbTitle,
            Eyebrow = seed.Eyebrow,
            HeadingBefore = seed.HeadingBefore,
            HeadingHighlight = seed.HeadingHighlight,
            Body = seed.Body,
            ImageLeftPath = seed.ImageLeftPath,
            ImageRightPath = seed.ImageRightPath,
            CallCtaLabel = seed.CallCtaLabel,
            ShowcaseEyebrow = seed.ShowcaseEyebrow,
            ShowcaseTitle = seed.ShowcaseTitle,
            ShowcaseDescription = seed.ShowcaseDescription,
            Showcase1Name = seed.Showcase1Name,
            Showcase1Descriptor = seed.Showcase1Descriptor,
            Showcase1ImagePath = seed.Showcase1ImagePath,
            Showcase2Name = seed.Showcase2Name,
            Showcase2Descriptor = seed.Showcase2Descriptor,
            Showcase2ImagePath = seed.Showcase2ImagePath,
            Showcase3Name = seed.Showcase3Name,
            Showcase3Descriptor = seed.Showcase3Descriptor,
            Showcase3ImagePath = seed.Showcase3ImagePath,
            Showcase4Name = seed.Showcase4Name,
            Showcase4Descriptor = seed.Showcase4Descriptor,
            Showcase4ImagePath = seed.Showcase4ImagePath
        };
    }

    private static AboutPageContentEditViewModel ToAboutEdit(AboutPagePublicContent current) => new()
    {
        PageTitle = current.PageTitle,
        SeoTitle = current.SeoTitle,
        SeoDescription = current.SeoDescription,
        BreadcrumbTitle = current.BreadcrumbTitle,
        Eyebrow = current.Eyebrow,
        HeadingBefore = current.HeadingBefore,
        HeadingHighlight = current.HeadingHighlight,
        Body = current.Body,
        ImageLeftPath = current.ImageLeftPath,
        ImageRightPath = current.ImageRightPath,
        CallCtaLabel = current.CallCtaLabel,
        ShowcaseEyebrow = current.ShowcaseEyebrow,
        ShowcaseTitle = current.ShowcaseTitle,
        ShowcaseDescription = current.ShowcaseDescription,
        Showcase1Name = current.Showcase1Name,
        Showcase1Descriptor = current.Showcase1Descriptor,
        Showcase1ImagePath = current.Showcase1ImagePath,
        Showcase2Name = current.Showcase2Name,
        Showcase2Descriptor = current.Showcase2Descriptor,
        Showcase2ImagePath = current.Showcase2ImagePath,
        Showcase3Name = current.Showcase3Name,
        Showcase3Descriptor = current.Showcase3Descriptor,
        Showcase3ImagePath = current.Showcase3ImagePath,
        Showcase4Name = current.Showcase4Name,
        Showcase4Descriptor = current.Showcase4Descriptor,
        Showcase4ImagePath = current.Showcase4ImagePath
    };

    private static ContactPageContentEditViewModel ToContactEditFromSeed()
    {
        var seed = ContactPageContentService.CreateSeedEntity();
        return new ContactPageContentEditViewModel
        {
            PageTitle = seed.PageTitle,
            SeoTitle = seed.SeoTitle,
            SeoDescription = seed.SeoDescription,
            FormTitle = seed.FormTitle,
            FormIntro = seed.FormIntro,
            LocationCardTitle = seed.LocationCardTitle,
            PhoneCardTitle = seed.PhoneCardTitle,
            EmailCardTitle = seed.EmailCardTitle,
            MapEmbedUrl = seed.MapEmbedUrl,
            MapTitle = seed.MapTitle
        };
    }

    private static ContactPageContentEditViewModel ToContactEdit(ContactPagePublicContent current) => new()
    {
        PageTitle = current.PageTitle,
        SeoTitle = current.SeoTitle,
        SeoDescription = current.SeoDescription,
        FormTitle = current.FormTitle,
        FormIntro = current.FormIntro,
        LocationCardTitle = current.LocationCardTitle,
        PhoneCardTitle = current.PhoneCardTitle,
        EmailCardTitle = current.EmailCardTitle,
        MapEmbedUrl = current.MapEmbedUrl,
        MapTitle = current.MapTitle
    };

    private static ShopMarketingContentEditViewModel ToShopEditFromSeed()
    {
        var seed = ShopMarketingContentService.CreateSeedEntity();
        return new ShopMarketingContentEditViewModel
        {
            HeroEyebrow = seed.HeroEyebrow,
            HeroTitle = seed.HeroTitle,
            HeroText = seed.HeroText,
            HeroImagePath = seed.HeroImagePath,
            HeroImageAlt = seed.HeroImageAlt,
            SeoTitle = seed.SeoTitle,
            SeoDescription = seed.SeoDescription,
            CategoriesEyebrow = seed.CategoriesEyebrow,
            CategoriesHeadingBefore = seed.CategoriesHeadingBefore,
            CategoriesHeadingHighlight = seed.CategoriesHeadingHighlight,
            CatalogEyebrow = seed.CatalogEyebrow,
            CatalogHeadingBefore = seed.CatalogHeadingBefore,
            CatalogHeadingHighlight = seed.CatalogHeadingHighlight,
            CatalogIntro = seed.CatalogIntro,
            WhyChooseEyebrow = seed.WhyChooseEyebrow,
            WhyChooseHeadingBefore = seed.WhyChooseHeadingBefore,
            WhyChooseHeadingHighlight = seed.WhyChooseHeadingHighlight,
            Why1Title = seed.Why1Title,
            Why1Description = seed.Why1Description,
            Why2Title = seed.Why2Title,
            Why2Description = seed.Why2Description,
            Why3Title = seed.Why3Title,
            Why3Description = seed.Why3Description,
            Why4Title = seed.Why4Title,
            Why4Description = seed.Why4Description,
            VisitEyebrow = seed.VisitEyebrow,
            VisitHeadingBefore = seed.VisitHeadingBefore,
            VisitHeadingHighlight = seed.VisitHeadingHighlight,
            VisitTip1Title = seed.VisitTip1Title,
            VisitTip2Title = seed.VisitTip2Title,
            FaqSectionEyebrow = seed.FaqSectionEyebrow,
            FaqSectionHeadingBefore = seed.FaqSectionHeadingBefore,
            FaqSectionHeadingHighlight = seed.FaqSectionHeadingHighlight,
            ContactFormTitle = seed.ContactFormTitle,
            BrandStripEyebrow = seed.BrandStripEyebrow,
            BrandStripHeadingBefore = seed.BrandStripHeadingBefore,
            BrandStripHeadingHighlight = seed.BrandStripHeadingHighlight,
            UsedCarsPromoTag = seed.UsedCarsPromoTag,
            UsedCarsPromoTitle = seed.UsedCarsPromoTitle,
            UsedCarsPromoImagePath = seed.UsedCarsPromoImagePath,
            VisitTip1Meta = seed.VisitTip1Meta,
            VisitTip2Meta = seed.VisitTip2Meta,
            VisitTip1ImagePath = seed.VisitTip1ImagePath,
            VisitTip2ImagePath = seed.VisitTip2ImagePath,
            HeroCtaBrowseParts = seed.HeroCtaBrowseParts,
            HeroCtaBrowseCars = seed.HeroCtaBrowseCars,
            HeroCtaVisit = seed.HeroCtaVisit
        };
    }

    [Fact]
    public async Task Team_dealer_and_shop_faq_seeds_expose_published_lists()
    {
        await using var db = CreateDb();
        var env = new TestEnvironment();
        var teams = new TeamMemberService(db, env);
        var dealers = new DealerService(db, env);
        var faqs = new FaqService(db);

        await teams.EnsureSeedAsync();
        await dealers.EnsureSeedAsync();
        await faqs.EnsureSeedAsync();

        var publishedTeam = await teams.GetPublishedAsync();
        Assert.NotEmpty(publishedTeam);
        Assert.Contains(TeamCatalog.Members[0].Name, publishedTeam.Select(m => m.Name));

        var publishedDealers = await dealers.GetPublishedAsync();
        Assert.NotEmpty(publishedDealers);
        Assert.Contains(DealerCatalog.All[0].Name, publishedDealers.Select(d => d.Name));
        Assert.All(publishedDealers, dealer => Assert.False(string.IsNullOrWhiteSpace(dealer.FeatureList)));

        var shopFaqs = await faqs.GetShopPublishedAsync();
        Assert.Equal(5, shopFaqs.Count);
        Assert.All(shopFaqs, item => Assert.True(item.ShowOnShop));
    }

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
