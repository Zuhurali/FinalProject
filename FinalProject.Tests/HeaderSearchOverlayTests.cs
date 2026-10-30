using Xunit;

namespace FinalProject.Tests;

public class HeaderSearchOverlayTests
{
    [Fact]
    public void Layout_overlay_submits_get_search_to_cars_index()
    {
        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("class=\"search-input-area\"", layout, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\"", layout, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Index\"", layout, StringComparison.Ordinal);
        Assert.Contains("method=\"get\"", layout, StringComparison.Ordinal);
        Assert.Contains("name=\"search\"", layout, StringComparison.Ordinal);
        Assert.Contains("type=\"search\"", layout, StringComparison.Ordinal);
        Assert.Contains("type=\"submit\"", layout, StringComparison.Ordinal);
        Assert.Contains("class=\"search-input autocomplete\"", layout, StringComparison.Ordinal);
        Assert.Contains("id=\"close\"", layout, StringComparison.Ordinal);
        Assert.Contains("search-close-icon", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Shop\"", layout.Substring(
            layout.IndexOf("search-input-area", StringComparison.Ordinal),
            600), StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderThree_exposes_search_overlay_trigger()
    {
        var headerThree = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Views", "Shared", "_HeaderThree.cshtml"));
        var headerFive = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Views", "Shared", "_HeaderFive.cshtml"));

        Assert.Contains("class=\"search\"", headerThree, StringComparison.Ordinal);
        Assert.Contains("class=\"search-area\"", headerThree, StringComparison.Ordinal);
        Assert.Contains("assets/images/icon/search", headerThree, StringComparison.Ordinal);
        Assert.Contains("class=\"search\"", headerFive, StringComparison.Ordinal);
        Assert.Contains("menu-btn", headerThree, StringComparison.Ordinal);
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
