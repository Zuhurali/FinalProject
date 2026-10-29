using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

/// <summary>Singleton site identity and contact settings (one row, Id = 1).</summary>
public class SiteSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    [Required, MaxLength(120)]
    public string SiteName { get; set; } = "AutoVault";

    [Required, MaxLength(40)]
    public string DisplayPhone { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string E164Phone { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string WhatsAppDigits { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string MapsQuery { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LogoPath { get; set; } = "~/assets/images/logo/logo.svg";

    [MaxLength(500)]
    public string FooterLogoPath { get; set; } = "~/assets/images/logo/footer-logo.svg";

    [MaxLength(500)]
    public string FaviconPath { get; set; } = "~/fav.svg";

    [Required, MaxLength(500)]
    public string FooterText { get; set; } =
        "Your trusted partner for quality vehicles and exceptional service. Find your perfect car with confidence at AutoVault.";

    [Required, MaxLength(200)]
    public string CopyrightText { get; set; } = "Copyright © {year} AutoVault. All rights reserved.";

    [MaxLength(300)]
    public string? FacebookUrl { get; set; }

    [MaxLength(300)]
    public string? InstagramUrl { get; set; }

    [MaxLength(300)]
    public string? YoutubeUrl { get; set; }

    [MaxLength(300)]
    public string? TwitterUrl { get; set; }

    [Required, MaxLength(160)]
    public string DefaultSeoTitle { get; set; } = "AutoVault Car Dealer";

    [Required, MaxLength(320)]
    public string DefaultMetaDescription { get; set; } =
        "Your trusted source for expert automotive services and quality cars. Find your perfect vehicle with our extensive inventory and professional service.";

    [Required, MaxLength(200)]
    public string CallCtaHeadline { get; set; } = "If you have any questions Please Call.";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
