namespace FinalProject.MVC.Commerce;

public static class AboutPageAssetRules
{
    public const string UploadFolderWeb = "~/uploads/about";
    public const string UploadFolderRelative = "uploads/about";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/about/",
        "/assets/images/about/",
        "/assets/images/catalog/sports-car/",
        "/assets/images/catalog/luxury/"
    ];

    public static string? ValidateStoredPath(string? path) =>
        CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes) ? null : "That about page image path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}
