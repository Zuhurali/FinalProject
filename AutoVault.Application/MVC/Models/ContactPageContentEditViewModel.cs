using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public sealed class ContactPageContentEditViewModel
{
    [Required, MaxLength(160), Display(Name = "Page title")]
    public string PageTitle { get; set; } = string.Empty;

    [Required, MaxLength(160), Display(Name = "SEO title")]
    public string SeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320), Display(Name = "SEO description")]
    public string SeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Form title")]
    public string FormTitle { get; set; } = string.Empty;

    [Required, MaxLength(500), Display(Name = "Form intro")]
    public string FormIntro { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Location card title")]
    public string LocationCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Phone card title")]
    public string PhoneCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(80), Display(Name = "Email card title")]
    public string EmailCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(1000), Display(Name = "Map embed URL")]
    public string MapEmbedUrl { get; set; } = string.Empty;

    [Required, MaxLength(300), Display(Name = "Map iframe title")]
    public string MapTitle { get; set; } = string.Empty;
}
