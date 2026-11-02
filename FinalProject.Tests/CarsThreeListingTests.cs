using FinalProject.MVC.Controllers;
using Xunit;

namespace FinalProject.Tests;

public class CarsThreeListingTests
{
    [Fact]
    public void CarsThree_action_renders_advanced_list_view_instead_of_redirecting()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "CarsThree.cshtml"));
        var mapper = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Mappings", "CarApiMapper.cs"));

        Assert.Contains("public async Task<IActionResult> CarsThree()", controller, StringComparison.Ordinal);
        Assert.Contains("var cars = await LoadCarsAsync();", controller, StringComparison.Ordinal);
        Assert.Contains("await MarkFavoritesAsync(cars);", controller, StringComparison.Ordinal);
        Assert.Contains("return View(cars);", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("RedirectToAction(nameof(Index))", ExtractCarsThreeMethod(controller), StringComparison.Ordinal);

        Assert.Contains("@model IReadOnlyList<FinalProject.MVC.Models.Car>", view, StringComparison.Ordinal);
        Assert.Contains("cars3-advanced", view, StringComparison.Ordinal);
        Assert.Contains("cars3-card", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterBrand\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterModel\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterType\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMinPrice\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMaxPrice\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMinYear\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMaxYear\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterFuel\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterTransmission\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMinMileage\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterMaxMileage\"", view, StringComparison.Ordinal);
        Assert.Contains("parseNonNegativeFilter", view, StringComparison.Ordinal);
        Assert.Contains("window.jQuery(el).on(\"change\", onFilterChange);", view, StringComparison.Ordinal);
        Assert.Contains("parseNonNegativeFilter(minMileageInput.value)", view, StringComparison.Ordinal);
        Assert.Contains("parseNonNegativeFilter(maxMileageInput.value)", view, StringComparison.Ordinal);
        Assert.Contains("mileage < minM", view, StringComparison.Ordinal);
        Assert.Contains("mileage > maxM", view, StringComparison.Ordinal);
        Assert.Contains("class=\"wishlist\"", view, StringComparison.Ordinal);
        Assert.Contains("cars3-toolbar__sort .nice-select", view, StringComparison.Ordinal);
        Assert.DoesNotContain("resolveSortMode", view, StringComparison.Ordinal);
        Assert.DoesNotContain("committedMaxMileage", view, StringComparison.Ordinal);
        Assert.DoesNotContain("commitMaxMileageFilter", view, StringComparison.Ordinal);
        Assert.DoesNotContain("min=\"@minMileage\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("max=\"@maxMileage\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterSeats\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carSort\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carClearFilters\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carResultCount\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carLoadMore\"", view, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", view, StringComparison.Ordinal);
        Assert.Contains("window.scrollTo(0, scrollY)", view, StringComparison.Ordinal);
        Assert.Contains("data-favorite-toggle", view, StringComparison.Ordinal);
        Assert.Contains("car-favorites.js", view, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Details\"", view, StringComparison.Ordinal);
        Assert.Contains("_DealershipCallCta", view, StringComparison.Ordinal);
        Assert.DoesNotContain("project-wrapper2 list-style", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Honda Civic Hatchback", view, StringComparison.Ordinal);

        Assert.Contains("CarPublicListing.CreateActiveQuery()", mapper, StringComparison.Ordinal);
        Assert.NotNull(typeof(CarsController).GetMethod(nameof(CarsController.CarsThree)));
    }

    private static string ExtractCarsThreeMethod(string controllerSource)
    {
        const string marker = "public async Task<IActionResult> CarsThree()";
        var start = controllerSource.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "CarsThree action not found.");
        var end = controllerSource.IndexOf("public async Task<IActionResult> Details", start, StringComparison.Ordinal);
        Assert.True(end > start, "Could not bound CarsThree method body.");
        return controllerSource[start..end];
    }
}
