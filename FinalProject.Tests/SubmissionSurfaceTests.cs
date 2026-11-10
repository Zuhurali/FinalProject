using Xunit;

namespace FinalProject.Tests;

public class SubmissionSurfaceTests
{
    [Fact]
    public void Most_favorited_cards_use_the_ranked_member_count()
    {
        var root = FindRepositoryRoot();
        var cars = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CarsController.cs"));
        var explore = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Cars", "Explore.cshtml"));
        var ranking = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "Services", "CarDiscoveryService.cs"));
        var counts = File.ReadAllText(Path.Combine(root, "AutoVault.Repository", "Implementations", "FinalProjectRepository.Cars.cs"));

        Assert.Contains("Saved by 1 member", cars, StringComparison.Ordinal);
        Assert.Contains("Saved by {item.Count} members", cars, StringComparison.Ordinal);
        Assert.Contains("Saved by @savedBy", explore, StringComparison.Ordinal);
        Assert.Contains("fa-heart", explore, StringComparison.Ordinal);
        Assert.Contains("GetFavoriteCountsByCarAsync", ranking, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(item => item.Count)", ranking, StringComparison.Ordinal);
        Assert.Contains("UX_Favorites_UserId_CarId", File.ReadAllText(Path.Combine(root, "FinalProject", "Data", "Migrations", "20261006142731_AddMarketplaceAccounts.cs")), StringComparison.Ordinal);
        Assert.Contains("group.Count()", counts, StringComparison.Ordinal);
        Assert.Contains("AddFavoriteAsync", cars, StringComparison.Ordinal);
        Assert.Contains("RemoveFavoriteAsync", cars, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_cart_checkout_and_coupon_screens_are_gone_while_store_workflows_remain()
    {
        var root = FindRepositoryRoot();
        var shop = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "ShopController.cs"));
        var checkout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CheckoutController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_AdminLayout.cshtml"));
        var nav = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_PublicNav.cshtml"));
        var details = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "Details.cshtml"));

        Assert.DoesNotContain("AddToCart", shop, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyCoupon", shop, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "Cart.cshtml")));
        Assert.False(File.Exists(Path.Combine(root, "FinalProject.MVC", "Views", "Checkout", "Index.cshtml")));
        Assert.False(File.Exists(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCoupons", "Index.cshtml")));
        Assert.DoesNotContain("asp-action=\"Cart\"", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestToBuy", details, StringComparison.Ordinal);
        Assert.DoesNotContain("Request to Buy", details, StringComparison.Ordinal);
        Assert.Contains("Online checkout is not available", checkout, StringComparison.Ordinal);
        Assert.Contains("Confirmation", checkout, StringComparison.Ordinal);
        Assert.Contains("OrderConfirmationAccess.CanView", checkout, StringComparison.Ordinal);
        Assert.Contains(">Campaigns<", layout, StringComparison.Ordinal);
        Assert.Contains(">In-Store Sales<", layout, StringComparison.Ordinal);
        Assert.DoesNotContain(">Purchase Requests<", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("My Purchase Requests", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_AccountNavigation.cshtml")), StringComparison.Ordinal);
        Assert.DoesNotContain(">Orders<", layout, StringComparison.Ordinal);
        Assert.DoesNotContain(">Coupons<", layout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "FinalProject.MVC", "Views", "Checkout", "Confirmation.cshtml")));
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find FinalProject.slnx.");
    }
}
