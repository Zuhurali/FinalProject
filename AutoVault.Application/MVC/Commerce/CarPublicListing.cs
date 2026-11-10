using FinalProject.MVC.DTOs;

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

    public static CarApiQueryDto WithActiveStatus(CarApiQueryDto query)
    {
        query.Status = ActiveStatus;
        return query;
    }

    public static CarApiQueryDto CreateActiveQuery(int page = 1, int pageSize = CarApiQueryDto.MaxPageSize) =>
        WithActiveStatus(new CarApiQueryDto
        {
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? CarApiQueryDto.MaxPageSize : pageSize,
            // Newest listing first — same rule as Advanced Search "Latest".
            Sort = "latest"
        });

    public static CarApiQueryDto CreateSoldQuery(int page = 1, int pageSize = CarApiQueryDto.MaxPageSize) =>
        new()
        {
            Status = SoldStatus,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? CarApiQueryDto.MaxPageSize : pageSize,
            Sort = "latest"
        };
}
