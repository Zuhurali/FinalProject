namespace FinalProject.MVC.Commerce;

/// <summary>
/// Seat count used by the Cars Two sidebar and cards.
/// Uses only the structured <c>Seats</c> value (1–12).
/// Description text, door counts, and photo counts are never treated as seats.
/// </summary>
public static class CarsTwoSeatCount
{
    public static int? Resolve(int storedSeats, string? description = null)
    {
        _ = description;
        return storedSeats is >= 1 and <= 12 ? storedSeats : null;
    }

    public static bool Matches(int storedSeats, string? description, IReadOnlyCollection<int> selectedSeats)
    {
        if (selectedSeats == null || selectedSeats.Count == 0)
        {
            return true;
        }

        var seatCount = Resolve(storedSeats, description);
        return seatCount.HasValue && selectedSeats.Contains(seatCount.Value);
    }

    /// <summary>
    /// Builds seat-count facet options for the Cars Two sidebar.
    /// When <paramref name="selectedMakes"/> is empty, the full catalog is used.
    /// When one or more makes are provided, only those makes are counted.
    /// Unknown/null seat values are omitted. The seat filter itself is not applied here.
    /// </summary>
    public static IReadOnlyList<(int Seats, int Count)> FacetOptions(
        IEnumerable<(string Make, int StoredSeats)> cars,
        IReadOnlyCollection<string>? selectedMakes = null)
    {
        ArgumentNullException.ThrowIfNull(cars);

        var scoped = cars;
        if (selectedMakes is { Count: > 0 })
        {
            var makes = new HashSet<string>(
                selectedMakes.Where(make => !string.IsNullOrWhiteSpace(make)),
                StringComparer.OrdinalIgnoreCase);
            if (makes.Count > 0)
            {
                scoped = cars.Where(car => makes.Contains(car.Make));
            }
        }

        return scoped
            .Select(car => Resolve(car.StoredSeats))
            .Where(seat => seat.HasValue)
            .GroupBy(seat => seat!.Value)
            .OrderBy(group => group.Key)
            .Select(group => (group.Key, group.Count()))
            .ToList();
    }
}
