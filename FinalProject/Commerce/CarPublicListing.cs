namespace FinalProject.Commerce;

/// <summary>
/// Status value used for public discovery/catalog surfaces (Explore, suggestions).
/// Admin list endpoints keep an explicit optional Status filter and are not forced here.
/// </summary>
public static class CarPublicListing
{
    public const string ActiveStatus = "Active";

    public static bool IsPubliclyListed(string? status) =>
        string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase);
}
