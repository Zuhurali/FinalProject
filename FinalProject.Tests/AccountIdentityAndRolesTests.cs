using System.Reflection;
using System.Security.Claims;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ApiCarsController = FinalProject.Controllers.CarsController;

namespace FinalProject.Tests;

public class AccountIdentityAndRolesTests
{
    [Fact]
    public void Privileged_mvc_and_api_surfaces_require_Admin_or_SuperAdmin()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal($"{AppRoles.Admin},{AppRoles.SuperAdmin}", typeof(AdminController).GetMethod(nameof(AdminController.Users))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal($"{AppRoles.Admin},{AppRoles.SuperAdmin}", typeof(AdminController).GetMethod(nameof(AdminController.DeleteUser))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(AppRoles.SuperAdmin, typeof(AdminController).GetMethod(nameof(AdminController.ChangeRole))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminPartsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminCouponsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminSalesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(AdminOrdersController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(
            AppRoles.Admin + "," + AppRoles.SuperAdmin,
            typeof(AccountController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == nameof(AccountController.AddCar) && method.GetParameters().Length == 0)
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(
            "Admin,SuperAdmin",
            typeof(ApiCarsController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == nameof(ApiCarsController.Create) && method.GetParameters().Length >= 1)
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(
            "Admin,SuperAdmin",
            typeof(ApiCarsController).GetMethod(nameof(ApiCarsController.CreateCatalog))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(ApiCarsController).GetMethod(nameof(ApiCarsController.Delete))!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Private_account_surfaces_require_authentication()
    {
        foreach (var name in new[]
                 {
                     nameof(AccountController.Profile),
                     nameof(AccountController.ChangePassword),
                     nameof(AccountController.DeleteAccount),
                     nameof(AccountController.Favorites),
                     nameof(AccountController.ProductQuestions),
                     nameof(AccountController.Messages),
                     nameof(AccountController.ContactRequests)
                 })
        {
            var methods = typeof(AccountController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name == name)
                .ToList();
            Assert.NotEmpty(methods);
            Assert.All(methods, method => Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>()));
        }

        Assert.Null(typeof(AccountController).GetMethod(nameof(AccountController.ForgotPassword), Type.EmptyTypes)!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(AccountController).GetMethod(nameof(AccountController.ResetPassword), [typeof(string), typeof(string)])!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Registration_persists_member_and_rejects_duplicate_email()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var email = $"member-{Guid.NewGuid():N}@example.com";
        var username = "u" + Guid.NewGuid().ToString("N")[..10];

        var created = await harness.Users.CreateAsync(new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        }, "Password1");
        Assert.True(created.Succeeded);
        var user = await harness.Users.FindByEmailAsync(email);
        Assert.NotNull(user);
        await harness.Users.AddToRoleAsync(user!, AppRoles.Member);
        Assert.Contains(AppRoles.Member, await harness.Users.GetRolesAsync(user!));

        var duplicate = await harness.Users.CreateAsync(new ApplicationUser
        {
            UserName = "other-" + username,
            Email = email,
            EmailConfirmed = true
        }, "Password1");
        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, error => error.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task Login_succeeds_with_password_and_fails_with_wrong_password()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("login@example.com", "Password1");

        var ok = await harness.SignIn.CheckPasswordSignInAsync(user, "Password1", lockoutOnFailure: true);
        Assert.True(ok.Succeeded);

        var bad = await harness.SignIn.CheckPasswordSignInAsync(user, "Wrong-password-1", lockoutOnFailure: true);
        Assert.False(bad.Succeeded);
        Assert.False(bad.IsLockedOut);
    }

    [Fact]
    public async Task Failed_logins_lock_the_account_after_threshold()
    {
        await using var harness = await AccountHarness.CreateAsync(maxFailedAttempts: 3);
        var user = await harness.CreateConfirmedMemberAsync("lockout@example.com", "Password1");

        for (var i = 0; i < 3; i++)
        {
            var attempt = await harness.SignIn.CheckPasswordSignInAsync(user, "Wrong-password-1", lockoutOnFailure: true);
            Assert.False(attempt.Succeeded);
        }

        var locked = await harness.SignIn.CheckPasswordSignInAsync(user, "Wrong-password-1", lockoutOnFailure: true);
        Assert.True(locked.IsLockedOut);

        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.True(await harness.Users.IsLockedOutAsync(reloaded!));
    }

    [Fact]
    public async Task Change_password_updates_store_and_rejects_wrong_current_password()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("change@example.com", "Password1");

        var wrong = await harness.Users.ChangePasswordAsync(user, "Not-the-password-1", "Password2");
        Assert.False(wrong.Succeeded);

        var changed = await harness.Users.ChangePasswordAsync(user, "Password1", "Password2");
        Assert.True(changed.Succeeded);
        Assert.True(await harness.Users.CheckPasswordAsync(user, "Password2"));
        Assert.False(await harness.Users.CheckPasswordAsync(user, "Password1"));
    }

    [Fact]
    public async Task Successful_login_change_and_reset_record_activity_timestamps()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("activity@example.com", "Password1");
        Assert.Null(user.LastLoginAt);
        Assert.Null(user.LastPasswordChangedAt);
        Assert.Null(user.LastPasswordResetAt);

        var beforeLogin = DateTime.UtcNow.AddSeconds(-2);
        var signInResult = await harness.CreateAccountController().SignIn(
            "activity@example.com",
            "Password1",
            rememberMe: false) as RedirectToActionResult;
        Assert.Equal("Index", signInResult!.ActionName);
        Assert.Equal("Home", signInResult.ControllerName);

        var afterLogin = await harness.Users.FindByIdAsync(user.Id);
        Assert.NotNull(afterLogin!.LastLoginAt);
        Assert.True(afterLogin.LastLoginAt >= beforeLogin);

        var beforeChange = DateTime.UtcNow.AddSeconds(-2);
        var changeResult = await harness.CreateAccountController(afterLogin).ChangePassword(
            new ChangePasswordViewModel
            {
                CurrentPassword = "Password1",
                NewPassword = "Password2",
                ConfirmPassword = "Password2"
            }) as RedirectToActionResult;
        Assert.Equal(nameof(AccountController.ChangePassword), changeResult!.ActionName);

        var afterChange = await harness.Users.FindByIdAsync(user.Id);
        Assert.NotNull(afterChange!.LastPasswordChangedAt);
        Assert.True(afterChange.LastPasswordChangedAt >= beforeChange);
        Assert.Null(afterChange.LastPasswordResetAt);

        var token = await harness.Users.GeneratePasswordResetTokenAsync(afterChange);
        var beforeReset = DateTime.UtcNow.AddSeconds(-2);
        var resetResult = await harness.CreateAccountController().ResetPassword(
            new ResetPasswordViewModel
            {
                UserId = afterChange.Id,
                Token = token,
                Password = "Password9",
                ConfirmPassword = "Password9"
            }) as RedirectToActionResult;
        Assert.Equal(nameof(AccountController.SignIn), resetResult!.ActionName);

        var afterReset = await harness.Users.FindByIdAsync(user.Id);
        Assert.NotNull(afterReset!.LastPasswordResetAt);
        Assert.True(afterReset.LastPasswordResetAt >= beforeReset);
        Assert.Equal(afterChange.LastPasswordChangedAt, afterReset.LastPasswordChangedAt);
    }

    [Fact]
    public async Task Failed_login_does_not_set_LastLoginAt()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("fail-login@example.com", "Password1");
        await harness.CreateAccountController().SignIn(
            "fail-login@example.com",
            "Wrong-password-1",
            rememberMe: false);
        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.Null(reloaded!.LastLoginAt);
    }

    [Fact]
    public async Task Password_reset_token_updates_password_without_revealing_unknown_users_in_controller()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("reset@example.com", "Password1");
        var token = await harness.Users.GeneratePasswordResetTokenAsync(user);
        var reset = await harness.Users.ResetPasswordAsync(user, token, "Password9");
        Assert.True(reset.Succeeded);
        Assert.True(await harness.Users.CheckPasswordAsync(user, "Password9"));

        var controller = harness.CreateAccountController();
        var known = await controller.ForgotPassword(new ForgotPasswordViewModel { Email = "reset@example.com" }) as RedirectToActionResult;
        var unknown = await controller.ForgotPassword(new ForgotPasswordViewModel { Email = "missing@example.com" }) as RedirectToActionResult;
        Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), known!.ActionName);
        Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), unknown!.ActionName);
        Assert.Equal(known.RouteValues?["email"], "reset@example.com");
        Assert.Equal(unknown.RouteValues?["email"], "missing@example.com");
    }

    [Fact]
    public async Task Profile_update_persists_name_and_ignores_posted_email()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("profile@example.com", "Password1");
        user.FirstName = "Before";
        user.LastName = "User";
        user.PhoneNumber = "555-0100";
        await harness.Users.UpdateAsync(user);

        var controller = harness.CreateAccountController(user);
        var result = await controller.Profile(new AccountProfileViewModel
        {
            FirstName = "After",
            LastName = "Name",
            Email = "attacker@example.com",
            PhoneNumber = null
        }) as RedirectToActionResult;

        Assert.Equal(nameof(AccountController.Profile), result!.ActionName);
        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.Equal("After", reloaded!.FirstName);
        Assert.Equal("Name", reloaded.LastName);
        Assert.Equal("555-0100", reloaded.PhoneNumber);
        Assert.Equal("profile@example.com", reloaded.Email);
    }

    [Fact]
    public async Task Profile_is_scoped_to_the_signed_in_user_only()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var owner = await harness.CreateConfirmedMemberAsync("owner@example.com", "Password1");
        var other = await harness.CreateConfirmedMemberAsync("other@example.com", "Password1");
        other.FirstName = "Other";
        await harness.Users.UpdateAsync(other);

        var controller = harness.CreateAccountController(owner);
        await controller.Profile(new AccountProfileViewModel
        {
            FirstName = "OwnerOnly",
            LastName = "Updated",
            Email = other.Email!,
            PhoneNumber = null
        });

        var ownerReload = await harness.Users.FindByIdAsync(owner.Id);
        var otherReload = await harness.Users.FindByIdAsync(other.Id);
        Assert.Equal("OwnerOnly", ownerReload!.FirstName);
        Assert.Equal("Other", otherReload!.FirstName);
        Assert.Equal("other@example.com", otherReload.Email);
    }

    [Fact]
    public async Task Sign_out_redirects_home_after_clearing_session()
    {
        await using var harness = await AccountHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("logout@example.com", "Password1");
        var controller = harness.CreateAccountController(user);
        var result = await controller.SignOutUser() as RedirectToActionResult;
        Assert.Equal("Index", result!.ActionName);
        Assert.Equal("Home", result.ControllerName);
        Assert.NotNull(typeof(AccountController).GetMethod(nameof(AccountController.SignOutUser))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    private sealed class AccountHarness : IAsyncDisposable
    {
        private AccountHarness(ServiceProvider provider)
        {
            Provider = provider;
            Users = provider.GetRequiredService<UserManager<ApplicationUser>>();
            SignIn = provider.GetRequiredService<SignInManager<ApplicationUser>>();
            Roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
            Cache = provider.GetRequiredService<IMemoryCache>();
        }

        public ServiceProvider Provider { get; }
        public UserManager<ApplicationUser> Users { get; }
        public SignInManager<ApplicationUser> SignIn { get; }
        public RoleManager<IdentityRole> Roles { get; }
        public IMemoryCache Cache { get; }

        public static async Task<AccountHarness> CreateAsync(int maxFailedAttempts = 5)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("account-identity-" + Guid.NewGuid().ToString("N")));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IAuthenticationService, NoOpAuthenticationService>();

            var provider = services.BuildServiceProvider();
            var harness = new AccountHarness(provider);
            foreach (var role in AppRoles.All)
            {
                await harness.Roles.CreateAsync(new IdentityRole(role));
            }

            return harness;
        }

        public async Task<ApplicationUser> CreateConfirmedMemberAsync(string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var created = await Users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", created.Errors.Select(error => error.Description)));
            }

            var role = await Users.AddToRoleAsync(user, AppRoles.Member);
            if (!role.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", role.Errors.Select(error => error.Description)));
            }

            return (await Users.FindByIdAsync(user.Id))!;
        }

        public AccountController CreateAccountController(ApplicationUser? user = null)
        {
            var httpContext = new DefaultHttpContext { RequestServices = Provider };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            Provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

            if (user != null)
            {
                var principal = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                        new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                    },
                    IdentityConstants.ApplicationScheme));
                httpContext.User = principal;
            }

            var controller = new AccountController(
                Users,
                SignIn,
                Roles,
                marketplaceApi: null!,
                notificationApi: null!,
                carApi: null!,
                tokenService: null!,
                emailSender: new UnconfiguredEmailSender(),
                environment: new DevelopmentEnvironment(),
                cache: Cache,
                conversations: null!,
                logger: NullLogger<AccountController>.Instance,
                activityHistory: new AccountActivityHistory(Users),
                contactRequests: null!,
                shopCatalog: null!,
                carImages: null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext,
                    RouteData = new RouteData(),
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        ControllerName = "Account",
                        ActionName = "Profile"
                    }
                },
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    httpContext,
                    new NullTempDataProvider()),
                Url = new FixedUrlHelper()
            };

            return controller;
        }

        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private sealed class UnconfiguredEmailSender : IEmailSender
    {
        public bool IsConfigured => false;

        public bool IsExternalConfigured => false;

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Email delivery is not configured.");
    }

    private sealed class DevelopmentEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.MVC";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        public string? Action(UrlActionContext actionContext) =>
            $"https://localhost/Account/{actionContext.Action}?userId=test&token=test";

        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true;
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
        public string? Link(string? routeName, object? values) => "/";
    }

    private sealed class NullTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class NoOpAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }
}
