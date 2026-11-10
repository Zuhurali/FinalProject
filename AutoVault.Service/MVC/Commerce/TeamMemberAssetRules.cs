namespace FinalProject.MVC.Commerce;

public static class TeamMemberAssetRules
{
    public const string UploadFolderWeb = "~/uploads/team";
    public const string UploadFolderRelative = "uploads/team";

    private static readonly string[] AllowedPathPrefixes =
    [
        "/uploads/team/",
        "/assets/images/team/"
    ];

    public static string? ValidateStoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Choose a team photo.";
        }

        return CmsAssetUpload.IsSafeStoredPath(path, AllowedPathPrefixes)
            ? null
            : "That team photo path is not allowed.";
    }

    public static string CoalescePath(string? path, string fallback) =>
        CmsAssetUpload.CoalescePath(path, fallback, AllowedPathPrefixes);
}
