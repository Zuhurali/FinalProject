using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class PartnerStripSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    [Required, MaxLength(120)]
    public string Eyebrow { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string HeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeadingHighlight { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PartnerStripItem
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string LogoPath { get; set; } = string.Empty;

    /// <summary>Optional absolute or app-relative link. Empty uses Cars Index filtered by make=Name.</summary>
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

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

public class FaqPageContent
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    [Required, MaxLength(120)] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Eyebrow { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string HeadingBefore { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string HeadingHighlight { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string BannerImagePath { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string SeoDescription { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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

public class TeamPageContent
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    [Required, MaxLength(120)] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Eyebrow { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string HeadingBefore { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string HeadingHighlight { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string SeoDescription { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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

public class DealerPageContent
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    [Required, MaxLength(120)] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string SeoDescription { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DealerPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "Page title")] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}

public class CalculatorPageContent
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    [Required, MaxLength(120)] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string IntroText { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string SeoDescription { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CalculatorPageContentEditViewModel
{
    [Required, MaxLength(120), Display(Name = "Breadcrumb title")] public string BreadcrumbTitle { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "Page title")] public string PageTitle { get; set; } = string.Empty;
    [Required, MaxLength(1000), Display(Name = "Intro text")] public string IntroText { get; set; } = string.Empty;
    [Required, MaxLength(160), Display(Name = "SEO title")] public string SeoTitle { get; set; } = string.Empty;
    [Required, MaxLength(320), Display(Name = "SEO description")] public string SeoDescription { get; set; } = string.Empty;
}
