using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class CalculatorBrandsAndCategoryIconTests
{
    [Fact]
    public void Calculator_view_loads_brands_from_model_not_hardcoded_makes()
    {
        var root = FindRepoRoot();
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Calculator", "Index.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "CalculatorController.cs"));

        Assert.Contains("Model.BrandNames", view, StringComparison.Ordinal);
        Assert.Contains("Other", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"Mazda\">Mazda</option>", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"Citroen\">Citroen</option>", view, StringComparison.Ordinal);
        Assert.Contains("GetLookupsAsync", controller, StringComparison.Ordinal);
        Assert.Contains("ICarApiService", controller, StringComparison.Ordinal);
        Assert.Contains("BrandNames", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_category_icons_resolve_from_sql_with_safe_fallback()
    {
        var root = FindRepoRoot();
        var home = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "HomeController.cs"));
        Assert.Contains("CategoryIconPaths.Resolve", home, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "IconPath = $\"~/assets/images/category/{(index % 6) + 1:00}.svg\"",
            home,
            StringComparison.Ordinal);

        Assert.Equal("~/assets/images/category/04.svg", CategoryIconPaths.Resolve("~/assets/images/category/04.svg", 0));
        Assert.Equal("~/assets/images/category/02.svg", CategoryIconPaths.Resolve("/assets/images/category/02.svg", 9));
        Assert.Equal(CategoryIconPaths.ForIndex(0), CategoryIconPaths.Resolve(null, 0));
        Assert.Equal(CategoryIconPaths.ForIndex(3), CategoryIconPaths.Resolve("https://evil.example/x.svg", 3));
        Assert.Equal(CategoryIconPaths.ForIndex(1), CategoryIconPaths.Resolve("~/assets/images/catalog/bmw/x.jpg", 1));
        Assert.False(CategoryIconPaths.IsSafeCategoryIcon("../assets/images/category/01.svg"));
        Assert.True(CategoryIconPaths.IsSafeCategoryIcon("~/assets/images/category/01.svg"));
    }

    [Fact]
    public void Category_entity_and_api_lookups_expose_icon_path()
    {
        var root = FindRepoRoot();
        var entity = File.ReadAllText(Path.Combine(root, "FinalProject", "Models", "Entities", "Category.cs"));
        var dto = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "DTOs", "CarApiDto.cs"));
        var service = File.ReadAllText(Path.Combine(root, "FinalProject", "Services", "CarService.cs"));
        var migration = File.ReadAllText(Path.Combine(
            root, "FinalProject", "Data", "Migrations", "20261030184500_AddCategoryIconPath.cs"));

        Assert.Contains("IconPath", entity, StringComparison.Ordinal);
        Assert.Contains("IconPath", dto, StringComparison.Ordinal);
        Assert.Contains("IconPath = category.IconPath", service, StringComparison.Ordinal);
        Assert.Contains("AddColumn", migration, StringComparison.Ordinal);
        Assert.Contains("IconPath", migration, StringComparison.Ordinal);
        Assert.Contains("ROW_NUMBER()", migration, StringComparison.Ordinal);
        Assert.Contains("assets/images/category/", migration, StringComparison.Ordinal);
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
