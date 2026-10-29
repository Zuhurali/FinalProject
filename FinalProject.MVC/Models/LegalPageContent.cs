using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class LegalPageContent
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

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

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
