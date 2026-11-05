using System.Reflection;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;
using ApiCarsController = FinalProject.Controllers.CarsController;
using ApiCatalogController = FinalProject.Controllers.AdminCatalogController;

namespace FinalProject.Tests;

public class StaffRoleParityTests
{
    private const string Staff = "Admin,SuperAdmin";

    [Fact]
    public void Admin_and_SuperAdmin_share_catalog_creation()
    {
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminBrandsController), nameof(AdminBrandsController.Create)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCategoriesController), nameof(AdminCategoriesController.Create)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCarsController), nameof(AdminCarsController.Create), typeof(CancellationToken)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminProductsController), nameof(AdminProductsController.Create)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.CreateBrand)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.CreateCategory)));
        Assert.Equal(
            Staff,
            typeof(ApiCarsController).GetMethod(nameof(ApiCarsController.CreateCatalog))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Admin_can_edit_and_delete_shared_catalog_records()
    {
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminBrandsController), nameof(AdminBrandsController.Edit)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminBrandsController), nameof(AdminBrandsController.Delete)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCategoriesController), nameof(AdminCategoriesController.Edit)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCategoriesController), nameof(AdminCategoriesController.Delete)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCarsController), nameof(AdminCarsController.Edit), typeof(int), typeof(CancellationToken)));
        Assert.Equal(Staff, EffectiveRoles(typeof(AdminCarsController), nameof(AdminCarsController.Delete)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.UpdateBrand)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.DeleteBrand)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.UpdateCategory)));
        Assert.Equal(Staff, EffectiveRoles(typeof(ApiCatalogController), nameof(ApiCatalogController.DeleteCategory)));
    }

    [Fact]
    public void Admin_navigation_hides_only_the_Users_menu()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_AdminLayout.cshtml"));
        var usersGate = layout.IndexOf("User.IsInRole(\"SuperAdmin\")", StringComparison.Ordinal);
        Assert.True(usersGate >= 0);
        Assert.Contains("asp-action=\"Users\"", layout[usersGate..], StringComparison.Ordinal);
        Assert.DoesNotContain("IsInRole(\"SuperAdmin\")", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminBrands", "Index.cshtml")));
        Assert.DoesNotContain("IsInRole(\"SuperAdmin\")", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCategories", "Index.cshtml")));
        Assert.DoesNotContain("IsInRole(\"SuperAdmin\")", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminProducts", "Index.cshtml")));
        var cars = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "Index.cshtml"));
        Assert.Contains("User.IsInRole(\"Admin\") || User.IsInRole(\"SuperAdmin\")", cars, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_has_no_role_change_endpoint()
    {
        var apiControllers = typeof(ApiCarsController).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal));
        Assert.DoesNotContain(apiControllers, type => type.GetMethods().Any(method =>
            method.Name.Contains("ChangeRole", StringComparison.Ordinal)
            || method.Name.Contains("Role", StringComparison.Ordinal) && method.GetCustomAttribute<AuthorizeAttribute>()?.Roles == "SuperAdmin"));
    }

    [Fact]
    public void Only_SuperAdmin_can_open_users_or_change_roles()
    {
        Assert.Equal(AppRoles.SuperAdmin, EffectiveRoles(typeof(AdminController), nameof(AdminController.Users)));
        Assert.Equal(AppRoles.SuperAdmin, EffectiveRoles(typeof(AdminController), nameof(AdminController.UserDetails)));
        Assert.Equal(AppRoles.SuperAdmin, EffectiveRoles(typeof(AdminController), nameof(AdminController.ChangeRole)));
        Assert.Equal(AppRoles.SuperAdmin, EffectiveRoles(typeof(AdminController), nameof(AdminController.ChangeRoles)));
        Assert.Equal(
            AppRoles.SuperAdmin,
            typeof(ApiCarsController).GetMethod(nameof(ApiCarsController.GetByOwner))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.DoesNotContain(
            AppRoles.Admin,
            EffectiveRoles(typeof(AdminController), nameof(AdminController.ChangeRole))!.Split(','));
    }

    private static string? EffectiveRoles(Type controller, string methodName, params Type[] parameterTypes)
    {
        var method = parameterTypes.Length == 0
            ? controller.GetMethod(methodName)
            : controller.GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);
        return method!.GetCustomAttribute<AuthorizeAttribute>()?.Roles
            ?? controller.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
    }
}
