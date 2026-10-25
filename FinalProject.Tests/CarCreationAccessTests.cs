using System.Reflection;
using System.Security.Claims;
using FinalProject.Controllers;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinalProject.Tests;

public class CarCreationAccessTests
{
    [Fact]
    public void Member_cannot_see_Add_Car()
    {
        Assert.False(CarCreationAccess.CanCreateCars(Principal(AppRoles.Member)));
    }

    [Fact]
    public void Anonymous_visitor_cannot_see_Add_Car()
    {
        Assert.False(CarCreationAccess.CanCreateCars(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.False(CarCreationAccess.CanCreateCars(null));
    }

    [Fact]
    public void Admin_can_see_Add_Car()
    {
        Assert.True(CarCreationAccess.CanCreateCars(Principal(AppRoles.Admin)));
    }

    [Fact]
    public void SuperAdmin_can_see_Add_Car()
    {
        Assert.True(CarCreationAccess.CanCreateCars(Principal(AppRoles.SuperAdmin)));
    }

    [Fact]
    public async Task Member_promoted_to_Admin_gains_Add_Car()
    {
        await using var identity = await IdentityHarness.CreateAsync(AppRoles.Member);
        Assert.False(CarCreationAccess.CanCreateCars(await identity.CurrentPrincipalAsync()));

        var stampBefore = await identity.StampAsync();
        var staleCookie = await identity.CurrentPrincipalAsync();
        await identity.ReplaceRoleAsync(AppRoles.Admin);
        var stampAfter = await identity.StampAsync();

        Assert.Equal(stampBefore, stampAfter);
        Assert.True(CarCreationAccess.CanCreateCars(await identity.CurrentPrincipalAsync()));
        var refreshed = await identity.RefreshCookieAsync(staleCookie);
        Assert.NotNull(refreshed.Principal);
        Assert.True(refreshed.ShouldRenew);
        Assert.True(CarCreationAccess.CanCreateCars(refreshed.Principal));
    }

    [Fact]
    public async Task Member_promoted_to_SuperAdmin_gains_Add_Car()
    {
        await using var identity = await IdentityHarness.CreateAsync(AppRoles.Member);
        Assert.False(CarCreationAccess.CanCreateCars(await identity.CurrentPrincipalAsync()));

        var staleCookie = await identity.CurrentPrincipalAsync();
        await identity.ReplaceRoleAsync(AppRoles.SuperAdmin);

        Assert.True(CarCreationAccess.CanCreateCars(await identity.CurrentPrincipalAsync()));
        Assert.Contains(AppRoles.SuperAdmin, await identity.RolesAsync());
        var refreshed = await identity.RefreshCookieAsync(staleCookie);
        Assert.NotNull(refreshed.Principal);
        Assert.True(refreshed.Principal.IsInRole(AppRoles.SuperAdmin));
        Assert.True(CarCreationAccess.CanCreateCars(refreshed.Principal));
    }

    [Fact]
    public async Task Admin_demoted_to_Member_loses_Add_Car()
    {
        await using var identity = await IdentityHarness.CreateAsync(AppRoles.Admin);
        var previousPrincipal = await identity.CurrentPrincipalAsync();
        Assert.True(CarCreationAccess.CanCreateCars(previousPrincipal));

        var stampBefore = await identity.StampAsync();
        await identity.ReplaceRoleAsync(AppRoles.Member);
        var stampAfter = await identity.StampAsync();

        Assert.Equal(stampBefore, stampAfter);
        Assert.False(CarCreationAccess.CanCreateCars(await identity.CurrentPrincipalAsync()));
        Assert.DoesNotContain(AppRoles.Admin, await identity.RolesAsync());
        var refreshed = await identity.RefreshCookieAsync(previousPrincipal);
        Assert.NotNull(refreshed.Principal);
        Assert.True(refreshed.ShouldRenew);
        Assert.False(refreshed.Principal.IsInRole(AppRoles.Admin));
        Assert.False(CarCreationAccess.CanCreateCars(refreshed.Principal));
        Assert.True(previousPrincipal.IsInRole(AppRoles.Admin));
    }

    [Fact]
    public async Task Stale_cookie_is_rejected_when_the_security_stamp_changes()
    {
        await using var identity = await IdentityHarness.CreateAsync(AppRoles.Admin);
        var staleCookie = await identity.CurrentPrincipalAsync();
        Assert.True(CarCreationAccess.CanCreateCars(staleCookie));

        await identity.RotateSecurityStampAsync();

        var refreshed = await identity.RefreshCookieAsync(staleCookie);
        Assert.Null(refreshed.Principal);
        Assert.False(refreshed.ShouldRenew);
    }

    [Fact]
    public async Task SuperAdmin_role_change_does_not_sign_out_the_actor()
    {
        await using var identity = await IdentityHarness.CreateAsync(AppRoles.SuperAdmin);
        var actor = await identity.CurrentPrincipalAsync();
        var actorStamp = await identity.StampAsync();
        var member = await identity.AddUserAsync("member@example.com", AppRoles.Member);
        var memberStamp = member.SecurityStamp;

        var result = await identity.ChangeRoleThroughUsersAdminAsync(actor, member.Id, AppRoles.Admin);

        Assert.True(result.Ok);
        Assert.Equal(actorStamp, await identity.StampAsync());
        var memberAfter = await identity.FindUserAsync(member.Id);
        Assert.Equal(memberStamp, memberAfter.SecurityStamp);
        Assert.Contains(AppRoles.Admin, await identity.RolesForAsync(member.Id));

        var actorAfter = await identity.RefreshCookieAsync(actor);
        Assert.NotNull(actorAfter.Principal);
        Assert.True(actorAfter.Principal.IsInRole(AppRoles.SuperAdmin));
        Assert.True(actorAfter.Principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public void Member_and_anonymous_cannot_call_car_creation_endpoints()
    {
        AssertCreationRoles(typeof(AccountController), nameof(AccountController.AddCar));
        AssertCreationRoles(typeof(global::FinalProject.Controllers.CarsController), nameof(global::FinalProject.Controllers.CarsController.Create));
    }

    [Fact]
    public void Public_Add_Car_links_are_role_gated()
    {
        var root = FindRepositoryRoot();
        var views = new[]
        {
            Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_Layout.cshtml"),
            Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderThree.cshtml"),
            Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderFive.cshtml"),
            Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_HeaderSix.cshtml"),
            Path.Combine(root, "FinalProject.MVC", "Views", "Account", "Listings.cshtml")
        };

        foreach (var view in views)
        {
            var text = File.ReadAllText(view);
            var links = Count(text, "asp-action=\"AddCar\"");
            Assert.True(links > 0, view);
            Assert.Equal(links, Count(text, "CarCreationAccess.CanCreateCars(User)"));
        }

        var program = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Program.cs"));
        Assert.Contains("ValidationInterval = TimeSpan.Zero", program, StringComparison.Ordinal);

        var users = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "AdminController.cs"));
        Assert.DoesNotContain("UpdateSecurityStampAsync", users, StringComparison.Ordinal);
    }

    private static void AssertCreationRoles(Type controller, string methodName)
    {
        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == methodName)
            .ToList();
        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var roles = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .SelectMany(attribute => (attribute.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToHashSet(StringComparer.Ordinal);
            Assert.Contains(AppRoles.Admin, roles);
            Assert.Contains(AppRoles.SuperAdmin, roles);
            Assert.DoesNotContain(AppRoles.Member, roles);
        }
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user"), new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "FinalProject.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class IdentityHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly ApplicationUser _user;

        private IdentityHarness(ServiceProvider provider, ApplicationUser user)
        {
            _provider = provider;
            _user = user;
        }

        public static async Task<IdentityHarness> CreateAsync(string role)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("car-creation-" + Guid.NewGuid().ToString("N")));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.Zero;
            });

            var provider = services.BuildServiceProvider();
            var roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var name in AppRoles.All)
            {
                await roles.CreateAsync(new IdentityRole(name));
            }

            var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "dealer-staff@example.com",
                Email = "dealer-staff@example.com",
                EmailConfirmed = true
            };
            var created = await users.CreateAsync(user, "Test-password-123!");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", created.Errors.Select(error => error.Description)));
            }

            var added = await users.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", added.Errors.Select(error => error.Description)));
            }

            return new IdentityHarness(provider, user);
        }

        public async Task ReplaceRoleAsync(string role)
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var current = await users.GetRolesAsync(_user);
            if (current.Count > 0)
            {
                var removed = await users.RemoveFromRolesAsync(_user, current);
                if (!removed.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(", ", removed.Errors.Select(error => error.Description)));
                }
            }

            var added = await users.AddToRoleAsync(_user, role);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", added.Errors.Select(error => error.Description)));
            }
        }

        public async Task RotateSecurityStampAsync()
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var stamped = await users.UpdateSecurityStampAsync(_user);
            if (!stamped.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", stamped.Errors.Select(error => error.Description)));
            }
        }

        public async Task<ApplicationUser> AddUserAsync(string email, string role)
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var created = await users.CreateAsync(user, "Test-password-123!");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", created.Errors.Select(error => error.Description)));
            }

            var added = await users.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", added.Errors.Select(error => error.Description)));
            }

            return (await users.FindByIdAsync(user.Id))!;
        }

        public async Task<(bool Ok, string Message)> ChangeRoleThroughUsersAdminAsync(
            ClaimsPrincipal actor,
            string userId,
            string role)
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var controller = new AdminController(users, null!, null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = actor }
                }
            };
            var method = typeof(AdminController).GetMethod(
                "ApplyRoleChangeAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task<(bool Ok, string Message)>)method!.Invoke(controller, [userId, role])!;
            return await task;
        }

        public async Task<(ClaimsPrincipal? Principal, bool ShouldRenew)> RefreshCookieAsync(ClaimsPrincipal stale)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _provider };
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

            var ticket = new AuthenticationTicket(
                stale,
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddMinutes(-1) },
                IdentityConstants.ApplicationScheme);
            var context = new CookieValidatePrincipalContext(
                httpContext,
                new AuthenticationScheme(
                    IdentityConstants.ApplicationScheme,
                    displayName: null,
                    typeof(CookieAuthenticationHandler)),
                new CookieAuthenticationOptions(),
                ticket);

            await _provider.GetRequiredService<ISecurityStampValidator>().ValidateAsync(context);
            return (context.Principal, context.ShouldRenew);
        }

        public async Task<ApplicationUser> FindUserAsync(string userId)
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            return (await users.FindByIdAsync(userId))!;
        }

        public async Task<IList<string>> RolesForAsync(string userId)
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId);
            return await users.GetRolesAsync(user!);
        }

        public async Task<ClaimsPrincipal> CurrentPrincipalAsync()
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var signIn = _provider.GetRequiredService<SignInManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(_user.Id);
            return await signIn.CreateUserPrincipalAsync(user!);
        }

        public async Task<string?> StampAsync()
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(_user.Id);
            return user?.SecurityStamp;
        }

        public async Task<IList<string>> RolesAsync()
        {
            var users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(_user.Id);
            return await users.GetRolesAsync(user!);
        }

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
    }
}
