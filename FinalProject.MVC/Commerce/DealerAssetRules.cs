namespace FinalProject.MVC.Commerce;

public static class DealerAssetRules
{
    public const string UploadFolderWeb = "~/uploads/dealers";
    public const string UploadFolderRelative = "uploads/dealers";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/dealers/",
        "/assets/images/brand/"
    ];

    public static string? ValidateStoredPath(string? path) =>
        CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes) ? null : "That dealer image path is not allowed.";

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}
