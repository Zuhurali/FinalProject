using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public sealed class LegalPageContentEditViewModel
{
    [Required, MaxLength(160), Display(Name = "Privacy title")]
    public string PrivacyTitle { get; set; } = string.Empty;

    [Required, MaxLength(8000), Display(Name = "Privacy body")]
    public string PrivacyBody { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "Privacy SEO title")]
    public string PrivacySeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320), Display(Name = "Privacy SEO description")]
    public string PrivacySeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "Terms title")]
    public string TermsTitle { get; set; } = string.Empty;

    [Required, MaxLength(8000), Display(Name = "Terms body")]
    public string TermsBody { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "Terms SEO title")]
    public string TermsSeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320), Display(Name = "Terms SEO description")]
    public string TermsSeoDescription { get; set; } = string.Empty;
}
