using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Public catalog helpers. Active is the default storefront; Sold is the public archive.
/// Draft stays private. Admin and "mine" endpoints keep their own status filters.
/// </summary>
public static class CarPublicListing
{
    public const string ActiveStatus = "Active";
    public const string SoldStatus = "Sold";

    public static bool IsPubliclyListed(string? status) =>
        string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, SoldStatus, StringComparison.OrdinalIgnoreCase);

    public static CarApiQuery WithActiveStatus(CarApiQuery query)
    {
        query.Status = ActiveStatus;
        return query;
    }

    public static CarApiQuery CreateActiveQuery(int page = 1, int pageSize = CarApiQuery.MaxPageSize) =>
        WithActiveStatus(new CarApiQuery
        {
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? CarApiQuery.MaxPageSize : pageSize
        });

    public static CarApiQuery CreateSoldQuery(int page = 1, int pageSize = CarApiQuery.MaxPageSize) =>
        new()
        {
            Status = SoldStatus,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? CarApiQuery.MaxPageSize : pageSize
        };
}
