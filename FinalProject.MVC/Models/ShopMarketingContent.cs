using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ShopMarketingContent : BaseEntity
{
    public ShopMarketingContent()
    {
        Id = SingletonId;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int SingletonId = 1;
    [Required, MaxLength(200)]
    public string HeroEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string HeroTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string HeroText { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string HeroImagePath { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string HeroImageAlt { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string SeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string SeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CategoriesEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CategoriesHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CategoriesHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CatalogEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CatalogHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CatalogHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string CatalogIntro { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string WhyChooseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string WhyChooseHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string WhyChooseHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Why1Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Why1Description { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Why2Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Why2Description { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Why3Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Why3Description { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Why4Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Why4Description { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string VisitEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string VisitHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string VisitHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string VisitTip1Title { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string VisitTip2Title { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FaqSectionEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FaqSectionHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FaqSectionHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ContactFormTitle { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string BrandStripEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string BrandStripHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BrandStripHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string UsedCarsPromoTag { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string UsedCarsPromoTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string UsedCarsPromoImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string VisitTip1Meta { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string VisitTip2Meta { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string VisitTip1ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string VisitTip2ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeroCtaBrowseParts { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeroCtaBrowseCars { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeroCtaVisit { get; set; } = string.Empty;
}
