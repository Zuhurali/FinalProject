using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FinalProject.MVC.Models;

public sealed class HomePageContentEditViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Hero title")]
    public string HeroTitle { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Hero highlight")]
    public string HeroHighlight { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string HeroImagePath { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Hero image alt text")]
    public string HeroImageAlt { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Categories eyebrow")]
    public string CategoriesEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Categories heading (before)")]
    public string CategoriesHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Categories heading highlight")]
    public string CategoriesHeadingHighlight { get; set; } = string.Empty;

    [MaxLength(80)]
    [Display(Name = "Categories heading (after)")]
    public string CategoriesHeadingAfter { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Portfolio eyebrow")]
    public string PortfolioEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Portfolio heading (before)")]
    public string PortfolioHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Portfolio heading highlight")]
    public string PortfolioHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Campaigns eyebrow")]
    public string CampaignsEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Campaigns heading (before)")]
    public string CampaignsHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Campaigns heading highlight")]
    public string CampaignsHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "About eyebrow")]
    public string AboutEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "About heading (before)")]
    public string AboutHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "About heading highlight")]
    public string AboutHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    [Display(Name = "About body")]
    public string AboutBody { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "About CTA label")]
    public string AboutCtaLabel { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageMainPath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageFloat1Path { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AboutImageFloat2Path { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Brands eyebrow")]
    public string BrandsEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Brands heading (before)")]
    public string BrandsHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Brands heading highlight")]
    public string BrandsHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Why Choose Us eyebrow")]
    public string WhyChooseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Why Choose Us heading (before)")]
    public string WhyChooseHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Why Choose Us heading highlight")]
    public string WhyChooseHeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string WhyChooseBackgroundPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Feature 1 title")]
    public string Feature1Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    [Display(Name = "Feature 1 description")]
    public string Feature1Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature1IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Feature 2 title")]
    public string Feature2Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    [Display(Name = "Feature 2 description")]
    public string Feature2Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature2IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Feature 3 title")]
    public string Feature3Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    [Display(Name = "Feature 3 description")]
    public string Feature3Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature3IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Feature 4 title")]
    public string Feature4Title { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    [Display(Name = "Feature 4 description")]
    public string Feature4Description { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Feature4IconPath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Featured Cars eyebrow")]
    public string FeaturedEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    [Display(Name = "Featured Cars heading (before)")]
    public string FeaturedHeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    [Display(Name = "Featured Cars heading highlight")]
    public string FeaturedHeadingHighlight { get; set; } = string.Empty;
}

public sealed class HomePageImageUploads
{
    public IFormFile? HeroImage { get; set; }
    public IFormFile? AboutImageMain { get; set; }
    public IFormFile? AboutImageFloat1 { get; set; }
    public IFormFile? AboutImageFloat2 { get; set; }
    public IFormFile? WhyChooseBackground { get; set; }
    public IFormFile? Feature1Icon { get; set; }
    public IFormFile? Feature2Icon { get; set; }
    public IFormFile? Feature3Icon { get; set; }
    public IFormFile? Feature4Icon { get; set; }
}
