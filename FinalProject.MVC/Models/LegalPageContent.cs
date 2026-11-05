using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class LegalPageContent : BaseEntity
{
    public LegalPageContent()
    {
        Id = SingletonId;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int SingletonId = 1;
    [Required, MaxLength(160)]
    public string PrivacyTitle { get; set; } = string.Empty;

    [Required, MaxLength(8000)]
    public string PrivacyBody { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string PrivacySeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string PrivacySeoDescription { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string TermsTitle { get; set; } = string.Empty;

    [Required, MaxLength(8000)]
    public string TermsBody { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string TermsSeoTitle { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string TermsSeoDescription { get; set; } = string.Empty;
}
