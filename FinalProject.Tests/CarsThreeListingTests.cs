using FinalProject.MVC.Controllers;
using FinalProject.MVC.Models;
using Xunit;

namespace FinalProject.Tests;

public class CarsThreeListingTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.MVC", "Views", "Cars", "CarsThree.cshtml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    [Fact]
    public void CarsThree_action_uses_server_paged_active_query_instead_of_full_catalog()
    {
        var root = FindRepoRoot();
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "CarsThree.cshtml"));
        var mapper = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Mappings", "CarApiMapper.cs"));
        var apiClient = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Services", "CarApiService.cs"));
        var queryModel = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "DTOs", "CarApiDto.cs"));
        var favorites = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "wwwroot", "js", "car-favorites.js"));
        var cardsPartial = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "_CarsThreeCards.cshtml"));

        Assert.Contains("BuildCarsThreePageAsync", controller, StringComparison.Ordinal);
        Assert.Contains("TryCreateCarsThreeApiQuery", controller, StringComparison.Ordinal);
        Assert.Contains("CarsThreeResults", controller, StringComparison.Ordinal);
        Assert.Contains("CarsThreeFilterQuery.PageSize", controller, StringComparison.Ordinal);
        Assert.Contains("MarkFavoritesAsync(cars)", controller, StringComparison.Ordinal);
        Assert.Contains("Sort = string.IsNullOrWhiteSpace(filters.Sort) ? \"latest\" : filters.Sort", controller, StringComparison.Ordinal);
        Assert.Contains("MinMileage = filters.MinMileage", controller, StringComparison.Ordinal);
        Assert.Contains("Seats = filters.Seats", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("RedirectToAction(nameof(Index))", ExtractCarsThreeMethod(controller), StringComparison.Ordinal);

        Assert.Contains("@model CarsThreePageModel", view, StringComparison.Ordinal);
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
        Assert.Contains("fetchPage(1, false)", view, StringComparison.Ordinal);
        Assert.Contains("fetchPage(currentPage + 1, true)", view, StringComparison.Ordinal);
        Assert.Contains("seenIds", view, StringComparison.Ordinal);
        Assert.Contains("CarsThreeResults", view, StringComparison.Ordinal);
        Assert.Contains("class=\"wishlist\"", cardsPartial, StringComparison.Ordinal);
        Assert.Contains("data-favorite-toggle", cardsPartial, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Details\"", cardsPartial, StringComparison.Ordinal);
        Assert.Contains("cars3-toolbar__sort .nice-select", view, StringComparison.Ordinal);
        Assert.DoesNotContain("matchingCards()", view, StringComparison.Ordinal);
        Assert.DoesNotContain("sortCards(", view, StringComparison.Ordinal);
        Assert.Contains("id=\"filterSeats\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carSort\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carClearFilters\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carResultCount\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carLoadMore\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"carPriceRangeError\"", view, StringComparison.Ordinal);
        Assert.Contains("Minimum price cannot be greater than maximum price.", view, StringComparison.Ordinal);
        Assert.Contains("function priceRangeInvalid()", view, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", view, StringComparison.Ordinal);
        Assert.Contains("window.scrollTo(0, scrollY)", view, StringComparison.Ordinal);
        Assert.Contains("car-favorites.js", view, StringComparison.Ordinal);
        Assert.Contains("_CarsThreeCards", view, StringComparison.Ordinal);
        Assert.Contains("_DealershipCallCta", view, StringComparison.Ordinal);
        Assert.DoesNotContain("project-wrapper2 list-style", view, StringComparison.Ordinal);

        Assert.Contains("MinMileage", queryModel, StringComparison.Ordinal);
        Assert.Contains("MaxMileage", queryModel, StringComparison.Ordinal);
        Assert.Contains("Seats", queryModel, StringComparison.Ordinal);
        Assert.Contains("Sort", queryModel, StringComparison.Ordinal);
        Assert.Contains("minMileage=", apiClient, StringComparison.Ordinal);
        Assert.Contains("maxMileage=", apiClient, StringComparison.Ordinal);
        Assert.Contains("seats=", apiClient, StringComparison.Ordinal);
        Assert.Contains("\"sort\"", apiClient, StringComparison.Ordinal);

        Assert.Contains("CarPublicListing.CreateActiveQuery()", mapper, StringComparison.Ordinal);
        Assert.Contains("ResolveSeats", mapper, StringComparison.Ordinal);
        Assert.Contains("data-favorite-toggle", favorites, StringComparison.Ordinal);
        Assert.Contains("document.addEventListener(\"submit\"", favorites, StringComparison.Ordinal);
        Assert.Equal(9, CarsThreeFilterQuery.PageSize);
        Assert.NotNull(typeof(CarsController).GetMethod(nameof(CarsController.CarsThree)));
        Assert.NotNull(typeof(CarsController).GetMethod(nameof(CarsController.CarsThreeResults)));
    }

    [Fact]
    public void CarsThree_price_validation_and_paging_reset_rules_are_preserved()
    {
        var root = FindRepoRoot();
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "CarsThree.cshtml"));

        Assert.Contains("priceRangeError.hidden = !invalidPriceRange;", view, StringComparison.Ordinal);
        Assert.Contains("empty.hidden = true;", view, StringComparison.Ordinal);
        Assert.Contains("currentPage = 1;", view, StringComparison.Ordinal);
        Assert.Contains("fetchPage(1, false)", view, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "An active minimum price sorts matches cheapest-first",
            view,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CarsOne_and_CarsTwo_remain_unchanged_by_CarsThree_server_paging()
    {
        var root = FindRepoRoot();
        var carsOne = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "CarsOne.cshtml"));
        var carsTwo = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "CarsTwo.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));

        Assert.DoesNotContain("CarsThreeResults", carsOne, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsThreeResults", carsTwo, StringComparison.Ordinal);
        var carsOneAction = ExtractMethod(controller, "CarsOne");
        var carsTwoAction = ExtractMethod(controller, "CarsTwo");
        Assert.DoesNotContain("BuildCarsThreePageAsync", carsOneAction, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildCarsThreePageAsync", carsTwoAction, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsThreeResults", carsOneAction, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsThreeResults", carsTwoAction, StringComparison.Ordinal);
        Assert.Contains("LoadMatchingCarsAsync", carsOneAction, StringComparison.Ordinal);
        Assert.Contains("var cars = await LoadCarsAsync();", carsTwoAction, StringComparison.Ordinal);
    }

    private static string ExtractCarsThreeMethod(string controllerSource)
    {
        const string marker = "public async Task<IActionResult> CarsThree(";
        var start = controllerSource.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "CarsThree action not found.");
        var end = controllerSource.IndexOf("public async Task<IActionResult> CarsThreeResults", start, StringComparison.Ordinal);
        Assert.True(end > start, "Could not bound CarsThree method body.");
        return controllerSource[start..end];
    }

    private static string ExtractMethod(string controllerSource, string actionName)
    {
        var marker = $"public async Task<IActionResult> {actionName}(";
        var start = controllerSource.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, actionName + " action not found.");
        var end = controllerSource.IndexOf("public async Task<IActionResult>", start + marker.Length, StringComparison.Ordinal);
        Assert.True(end > start, "Could not bound " + actionName + " method body.");
        return controllerSource[start..end];
    }
}
