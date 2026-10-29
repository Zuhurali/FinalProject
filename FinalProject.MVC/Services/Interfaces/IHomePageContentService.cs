using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IHomePageContentService
{
    HomePagePublicContent GetPublic();

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(
        HomePageContentEditViewModel model,
        HomePageImageUploads uploads,
        CancellationToken cancellationToken = default);
}

public interface ICurrentHomePageContent
{
    HomePagePublicContent Current { get; }
}

public sealed class HomePagePublicContent
{
    public string HeroTitle { get; init; } = string.Empty;
    public string HeroHighlight { get; init; } = string.Empty;
    public string HeroImagePath { get; init; } = string.Empty;
    public string HeroImageAlt { get; init; } = string.Empty;

    public string CategoriesEyebrow { get; init; } = string.Empty;
    public string CategoriesHeadingBefore { get; init; } = string.Empty;
    public string CategoriesHeadingHighlight { get; init; } = string.Empty;
    public string CategoriesHeadingAfter { get; init; } = string.Empty;

    public string PortfolioEyebrow { get; init; } = string.Empty;
    public string PortfolioHeadingBefore { get; init; } = string.Empty;
    public string PortfolioHeadingHighlight { get; init; } = string.Empty;

    public string CampaignsEyebrow { get; init; } = string.Empty;
    public string CampaignsHeadingBefore { get; init; } = string.Empty;
    public string CampaignsHeadingHighlight { get; init; } = string.Empty;

    public string AboutEyebrow { get; init; } = string.Empty;
    public string AboutHeadingBefore { get; init; } = string.Empty;
    public string AboutHeadingHighlight { get; init; } = string.Empty;
    public string AboutBody { get; init; } = string.Empty;
    public string AboutCtaLabel { get; init; } = string.Empty;
    public string AboutImageMainPath { get; init; } = string.Empty;
    public string AboutImageFloat1Path { get; init; } = string.Empty;
    public string AboutImageFloat2Path { get; init; } = string.Empty;

    public string BrandsEyebrow { get; init; } = string.Empty;
    public string BrandsHeadingBefore { get; init; } = string.Empty;
    public string BrandsHeadingHighlight { get; init; } = string.Empty;

    public string WhyChooseEyebrow { get; init; } = string.Empty;
    public string WhyChooseHeadingBefore { get; init; } = string.Empty;
    public string WhyChooseHeadingHighlight { get; init; } = string.Empty;
    public string WhyChooseBackgroundPath { get; init; } = string.Empty;

    public string Feature1Title { get; init; } = string.Empty;
    public string Feature1Description { get; init; } = string.Empty;
    public string Feature1IconPath { get; init; } = string.Empty;
    public string Feature2Title { get; init; } = string.Empty;
    public string Feature2Description { get; init; } = string.Empty;
    public string Feature2IconPath { get; init; } = string.Empty;
    public string Feature3Title { get; init; } = string.Empty;
    public string Feature3Description { get; init; } = string.Empty;
    public string Feature3IconPath { get; init; } = string.Empty;
    public string Feature4Title { get; init; } = string.Empty;
    public string Feature4Description { get; init; } = string.Empty;
    public string Feature4IconPath { get; init; } = string.Empty;

    public string FeaturedEyebrow { get; init; } = string.Empty;
    public string FeaturedHeadingBefore { get; init; } = string.Empty;
    public string FeaturedHeadingHighlight { get; init; } = string.Empty;
}
