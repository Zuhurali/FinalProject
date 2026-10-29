using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IContactPageContentService
{
    ContactPagePublicContent GetPublic();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        ContactPageContentEditViewModel model,
        CancellationToken cancellationToken = default);
}

public interface ICurrentContactPageContent
{
    ContactPagePublicContent Current { get; }
}

public sealed class ContactPagePublicContent
{
    public string PageTitle { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
    public string FormTitle { get; init; } = string.Empty;
    public string FormIntro { get; init; } = string.Empty;
    public string LocationCardTitle { get; init; } = string.Empty;
    public string PhoneCardTitle { get; init; } = string.Empty;
    public string EmailCardTitle { get; init; } = string.Empty;
    public string MapEmbedUrl { get; init; } = string.Empty;
    public string MapTitle { get; init; } = string.Empty;
}
