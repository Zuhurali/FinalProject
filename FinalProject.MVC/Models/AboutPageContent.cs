using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class AboutPageContent : BaseEntity
{
    public AboutPageContent()
    {
        Id = SingletonId;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int SingletonId = 1;
    [Required, MaxLength(160)]
    public string PageTitle { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string SeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string SeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string BreadcrumbTitle { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Eyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string HeadingBefore { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HeadingHighlight { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageLeftPath { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ImageRightPath { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string CallCtaLabel { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string ShowcaseEyebrow { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ShowcaseTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ShowcaseDescription { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Showcase1Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Showcase1Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Showcase1ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Showcase2Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Showcase2Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Showcase2ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Showcase3Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Showcase3Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Showcase3ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Showcase4Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Showcase4Descriptor { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Showcase4ImagePath { get; set; } = string.Empty;
}
