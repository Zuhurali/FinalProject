using System.Text.RegularExpressions;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Seat count used only by the CarsTwo sidebar. A stored 1–9 wins.
/// Otherwise a description is used only when it contains exactly one
/// unambiguous "N seats" phrase, with N from 1 to 9.
/// </summary>
public static class CarsTwoSeatCount
{
    private static readonly Regex ExplicitSeats = new(
        @"\b([1-9]|1[0-2])\s+seats\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static int? Resolve(int storedSeats, string? description)
    {
        if (storedSeats is >= 1 and <= 9)
        {
            return storedSeats;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var matches = ExplicitSeats.Matches(description);
        if (matches.Count != 1
            || !int.TryParse(matches[0].Groups[1].Value, out var seats)
            || seats is < 1 or > 9)
        {
            return null;
        }

        return seats;
    }
}
