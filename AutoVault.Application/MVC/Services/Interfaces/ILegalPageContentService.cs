using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface ILegalPageContentService
{
    LegalPagePublicContent GetPublic();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        LegalPageContentEditViewModel model,
        CancellationToken cancellationToken = default);
}

public interface ICurrentLegalPageContent
{
    LegalPagePublicContent Current { get; }
}

public sealed class LegalPagePublicContent
{
    public string PrivacyTitle { get; init; } = string.Empty;
    public string PrivacyBody { get; init; } = string.Empty;
    public string PrivacySeoTitle { get; init; } = string.Empty;
    public string PrivacySeoDescription { get; init; } = string.Empty;
    public string TermsTitle { get; init; } = string.Empty;
    public string TermsBody { get; init; } = string.Empty;
    public string TermsSeoTitle { get; init; } = string.Empty;
    public string TermsSeoDescription { get; init; } = string.Empty;
}
