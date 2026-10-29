using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FinalProject.MVC.Models;

public sealed class ShopMarketingContentEditViewModel
{
    [Required, MaxLength(200), Display(Name = "Hero eyebrow")]
    public string HeroEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Hero title")]
    public string HeroTitle { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Hero text")]
    public string HeroText { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string HeroImagePath { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Hero image alt")]
    public string HeroImageAlt { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "SEO title")]
    public string SeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320), Display(Name = "SEO description")]
    public string SeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Categories eyebrow")]
    public string CategoriesEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Categories heading (before)")]
    public string CategoriesHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Categories heading highlight")]
    public string CategoriesHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Catalog eyebrow")]
    public string CatalogEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Catalog heading (before)")]
    public string CatalogHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Catalog heading highlight")]
    public string CatalogHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(1000), Display(Name = "Catalog intro")]
    public string CatalogIntro { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why Choose eyebrow")]
    public string WhyChooseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why Choose heading (before)")]
    public string WhyChooseHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Why Choose heading highlight")]
    public string WhyChooseHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why card 1 title")]
    public string Why1Title { get; set; } = string.Empty;

    [Required, MaxLength(400), Display(Name = "Why card 1 description")]
    public string Why1Description { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why card 2 title")]
    public string Why2Title { get; set; } = string.Empty;

    [Required, MaxLength(400), Display(Name = "Why card 2 description")]
    public string Why2Description { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why card 3 title")]
    public string Why3Title { get; set; } = string.Empty;

    [Required, MaxLength(400), Display(Name = "Why card 3 description")]
    public string Why3Description { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Why card 4 title")]
    public string Why4Title { get; set; } = string.Empty;

    [Required, MaxLength(400), Display(Name = "Why card 4 description")]
    public string Why4Description { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Visit eyebrow")]
    public string VisitEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Visit heading (before)")]
    public string VisitHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Visit heading highlight")]
    public string VisitHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(300), Display(Name = "Visit tip 1 title")]
    public string VisitTip1Title { get; set; } = string.Empty;

    [Required, MaxLength(300), Display(Name = "Visit tip 2 title")]
    public string VisitTip2Title { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "FAQ section eyebrow")]
    public string FaqSectionEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "FAQ heading (before)")]
    public string FaqSectionHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "FAQ heading highlight")]
    public string FaqSectionHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Contact form title")]
    public string ContactFormTitle { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Brand strip eyebrow")]
    public string BrandStripEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Brand strip heading (before)")]
    public string BrandStripHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Brand strip heading highlight")]
    public string BrandStripHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Used cars promo tag")]
    public string UsedCarsPromoTag { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Used cars promo title")]
    public string UsedCarsPromoTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string UsedCarsPromoImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Visit tip 1 meta")]
    public string VisitTip1Meta { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Visit tip 2 meta")]
    public string VisitTip2Meta { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string VisitTip1ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string VisitTip2ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Hero CTA: browse parts")]
    public string HeroCtaBrowseParts { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Hero CTA: browse cars")]
    public string HeroCtaBrowseCars { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Hero CTA: visit")]
    public string HeroCtaVisit { get; set; } = string.Empty;
}

public sealed class ShopMarketingImageUploads
{
    public IFormFile? HeroImage { get; init; }
    public IFormFile? UsedCarsPromoImage { get; init; }
    public IFormFile? VisitTip1Image { get; init; }
    public IFormFile? VisitTip2Image { get; init; }
}
