using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ContactPageContent : BaseEntity
{
    public ContactPageContent()
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
    public string FormTitle { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FormIntro { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LocationCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string PhoneCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string EmailCardTitle { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string MapEmbedUrl { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string MapTitle { get; set; } = string.Empty;
}
