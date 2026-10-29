using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinalProject.MVC.Models;

public sealed class AboutPageContentEditViewModel
{
    [Required, MaxLength(160), Display(Name = "Page title")]
    public string PageTitle { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "SEO title")]
    public string SeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320), Display(Name = "SEO description")]
    public string SeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Breadcrumb title")]
    public string BreadcrumbTitle { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Eyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Heading (before highlight)")]
    public string HeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Heading highlight")]
    public string HeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageLeftPath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageRightPath { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Call CTA label")]
    public string CallCtaLabel { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Showcase eyebrow")]
    public string ShowcaseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Showcase title")]
    public string ShowcaseTitle { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Showcase description")]
    public string ShowcaseDescription { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Showcase 1 name")]
    public string Showcase1Name { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Showcase 1 descriptor")]
    public string Showcase1Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Showcase 1 image path")]
    public string Showcase1ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Showcase 2 name")]
    public string Showcase2Name { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Showcase 2 descriptor")]
    public string Showcase2Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Showcase 2 image path")]
    public string Showcase2ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Showcase 3 name")]
    public string Showcase3Name { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Showcase 3 descriptor")]
    public string Showcase3Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Showcase 3 image path")]
    public string Showcase3ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Showcase 4 name")]
    public string Showcase4Name { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Showcase 4 descriptor")]
    public string Showcase4Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Showcase 4 image path")]
    public string Showcase4ImagePath { get; set; } = string.Empty;
}

public sealed class AboutPageImageUploads
{
    public IFormFile? ImageLeft { get; init; }
    public IFormFile? ImageRight { get; init; }
    public IFormFile? Showcase1Image { get; init; }
    public IFormFile? Showcase2Image { get; init; }
    public IFormFile? Showcase3Image { get; init; }
    public IFormFile? Showcase4Image { get; init; }
}

public sealed class AboutPageViewModel
{
    public AboutPagePublicContent Content { get; init; } = new();
    public IReadOnlyList<LuxuryShowcaseBrand> Showcase { get; init; } = Array.Empty<LuxuryShowcaseBrand>();
}
