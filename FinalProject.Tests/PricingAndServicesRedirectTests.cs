using System.Reflection;
using FinalProject.MVC.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FinalProject.Tests;

public class PricingAndServicesRedirectTests
{
    [Fact]
    public void Pricing_and_Services_index_permanently_redirect_to_Contact()
    {
        var pricing = new PricingController().Index();
        var services = new ServicesController().Index();

        var pricingRedirect = Assert.IsType<RedirectToActionResult>(pricing);
        Assert.True(pricingRedirect.Permanent);
        Assert.Equal("Index", pricingRedirect.ActionName);
        Assert.Equal("Contact", pricingRedirect.ControllerName);

        var servicesRedirect = Assert.IsType<RedirectToActionResult>(services);
        Assert.True(servicesRedirect.Permanent);
        Assert.Equal("Index", servicesRedirect.ActionName);
        Assert.Equal("Contact", servicesRedirect.ControllerName);
    }

    [Fact]
    public void Public_navigation_does_not_link_to_Pricing_or_Services_demo_pages()
    {
        var root = FindRepoRoot();
        var nav = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_PublicNav.cshtml"));
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"));
        var explore = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_ExploreNav.cshtml"));
        var pricingView = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Pricing", "Index.cshtml"));
        var servicesView = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Services", "Index.cshtml"));

        Assert.DoesNotContain("asp-controller=\"Pricing\"", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Services\"", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Pricing\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Services\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Pricing\"", explore, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Services\"", explore, StringComparison.Ordinal);

        Assert.DoesNotContain("Choose Plan", pricingView, StringComparison.Ordinal);
        Assert.DoesNotContain("Basic Repair", pricingView, StringComparison.Ordinal);
        Assert.DoesNotContain("Service Center", servicesView, StringComparison.Ordinal);
        Assert.Contains("redirects to Contact", pricingView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redirects to Contact", servicesView, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pricing_and_Services_controllers_expose_only_index_redirect_actions()
    {
        Assert.Single(typeof(PricingController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Single(typeof(ServicesController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(PricingController.Index), typeof(PricingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single()
            .Name);
        Assert.Equal(nameof(ServicesController.Index), typeof(ServicesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single()
            .Name);
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
