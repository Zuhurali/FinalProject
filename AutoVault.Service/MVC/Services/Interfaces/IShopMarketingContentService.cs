using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IShopMarketingContentService
{
    ShopMarketingPublicContent GetPublic();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        ShopMarketingContentEditViewModel model,
        ShopMarketingImageUploads uploads,
        CancellationToken cancellationToken = default);
}

public interface ICurrentShopMarketingContent
{
    ShopMarketingPublicContent Current { get; }
}

public sealed class ShopMarketingPublicContent
{
    public string HeroEyebrow { get; init; } = string.Empty;
    public string HeroTitle { get; init; } = string.Empty;
    public string HeroText { get; init; } = string.Empty;
    public string HeroImagePath { get; init; } = string.Empty;
    public string HeroImageAlt { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
    public string CategoriesEyebrow { get; init; } = string.Empty;
    public string CategoriesHeadingBefore { get; init; } = string.Empty;
    public string CategoriesHeadingHighlight { get; init; } = string.Empty;
    public string CatalogEyebrow { get; init; } = string.Empty;
    public string CatalogHeadingBefore { get; init; } = string.Empty;
    public string CatalogHeadingHighlight { get; init; } = string.Empty;
    public string CatalogIntro { get; init; } = string.Empty;
    public string WhyChooseEyebrow { get; init; } = string.Empty;
    public string WhyChooseHeadingBefore { get; init; } = string.Empty;
    public string WhyChooseHeadingHighlight { get; init; } = string.Empty;
    public string Why1Title { get; init; } = string.Empty;
    public string Why1Description { get; init; } = string.Empty;
    public string Why2Title { get; init; } = string.Empty;
    public string Why2Description { get; init; } = string.Empty;
    public string Why3Title { get; init; } = string.Empty;
    public string Why3Description { get; init; } = string.Empty;
    public string Why4Title { get; init; } = string.Empty;
    public string Why4Description { get; init; } = string.Empty;
    public string Why1IconPath { get; init; } = string.Empty;
    public string Why2IconPath { get; init; } = string.Empty;
    public string Why3IconPath { get; init; } = string.Empty;
    public string Why4IconPath { get; init; } = string.Empty;
    public string VisitEyebrow { get; init; } = string.Empty;
    public string VisitHeadingBefore { get; init; } = string.Empty;
    public string VisitHeadingHighlight { get; init; } = string.Empty;
    public string VisitTip1Title { get; init; } = string.Empty;
    public string VisitTip2Title { get; init; } = string.Empty;
    public string FaqSectionEyebrow { get; init; } = string.Empty;
    public string FaqSectionHeadingBefore { get; init; } = string.Empty;
    public string FaqSectionHeadingHighlight { get; init; } = string.Empty;
    public string ContactFormTitle { get; init; } = string.Empty;
    public string BrandStripEyebrow { get; init; } = string.Empty;
    public string BrandStripHeadingBefore { get; init; } = string.Empty;
    public string BrandStripHeadingHighlight { get; init; } = string.Empty;
    public string UsedCarsPromoTag { get; init; } = string.Empty;
    public string UsedCarsPromoTitle { get; init; } = string.Empty;
    public string UsedCarsPromoImagePath { get; init; } = string.Empty;
    public string VisitTip1Meta { get; init; } = string.Empty;
    public string VisitTip2Meta { get; init; } = string.Empty;
    public string VisitTip1ImagePath { get; init; } = string.Empty;
    public string VisitTip2ImagePath { get; init; } = string.Empty;
    public string HeroCtaBrowseParts { get; init; } = string.Empty;
    public string HeroCtaBrowseCars { get; init; } = string.Empty;
    public string HeroCtaVisit { get; init; } = string.Empty;
}
