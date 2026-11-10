namespace FinalProject.Commerce;

/// <summary>
/// Status values used for public discovery/catalog surfaces (Explore, suggestions, sold archive).
/// Draft stays private. Admin list endpoints keep an explicit optional Status filter.
/// </summary>
public static class CarPublicListing
{
    public const string ActiveStatus = "Active";
    public const string SoldStatus = "Sold";

    public static bool IsPubliclyListed(string? status) =>
        string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, SoldStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Anonymous catalog queries may request Active (default) or Sold. Draft is never allowed.
    /// </summary>
    public static string NormalizeAnonymousListStatus(string? requestedStatus)
    {
        if (string.Equals(requestedStatus, SoldStatus, StringComparison.OrdinalIgnoreCase))
        {
            return SoldStatus;
        }

        return ActiveStatus;
    }
}
