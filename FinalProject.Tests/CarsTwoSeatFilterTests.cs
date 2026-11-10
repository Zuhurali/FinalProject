using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class CarsTwoSeatFilterTests
{
    private static readonly (string Make, int StoredSeats)[] SampleCatalog =
    [
        ("Ford", 4),
        ("Ford", 4),
        ("Ford", 4),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 5),
        ("Ford", 7),
        ("Ford", 8),
        ("Ford", 0),
        ("BMW", 2),
        ("BMW", 4),
        ("BMW", 4),
        ("BMW", 4),
        ("BMW", 5),
        ("BMW", 5),
        ("BMW", 5),
        ("Audi", 4),
        ("Audi", 5)
    ];

    [Theory]
    [InlineData(2, "anything 5 seats", 2)]
    [InlineData(4, "ignored 2 seats", 4)]
    [InlineData(5, null, 5)]
    [InlineData(12, "12 seats", 12)]
    public void Structured_seat_counts_are_the_only_source_of_truth(int stored, string? description, int expected)
    {
        Assert.Equal(expected, CarsTwoSeatCount.Resolve(stored, description));
    }

    [Theory]
    [InlineData(0, "BMW 3 Series M340i. 5 seats, xDrive all-wheel drive.")]
    [InlineData(0, "BMW 4 Series M440i. 4 seats, xDrive all-wheel drive.")]
    [InlineData(0, "Example. 9 seats.")]
    [InlineData(0, "Example. 2 seats.")]
    [InlineData(0, "Ferrari California")]
    [InlineData(0, "Heated seats and leather seats.")]
    [InlineData(0, "4 seats in front and 5 seats overall.")]
    [InlineData(0, "3 doors and 2 seats.")]
    [InlineData(0, "")]
    [InlineData(0, null)]
    [InlineData(13, "13 seats")]
    [InlineData(-1, "4 seats")]
    public void Description_doors_and_unknown_values_are_not_treated_as_seats(int stored, string? description)
    {
        Assert.Null(CarsTwoSeatCount.Resolve(stored, description));
    }

    [Fact]
    public void Selecting_4_seats_excludes_2_and_3_seat_cars()
    {
        var selected = new[] { 4 };
        Assert.True(CarsTwoSeatCount.Matches(4, "2 seats", selected));
        Assert.False(CarsTwoSeatCount.Matches(2, "4 seats", selected));
        Assert.False(CarsTwoSeatCount.Matches(3, null, selected));
        Assert.False(CarsTwoSeatCount.Matches(0, "4 seats", selected));
    }

    [Fact]
    public void Selecting_2_seats_excludes_4_and_5_seat_cars()
    {
        var selected = new[] { 2 };
        Assert.True(CarsTwoSeatCount.Matches(2, null, selected));
        Assert.False(CarsTwoSeatCount.Matches(4, "2 seats", selected));
        Assert.False(CarsTwoSeatCount.Matches(5, null, selected));
        Assert.False(CarsTwoSeatCount.Matches(0, "2 seats", selected));
    }

    [Fact]
    public void Multiple_seat_selections_match_any_exact_value()
    {
        var selected = new[] { 2, 5 };
        Assert.True(CarsTwoSeatCount.Matches(2, null, selected));
        Assert.True(CarsTwoSeatCount.Matches(5, "4 seats", selected));
        Assert.False(CarsTwoSeatCount.Matches(4, null, selected));
        Assert.False(CarsTwoSeatCount.Matches(0, "2 seats", selected));
    }

    [Fact]
    public void Clearing_seat_selection_keeps_all_cars_including_unknown_seats()
    {
        var selected = Array.Empty<int>();
        Assert.True(CarsTwoSeatCount.Matches(2, null, selected));
        Assert.True(CarsTwoSeatCount.Matches(4, null, selected));
        Assert.True(CarsTwoSeatCount.Matches(0, "4 seats", selected));
        Assert.True(CarsTwoSeatCount.Matches(0, null, selected));
    }

    [Fact]
    public void Seat_facets_without_a_brand_use_the_full_catalog_and_skip_unknown_seats()
    {
        var options = CarsTwoSeatCount.FacetOptions(SampleCatalog);
        Assert.Equal([(2, 1), (4, 7), (5, 13), (7, 1), (8, 1)], options);
    }

    [Fact]
    public void Ford_only_seat_facets_exclude_other_brands_and_unknown_seats()
    {
        var options = CarsTwoSeatCount.FacetOptions(SampleCatalog, ["Ford"]);
        Assert.Equal([(4, 3), (5, 9), (7, 1), (8, 1)], options);
        Assert.DoesNotContain(options, option => option.Seats == 2);
    }

    [Fact]
    public void Bmw_only_seat_facets_use_bmw_counts_only()
    {
        var options = CarsTwoSeatCount.FacetOptions(SampleCatalog, ["BMW"]);
        Assert.Equal([(2, 1), (4, 3), (5, 3)], options);
        Assert.DoesNotContain(options, option => option.Seats is 7 or 8);
    }

    [Fact]
    public void Multiple_selected_brands_combine_seat_facets()
    {
        var options = CarsTwoSeatCount.FacetOptions(SampleCatalog, ["Ford", "BMW"]);
        Assert.Equal([(2, 1), (4, 6), (5, 12), (7, 1), (8, 1)], options);
    }

    [Fact]
    public void Clearing_brand_selection_restores_global_seat_facets()
    {
        var fordOnly = CarsTwoSeatCount.FacetOptions(SampleCatalog, ["Ford"]);
        var cleared = CarsTwoSeatCount.FacetOptions(SampleCatalog, Array.Empty<string>());
        Assert.NotEqual(fordOnly, cleared);
        Assert.Equal(CarsTwoSeatCount.FacetOptions(SampleCatalog), cleared);
    }

    [Fact]
    public void Brand_and_seat_filters_combine_for_matching_results()
    {
        var selectedSeats = new[] { 4 };
        var fordMatches = SampleCatalog
            .Where(car => string.Equals(car.Make, "Ford", StringComparison.OrdinalIgnoreCase))
            .Where(car => CarsTwoSeatCount.Matches(car.StoredSeats, null, selectedSeats))
            .ToList();
        Assert.Equal(3, fordMatches.Count);
        Assert.All(fordMatches, car => Assert.Equal(4, car.StoredSeats));
    }

    [Fact]
    public void CarsTwo_lists_seat_counts_without_waiting_for_a_brand()
    {
        var view = Read("FinalProject.MVC", "Views", "Cars", "CarsTwo.cshtml");
        var cards = Read("FinalProject.MVC", "Views", "Cars", "_CarsTwoCards.cshtml");
        var controller = Read("FinalProject.MVC", "Controllers", "CarsController.cs");
        Assert.DoesNotContain("fa-xmark", view, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"cross\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain(">0 seats<", view, StringComparison.Ordinal);
        Assert.Equal(1, Count(view, "<h2>Car Seats</h2>"));

        var fuel = view.IndexOf("<h2>Fuel</h2>", StringComparison.Ordinal);
        var seats = view.IndexOf("<h2>Car Seats</h2>", StringComparison.Ordinal);
        Assert.True(fuel >= 0 && seats > fuel);

        var seatList = view.IndexOf("id=\"carSeatList\"", StringComparison.Ordinal);
        var seatListEnd = view.IndexOf("</ul>", seatList, StringComparison.Ordinal);
        var seatMarkup = view[seatList..seatListEnd];
        Assert.DoesNotContain("Select a brand first", seatMarkup, StringComparison.Ordinal);
        Assert.Contains("class=\"seat-filter\"", seatMarkup, StringComparison.Ordinal);
        Assert.Contains("option.Seats", seatMarkup, StringComparison.Ordinal);
        Assert.Contains("CarsTwo(int[]? seats)", controller, StringComparison.Ordinal);
        Assert.Contains("CarsTwoResults(int[]? seats)", controller, StringComparison.Ordinal);
        Assert.Contains("PartialView(\"_CarsTwoCards\"", controller, StringComparison.Ordinal);
        Assert.Contains("CarsTwoSeatCount.Resolve(car.Seats, car.Description)", controller, StringComparison.Ordinal);
        Assert.Contains("CarsTwoSeatCount.Matches(car.Seats, car.Description, selectedSeats)", controller, StringComparison.Ordinal);
        Assert.Contains("params.append(\"seats\", value)", view, StringComparison.Ordinal);
        Assert.Contains("history.replaceState", view, StringComparison.Ordinal);
        Assert.Contains("fetch(resultsUrl", view, StringComparison.Ordinal);
        Assert.DoesNotContain("window.location.search =", view, StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload", view, StringComparison.Ordinal);
        Assert.Contains("seatValues.indexOf(card.getAttribute(\"data-seats\"))", view, StringComparison.Ordinal);
        Assert.Contains("No cars match this filter.", view, StringComparison.Ordinal);
        Assert.Contains("name=\"_CarsTwoCards\"", view, StringComparison.Ordinal);
        Assert.Contains("CarsTwoSeatCount.Resolve(car.Seats, car.Description)", cards, StringComparison.Ordinal);
        Assert.Contains("cars-two-seat-badge", cards, StringComparison.Ordinal);
        Assert.Contains("@seatCount.Value seats", cards, StringComparison.Ordinal);
        Assert.DoesNotContain("@car.ImageCount", cards, StringComparison.Ordinal);
        Assert.Contains("refreshColorAndSeatFilters", view, StringComparison.Ordinal);
        Assert.Contains("refreshSeatFilters", view, StringComparison.Ordinal);
        Assert.Contains("catalogForSeatFacets", view, StringComparison.Ordinal);
        Assert.Contains("facetCatalog", view, StringComparison.Ordinal);
        Assert.Contains("cardsForFacetScope", view, StringComparison.Ordinal);
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
