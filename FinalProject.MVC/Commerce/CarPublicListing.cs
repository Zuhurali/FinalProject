using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Public catalog queries must only surface Active cars. Admin and "mine" endpoints
/// keep their own status filters and are not routed through this helper.
/// </summary>
public static class CarPublicListing
{
    public const string ActiveStatus = "Active";

    public static bool IsPubliclyListed(string? status) =>
        string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase);

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
}
