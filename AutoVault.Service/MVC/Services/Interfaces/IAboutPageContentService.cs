using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IAboutPageContentService
{
    AboutPagePublicContent GetPublic();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        AboutPageContentEditViewModel model,
        AboutPageImageUploads uploads,
        CancellationToken cancellationToken = default);
}

public interface ICurrentAboutPageContent
{
    AboutPagePublicContent Current { get; }
}

public sealed class AboutPagePublicContent
{
    public string PageTitle { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
    public string BreadcrumbTitle { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string HeadingBefore { get; init; } = string.Empty;
    public string HeadingHighlight { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string ImageLeftPath { get; init; } = string.Empty;
    public string ImageRightPath { get; init; } = string.Empty;
    public string CallCtaLabel { get; init; } = string.Empty;
    public string ShowcaseEyebrow { get; init; } = string.Empty;
    public string ShowcaseTitle { get; init; } = string.Empty;
    public string ShowcaseDescription { get; init; } = string.Empty;
    public string Showcase1Name { get; init; } = string.Empty;
    public string Showcase1Descriptor { get; init; } = string.Empty;
    public string Showcase1ImagePath { get; init; } = string.Empty;
    public string Showcase2Name { get; init; } = string.Empty;
    public string Showcase2Descriptor { get; init; } = string.Empty;
    public string Showcase2ImagePath { get; init; } = string.Empty;
    public string Showcase3Name { get; init; } = string.Empty;
    public string Showcase3Descriptor { get; init; } = string.Empty;
    public string Showcase3ImagePath { get; init; } = string.Empty;
    public string Showcase4Name { get; init; } = string.Empty;
    public string Showcase4Descriptor { get; init; } = string.Empty;
    public string Showcase4ImagePath { get; init; } = string.Empty;
}
