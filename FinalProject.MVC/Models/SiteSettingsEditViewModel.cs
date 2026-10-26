using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public sealed class SiteSettingsEditViewModel
{
    [Required, MaxLength(120)]
    [Display(Name = "Site name")]
    public string SiteName { get; set; } = "AutoVault";

    [Required, MaxLength(40)]
    [Display(Name = "Display phone")]
    public string DisplayPhone { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Display(Name = "E.164 phone")]
    public string E164Phone { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Display(Name = "WhatsApp digits")]
    public string WhatsAppDigits { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    [Display(Name = "Google Maps query")]
    public string MapsQuery { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoPath { get; set; } = "~/assets/images/logo/logo.svg";

    [MaxLength(500)]
    public string FooterLogoPath { get; set; } = "~/assets/images/logo/footer-logo.svg";

    [MaxLength(500)]
    public string FaviconPath { get; set; } = "~/fav.svg";

    [Required, MaxLength(500)]
    [Display(Name = "Footer text")]
    public string FooterText { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Copyright text")]
    public string CopyrightText { get; set; } = string.Empty;

    [MaxLength(300)]
    [Display(Name = "Facebook URL")]
    public string? FacebookUrl { get; set; }

    [MaxLength(300)]
    [Display(Name = "Instagram URL")]
    public string? InstagramUrl { get; set; }

    [MaxLength(300)]
    [Display(Name = "YouTube URL")]
    public string? YoutubeUrl { get; set; }

    [MaxLength(300)]
    [Display(Name = "Twitter / X URL")]
    public string? TwitterUrl { get; set; }

    [Required, MaxLength(160)]
    [Display(Name = "Default SEO title")]
    public string DefaultSeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    [Display(Name = "Default meta description")]
    public string DefaultMetaDescription { get; set; } = string.Empty;
}
