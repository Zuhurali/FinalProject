namespace FinalProject.MVC.Commerce;

public static class PartnerStripDefaults
{
    public const string Eyebrow = "Partners";
    public const string HeadingBefore = "Browse by";
    public const string HeadingHighlight = "Company";

    public static readonly (string Name, string LogoPath)[] Items =
    [
        ("Jeep", "~/assets/images/brand/01-w.svg"),
        ("Ford", "~/assets/images/brand/02.svg"),
        ("Mazda", "~/assets/images/brand/03.svg"),
        ("BMW", "~/assets/images/brand/04.svg"),
        ("Honda", "~/assets/images/brand/05.svg"),
        ("Audi", "~/assets/images/brand/06.svg"),
        ("Nissan", "~/assets/images/brand/07.svg")
    ];
}

public static class PartnerStripAssetRules
{
    public const string UploadFolderWeb = "~/uploads/partners";
    public const string UploadFolderRelative = "uploads/partners";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/partners/",
        "/assets/images/brand/"
    ];

    public static string? ValidateStoredPath(string? path) =>
        CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes) ? null : "That partner logo path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}

public static class FaqPageContentDefaults
{
    public const string BreadcrumbTitle = "Faq";
    public const string Eyebrow = "FAQ";
    public const string HeadingBefore = "Frequently Asked";
    public const string HeadingHighlight = "Question";
    public const string BannerImagePath = "~/assets/images/banner/faq.webp";
    public const string SeoTitle = "FAQ - AutoVault";
    public const string SeoDescription = "Answers about AutoVault store visits, inventory, and spare parts in Baku.";
}

public static class FaqPageAssetRules
{
    public const string UploadFolderWeb = "~/uploads/faq";
    public const string UploadFolderRelative = "uploads/faq";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/faq/",
        "/assets/images/banner/"
    ];

    public static string? ValidateStoredPath(string? path) =>
        CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes) ? null : "That FAQ banner path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}

public static class TeamPageContentDefaults
{
    public const string BreadcrumbTitle = "Team";
    public const string Eyebrow = "Our Team";
    public const string HeadingBefore = "Let's Meet Our";
    public const string HeadingHighlight = "Expert";
    public const string SeoTitle = "Team - AutoVault";
    public const string SeoDescription = "Meet the AutoVault team at the Baku showroom.";
}

public static class DealerPageContentDefaults
{
    public const string BreadcrumbTitle = "Dealer";
    public const string PageTitle = "Dealer List";
    public const string SeoTitle = "Dealer List - AutoVault";
    public const string SeoDescription = "Browse AutoVault dealer partners and showroom listings.";
}

public static class CalculatorPageContentDefaults
{
    public const string BreadcrumbTitle = "Calculator";
    public const string PageTitle = "Car Loan Calculator";
    public const string IntroText =
        "Estimate your monthly payment with a standard fixed-rate amortization formula. Enter numbers only; use a period for decimals.";
    public const string SeoTitle = "Calculator - AutoVault";
    public const string SeoDescription = "Estimate AutoVault car loan monthly payments with a fixed-rate amortization calculator.";
}
