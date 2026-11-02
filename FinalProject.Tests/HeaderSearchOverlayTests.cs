using Xunit;

namespace FinalProject.Tests;

public class HeaderSearchOverlayTests
{
    [Fact]
    public void Public_headers_do_not_expose_header_search_icon_or_overlay()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"));
        var headerThree = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderThree.cshtml"));
        var headerFive = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderFive.cshtml"));
        var headerSix = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderSix.cshtml"));

        Assert.DoesNotContain("search-input-area", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Open car search", layout, StringComparison.Ordinal);

        Assert.DoesNotContain("class=\"search\"", headerThree, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"search-area\"", headerThree, StringComparison.Ordinal);
        Assert.DoesNotContain("assets/images/icon/search", headerThree, StringComparison.Ordinal);
        Assert.Contains("menu-btn", headerThree, StringComparison.Ordinal);
        Assert.Contains("_AddNewDropdown", headerThree, StringComparison.Ordinal);

        Assert.DoesNotContain("class=\"search\"", headerFive, StringComparison.Ordinal);
        Assert.DoesNotContain("assets/images/icon/search", headerFive, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"search\"", headerSix, StringComparison.Ordinal);
        Assert.DoesNotContain("assets/images/icon/search", headerSix, StringComparison.Ordinal);
    }

    [Fact]
    public void CarsThree_no_longer_needs_page_scoped_header_search_css_hide()
    {
        var carsThree = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Views", "Cars", "CarsThree.cshtml"));
        Assert.DoesNotContain(".header-three .bottom-right > .search-area", carsThree, StringComparison.Ordinal);
        Assert.Contains("cars3-advanced", carsThree, StringComparison.Ordinal);
        Assert.Contains("id=\"carSearch\"", carsThree, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_hero_search_still_targets_cars_index_independently()
    {
        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Views", "Home", "Index.cshtml"));
        Assert.Contains("id=\"heroSearchForm\"", home, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"Index\"", home, StringComparison.Ordinal);
        Assert.Contains("name=\"search\"", home, StringComparison.Ordinal);
        Assert.Contains("hero-search.js", home, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "FinalProject.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
