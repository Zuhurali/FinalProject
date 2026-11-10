using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public sealed class PartnerStripSettingsEditViewModel
{
    [Required, MaxLength(120)]
    public string Eyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Heading (before)")]
    public string HeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Heading highlight")]
    public string HeadingHighlight { get; set; } = string.Empty;
}

public sealed class PartnerStripItemEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoPath { get; set; } = string.Empty;

    [MaxLength(500), Display(Name = "Custom link (optional)")]
    public string? LinkUrl { get; set; }

    [Range(0, 100000), Display(Name = "Sort order")]
    public int SortOrder { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;
}

public sealed class FaqPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Eyebrow { get; set; } = string.Empty;
    [Required, MaxLength(120), Display(Name = "Heading (before)")] public string HeadingBefore { get; set; } = string.Empty;
    [Required, MaxLength(80), Display(Name = "Heading highlight")] public string HeadingHighlight { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string BannerImagePath { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}

public sealed class TeamPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Eyebrow { get; set; } = string.Empty;
    [Required, MaxLength(120), Display(Name = "Heading (before)")] public string HeadingBefore { get; set; } = string.Empty;
    [Required, MaxLength(80), Display(Name = "Heading highlight")] public string HeadingHighlight { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}

public sealed class DealerPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "Page title")] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}

public sealed class CalculatorPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "Page title")] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(1000), Display(Name = "Intro text")] public string IntroText { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}
