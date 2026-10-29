using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

/// <summary>Singleton homepage marketing content (one row, Id = 1).</summary>
public class HomePageContent
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    [Required, MaxLength(200)]
    public string HeroTitle { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeroHighlight { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string HeroImagePath { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string HeroImageAlt { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CategoriesEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CategoriesHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CategoriesHeadingHighlight { get; set; } = string.Empty;

    [MaxLength(80)]
    public string CategoriesHeadingAfter { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string PortfolioEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string PortfolioHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string PortfolioHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CampaignsEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CampaignsHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CampaignsHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string AboutEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AboutHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string AboutHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string AboutBody { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string AboutCtaLabel { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageMainPath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageFloat1Path { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageFloat2Path { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string BrandsEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string BrandsHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string BrandsHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string WhyChooseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string WhyChooseHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string WhyChooseHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string WhyChooseBackgroundPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Feature1Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Feature1Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature1IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Feature2Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Feature2Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature2IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Feature3Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Feature3Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature3IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Feature4Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string Feature4Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature4IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FeaturedEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FeaturedHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FeaturedHeadingHighlight { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
