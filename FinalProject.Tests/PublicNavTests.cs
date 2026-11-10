using Xunit;

namespace FinalProject.Tests;

public class PublicNavTests
{
    [Fact]
    public void Public_nav_restores_original_listing_dropdown_and_menu_structure()
    {
        var root = FindRepoRoot();
        var nav = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_PublicNav.cshtml"));
        var explore = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_ExploreNav.cshtml"));
        var headerThree = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderThree.cshtml"));
        var headerFive = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderFive.cshtml"));
        var headerSix = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderSix.cshtml"));
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"));

        // Top-level labels / order from the pre-change Autovault MVC nav.
        Assert.Contains(">Dealership</a>", nav, StringComparison.Ordinal);
        Assert.Contains(">About Us</a>", nav, StringComparison.Ordinal);
        Assert.Contains(">Listing</a>", nav, StringComparison.Ordinal);
        Assert.Contains(">Page</a>", nav, StringComparison.Ordinal);
        Assert.Contains(">Spare Parts</a>", nav, StringComparison.Ordinal);
        Assert.Contains(">Contact</a>", nav, StringComparison.Ordinal);
        Assert.Contains("_ExploreNav", nav, StringComparison.Ordinal);

        Assert.Contains("submenuClass", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"CarsOne\">Cars One</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"CarsTwo\">Cars Two</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"CarsThree\">Cars Three</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"Details\" asp-route-id=\"1\">Car Single</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"Index\">Used Cars</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Cars\" asp-action=\"New\">New Cars</a>", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Cars\" asp-action=\"Index\">Listing</a>", nav, StringComparison.Ordinal);
        var listing = nav[(nav.IndexOf(">Listing</a>", StringComparison.Ordinal))..nav.IndexOf(">Page</a>", StringComparison.Ordinal)];
        var carsOne = listing.IndexOf("Cars One", StringComparison.Ordinal);
        var carsTwo = listing.IndexOf("Cars Two", StringComparison.Ordinal);
        var carsThree = listing.IndexOf("Cars Three", StringComparison.Ordinal);
        var carSingle = listing.IndexOf("Car Single", StringComparison.Ordinal);
        var used = listing.IndexOf("Used Cars", StringComparison.Ordinal);
        var newer = listing.IndexOf("New Cars", StringComparison.Ordinal);
        Assert.True(carsOne < carsTwo && carsTwo < carsThree && carsThree < carSingle && carSingle < used && used < newer);

        // Page submenu + explore remain.
        Assert.Contains("asp-controller=\"Faq\" asp-action=\"Index\">FAQ</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Calculator\" asp-action=\"Index\">Calculator</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Team\" asp-action=\"Index\">Team</a>", nav, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"Account\" asp-action=\"Index\">Account</a>", nav, StringComparison.Ordinal);
        Assert.Contains("MostViewed", explore, StringComparison.Ordinal);
        Assert.Contains("MostFavorited", explore, StringComparison.Ordinal);
        Assert.Contains("Latest", explore, StringComparison.Ordinal);

        // Shared across header variants.
        Assert.Contains("_PublicNav", headerThree, StringComparison.Ordinal);
        Assert.Contains("_PublicNav", headerFive, StringComparison.Ordinal);
        Assert.Contains("_PublicNav", headerSix, StringComparison.Ordinal);
        Assert.Contains("_PublicNav", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsOne", headerThree, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsOne", headerFive, StringComparison.Ordinal);
        Assert.DoesNotContain("CarsOne", headerSix, StringComparison.Ordinal);
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
