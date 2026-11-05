using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class CarsTwoSeatFilterTests
{
    [Theory]
    [InlineData(4, "ignored 5 seats", 4)]
    [InlineData(0, "BMW 3 Series M340i. 5 seats, xDrive all-wheel drive.", 5)]
    [InlineData(0, "BMW 4 Series M440i. 4 seats, xDrive all-wheel drive.", 4)]
    [InlineData(0, "Example. 9 seats.", 9)]
    [InlineData(0, "Example. 1 seats.", 1)]
    public void Reliable_seat_counts_come_from_stored_values_or_one_explicit_phrase(int stored, string description, int expected)
    {
        Assert.Equal(expected, CarsTwoSeatCount.Resolve(stored, description));
    }

    [Theory]
    [InlineData(0, "Ferrari California")]
    [InlineData(0, "Heated seats and leather seats.")]
    [InlineData(0, "4 seats in front and 5 seats overall.")]
    [InlineData(0, "Example. 10 seats.")]
    [InlineData(0, "Example. 0 seats.")]
    [InlineData(0, "")]
    [InlineData(0, null)]
    public void Missing_or_ambiguous_text_does_not_invent_a_seat_count(int stored, string? description)
    {
        Assert.Null(CarsTwoSeatCount.Resolve(stored, description));
    }

    [Fact]
    public void CarsTwo_sidebar_drops_clear_icons_and_offers_seat_checkboxes_last()
    {
        var view = Read("FinalProject.MVC", "Views", "Cars", "CarsTwo.cshtml");
        Assert.DoesNotContain("fa-xmark", view, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"cross\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain(">0 seats<", view, StringComparison.Ordinal);
        Assert.Equal(1, Count(view, "<h2>Car Seats</h2>"));

        var fuel = view.IndexOf("<h2>Fuel</h2>", StringComparison.Ordinal);
        var seats = view.IndexOf("<h2>Car Seats</h2>", StringComparison.Ordinal);
        Assert.True(fuel >= 0 && seats > fuel);

        for (var seat = 1; seat <= 9; seat++)
        {
            Assert.Contains($"value=\"@seat\"", view, StringComparison.Ordinal);
        }

        Assert.Contains("class=\"seat-filter\"", view, StringComparison.Ordinal);
        Assert.Contains("CarsTwoSeatCount.Resolve(car.Seats, car.Description)", view, StringComparison.Ordinal);
        Assert.Contains("seatValues.indexOf(card.getAttribute(\"data-seats\"))", view, StringComparison.Ordinal);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Read(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? throw new InvalidOperationException("Could not find the solution root.");
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}
