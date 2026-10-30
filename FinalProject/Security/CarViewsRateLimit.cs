namespace FinalProject.Security;

/// <summary>
/// Rate limit for anonymous <c>POST api/cars/{id}/views</c>.
/// Partitioned by client IP. MVC calls the API from one host IP, so the
/// permit budget must cover normal site traffic, not a single browser tab.
/// </summary>
public static class CarViewsRateLimit
{
    public const string PolicyName = "car-views";

    public const int PermitLimitPerMinute = 120;
}
