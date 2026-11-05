using System.ComponentModel.DataAnnotations;
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
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinalProject.Tests;

public class AccountDeleteTests
{
    [Fact]
    public void DeleteAccount_requires_authentication_and_antiforgery()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.DeleteAccount))!;
        Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
    }

    [Fact]
    public void DeleteAccountViewModel_requires_password_and_checked_confirmation()
    {
        var empty = new DeleteAccountViewModel();
        var emptyResults = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(empty, new ValidationContext(empty), emptyResults, validateAllProperties: true));
        Assert.Contains(emptyResults, result => result.MemberNames.Contains(nameof(DeleteAccountViewModel.CurrentPassword)));
        Assert.Contains(emptyResults, result => result.MemberNames.Contains(nameof(DeleteAccountViewModel.ConfirmDeactivation)));

        var uncheckedConfirm = new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = false
        };
        var uncheckedResults = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(
            uncheckedConfirm,
            new ValidationContext(uncheckedConfirm),
            uncheckedResults,
            validateAllProperties: true));
        Assert.Contains(
            uncheckedResults,
            result => result.ErrorMessage == "Confirm that you want to deactivate this account.");

        var valid = new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        };
        Assert.True(Validator.TryValidateObject(valid, new ValidationContext(valid), new List<ValidationResult>(), validateAllProperties: true));
    }

    [Fact]
    public async Task DeleteAccount_unchecked_confirmation_returns_profile_with_error()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("confirm-box@example.com", "Password1");
        var controller = harness.CreateAccountController(user);
        controller.ModelState.AddModelError(
            nameof(DeleteAccountViewModel.ConfirmDeactivation),
            "Confirm that you want to deactivate this account.");

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = false
        }) as ViewResult;

        Assert.Equal(nameof(AccountController.Profile), result!.ViewName);
        Assert.Equal(true, controller.ViewData["DeleteAccountAttempted"]);
        Assert.True(controller.ModelState.ContainsKey(nameof(DeleteAccountViewModel.ConfirmDeactivation)));
        Assert.False((await harness.Users.FindByIdAsync(user.Id))!.IsDeleted);
    }

    [Fact]
    public void Profile_delete_form_surfaces_validation_errors_outside_collapsed_details()
    {
        var root = FindRepoRoot();
        var view = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "Profile.cshtml"));

        Assert.Contains("id=\"deleteAccountErrors\"", view, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"DeleteAccount\"", view, StringComparison.Ordinal);
        Assert.Contains("name=\"ConfirmDeactivation\"", view, StringComparison.Ordinal);
        Assert.Contains("name=\"CurrentPassword\"", view, StringComparison.Ordinal);
        Assert.Contains("MustBeTrue", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Models", "AccountViewModels.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Range(typeof(bool), \"true\", \"true\"",
            File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Models", "AccountViewModels.cs")),
            StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.MVC", "Views", "Account", "Profile.cshtml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    [Fact]
    public async Task Member_can_soft_delete_own_account_with_correct_password()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("member-delete@example.com", "Password1");
        var controller = harness.CreateAccountController(user);

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        }) as RedirectToActionResult;

        Assert.Equal(nameof(AccountController.SignIn), result!.ActionName);
        Assert.Equal("Your account has been deactivated. You can no longer sign in.", controller.TempData["Success"]);

        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded!.IsDeleted);
        Assert.NotNull(reloaded.DeletedAt);
        Assert.True(reloaded.LockoutEnabled);
        Assert.True(reloaded.LockoutEnd.HasValue && reloaded.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DeleteAccount_rejects_incorrect_password_without_deactivating()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("bad-pass@example.com", "Password1");
        var controller = harness.CreateAccountController(user);

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Wrong-password-1",
            ConfirmDeactivation = true
        }) as ViewResult;

        Assert.Equal(nameof(AccountController.Profile), result!.ViewName);
        Assert.Equal(true, controller.ViewData["DeleteAccountAttempted"]);
        Assert.True(controller.ModelState.ContainsKey(nameof(DeleteAccountViewModel.CurrentPassword)));
        Assert.Contains(
            "The password is incorrect.",
            controller.ModelState[nameof(DeleteAccountViewModel.CurrentPassword)]!.Errors.Select(error => error.ErrorMessage));

        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.False(reloaded!.IsDeleted);
        Assert.Null(reloaded.DeletedAt);
    }

    [Fact]
    public async Task DeleteAccount_empty_password_returns_profile_with_error()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("empty-pass@example.com", "Password1");
        var controller = harness.CreateAccountController(user);

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "   ",
            ConfirmDeactivation = true
        }) as ViewResult;

        Assert.Equal(nameof(AccountController.Profile), result!.ViewName);
        Assert.Equal(true, controller.ViewData["DeleteAccountAttempted"]);
        Assert.Contains(
            "Enter your current password.",
            controller.ModelState[nameof(DeleteAccountViewModel.CurrentPassword)]!.Errors.Select(error => error.ErrorMessage));
        Assert.False((await harness.Users.FindByIdAsync(user.Id))!.IsDeleted);
    }

    [Fact]
    public async Task DeleteAccount_challenges_unauthenticated_requests()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var controller = harness.CreateAccountController();

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        });

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task DeleteAccount_only_affects_the_signed_in_member()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var owner = await harness.CreateConfirmedMemberAsync("owner-delete@example.com", "Password1");
        var other = await harness.CreateConfirmedMemberAsync("other-keep@example.com", "Password1");
        var controller = harness.CreateAccountController(owner);

        await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        });

        var ownerReload = await harness.Users.FindByIdAsync(owner.Id);
        var otherReload = await harness.Users.FindByIdAsync(other.Id);
        Assert.True(ownerReload!.IsDeleted);
        Assert.False(otherReload!.IsDeleted);
        Assert.Null(otherReload.DeletedAt);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    public async Task Staff_roles_cannot_use_member_delete_account(string staffRole)
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedUserAsync($"staff-{staffRole.ToLowerInvariant()}@example.com", "Password1", staffRole);
        var controller = harness.CreateAccountController(user);

        var result = await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        });

        Assert.IsType<ForbidResult>(result);
        var reloaded = await harness.Users.FindByIdAsync(user.Id);
        Assert.False(reloaded!.IsDeleted);
    }

    [Fact]
    public async Task Soft_deleted_account_cannot_sign_in()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("gone@example.com", "Password1");
        var deleteController = harness.CreateAccountController(user);
        await deleteController.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        });

        var signInController = harness.CreateAccountController();
        var result = await signInController.SignIn("gone@example.com", "Password1") as RedirectToActionResult;

        Assert.Equal(nameof(AccountController.SignIn), result!.ActionName);
        Assert.Equal("This account has been deactivated and can no longer sign in.", signInController.TempData["Error"]);

        var check = await harness.SignIn.CheckPasswordSignInAsync(
            (await harness.Users.FindByIdAsync(user.Id))!,
            "Password1",
            lockoutOnFailure: true);
        Assert.False(check.Succeeded);
        Assert.True(check.IsLockedOut);
    }

    [Fact]
    public async Task Soft_delete_preserves_contact_requests_and_user_id_for_historical_records()
    {
        await using var harness = await DeleteHarness.CreateAsync();
        var user = await harness.CreateConfirmedMemberAsync("history@example.com", "Password1");

        // ContactRequests store UserId without cascading from AspNetUsers soft-delete.
        // Cars/conversations in the API DB also keep OwnerId/BuyerId/SellerId string references.
        var db = harness.Provider.GetRequiredService<ApplicationDbContext>();
        db.ContactRequests.Add(new ContactRequest
        {
            CustomerName = "History User",
            Email = user.Email!,
            Phone = "555-0100",
            Interest = "SUV inquiry",
            Message = "Please keep this historical contact request intact.",
            Status = ContactRequestStatus.New,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ownerId = user.Id;
        var conversationKey = $"buyer:{ownerId}|seller:other-seller";

        var controller = harness.CreateAccountController(user);
        await controller.DeleteAccount(new DeleteAccountViewModel
        {
            CurrentPassword = "Password1",
            ConfirmDeactivation = true
        });

        var reloadedUser = await harness.Users.FindByIdAsync(ownerId);
        Assert.NotNull(reloadedUser);
        Assert.True(reloadedUser!.IsDeleted);
        Assert.Equal(ownerId, reloadedUser.Id);

        var request = await db.ContactRequests.SingleAsync(r => r.UserId == ownerId);
        Assert.Equal("Please keep this historical contact request intact.", request.Message);
        Assert.Equal(ownerId, request.UserId);
        Assert.Contains(ownerId, conversationKey, StringComparison.Ordinal);
    }

    private sealed class DeleteHarness : IAsyncDisposable
    {
        private DeleteHarness(ServiceProvider provider)
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

        public static async Task<DeleteHarness> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("account-delete-" + Guid.NewGuid().ToString("N")));
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
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IAuthenticationService, NoOpAuthenticationService>();

            var provider = services.BuildServiceProvider();
            var harness = new DeleteHarness(provider);
            foreach (var role in AppRoles.All)
            {
                await harness.Roles.CreateAsync(new IdentityRole(role));
            }

            return harness;
        }

        public Task<ApplicationUser> CreateConfirmedMemberAsync(string email, string password) =>
            CreateConfirmedUserAsync(email, password, AppRoles.Member);

        public async Task<ApplicationUser> CreateConfirmedUserAsync(string email, string password, string role)
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
                throw new InvalidOperationException(string.Join(", ", created.Errors.Select(e => e.Description)));
            }

            var roleResult = await Users.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
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
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                        new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                    },
                    IdentityConstants.ApplicationScheme));
            }

            return new AccountController(
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
                        ActionName = "DeleteAccount"
                    }
                },
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    httpContext,
                    new NullTempDataProvider()),
                Url = new FixedUrlHelper()
            };
        }

        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private sealed class UnconfiguredEmailSender : IEmailSender
    {
        public bool IsConfigured => false;

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
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        public string? Action(UrlActionContext actionContext) => "/Account/SignIn";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
        public string? Link(string? routeName, object? values) => "/";
    }

    private sealed class NullTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
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
