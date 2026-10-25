namespace FinalProject.Commerce;

public static class CarDiscoveryRules
{
    public const string Today = "today";
    public const string Week = "week";
    public const string All = "all";
    public const int DefaultPageSize = 9;
    public const int MaxPageSize = 48;

    public static TimeZoneInfo DealershipZone { get; } = ResolveZone();

    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }
        else if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        return (page, pageSize);
    }

    public static bool TryNormalizePeriod(string? period, out string normalized)
    {
        normalized = period?.Trim().ToLowerInvariant() switch
        {
            null or "" or "all" or "alltime" or "all-time" => All,
            "today" => Today,
            "week" or "7" or "last7" or "last-7-days" => Week,
            _ => ""
        };
        return normalized.Length > 0;
    }

    public static DateTime? PeriodStartUtc(string period, DateTime utcNow)
    {
        if (!TryNormalizePeriod(period, out var normalized))
        {
            throw new ArgumentException("Unknown view period.", nameof(period));
        }

        if (normalized == All)
        {
            return null;
        }

        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, DealershipZone);
        var startLocal = normalized == Today
            ? local.Date
            : local.Date.AddDays(-6);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), DealershipZone);
    }

    public static DateTime LocalDate(DateTime viewedAtUtc)
    {
        var utc = viewedAtUtc.Kind == DateTimeKind.Utc
            ? viewedAtUtc
            : DateTime.SpecifyKind(viewedAtUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, DealershipZone).Date;
    }

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Baku", "Azerbaijan Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
