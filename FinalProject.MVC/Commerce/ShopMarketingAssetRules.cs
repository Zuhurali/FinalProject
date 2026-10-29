namespace FinalProject.MVC.Commerce;

public static class ShopMarketingAssetRules
{
    public const string UploadFolderWeb = "~/uploads/shop";
    public const string UploadFolderRelative = "uploads/shop";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/shop/",
        "/assets/images/catalog/hero/",
        "/assets/images/blog/",
        "/assets/images/cta/"
    ];

    public static string? ValidateStoredPath(string? path) =>
        CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes) ? null : "That shop marketing image path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}
