using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IPartnerStripService
{
    PartnerStripPublicSettings GetSettings();
    IReadOnlyList<PartnerStripPublicItem> GetPublishedItems();
    string? FindLogoPathByName(string? name);
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SaveSettingsAsync(PartnerStripSettingsEditViewModel model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerStripItem>> GetAllItemsAsync(CancellationToken cancellationToken = default);
    Task<PartnerStripItem?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, int? Id)> CreateItemAsync(PartnerStripItemEditViewModel model, IFormFile? logo, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> UpdateItemAsync(PartnerStripItemEditViewModel model, IFormFile? logo, CancellationToken cancellationToken = default);
    Task<bool> DeleteItemAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SetItemPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> MoveItemAsync(int id, int direction, CancellationToken cancellationToken = default);
}

public interface ICurrentPartnerStrip
{
    PartnerStripPublicSettings Settings { get; }
    IReadOnlyList<PartnerStripPublicItem> Items { get; }
}

public sealed class PartnerStripPublicSettings
{
    public string Eyebrow { get; init; } = string.Empty;
    public string HeadingBefore { get; init; } = string.Empty;
    public string HeadingHighlight { get; init; } = string.Empty;
}

public sealed class PartnerStripPublicItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LogoPath { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
}

public interface IFaqPageContentService
{
    FaqPagePublicContent GetPublic();
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SaveAsync(FaqPageContentEditViewModel model, IFormFile? banner, CancellationToken cancellationToken = default);
}

public interface ICurrentFaqPageContent
{
    FaqPagePublicContent Current { get; }
}

public sealed class FaqPagePublicContent
{
    public string BreadcrumbTitle { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string HeadingBefore { get; init; } = string.Empty;
    public string HeadingHighlight { get; init; } = string.Empty;
    public string BannerImagePath { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
}

public interface ITeamPageContentService
{
    TeamPagePublicContent GetPublic();
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SaveAsync(TeamPageContentEditViewModel model, CancellationToken cancellationToken = default);
}

public interface ICurrentTeamPageContent
{
    TeamPagePublicContent Current { get; }
}

public sealed class TeamPagePublicContent
{
    public string BreadcrumbTitle { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string HeadingBefore { get; init; } = string.Empty;
    public string HeadingHighlight { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
}

public interface IDealerPageContentService
{
    DealerPagePublicContent GetPublic();
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SaveAsync(DealerPageContentEditViewModel model, CancellationToken cancellationToken = default);
}

public interface ICurrentDealerPageContent
{
    DealerPagePublicContent Current { get; }
}

public sealed class DealerPagePublicContent
{
    public string BreadcrumbTitle { get; init; } = string.Empty;
    public string PageTitle { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
}

public interface ICalculatorPageContentService
{
    CalculatorPagePublicContent GetPublic();
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> SaveAsync(CalculatorPageContentEditViewModel model, CancellationToken cancellationToken = default);
}

public interface ICurrentCalculatorPageContent
{
    CalculatorPagePublicContent Current { get; }
}

public sealed class CalculatorPagePublicContent
{
    public string BreadcrumbTitle { get; init; } = string.Empty;
    public string PageTitle { get; init; } = string.Empty;
    public string IntroText { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
}
