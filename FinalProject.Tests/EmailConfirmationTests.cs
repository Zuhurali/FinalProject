using System.Reflection;
using System.Security.Claims;
using FinalProject.MVC.Configuration;
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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinalProject.Tests;

public class EmailConfirmationTests
{
    private const string PublicBase = "https://autovault.example";
    private const string Password = "Password1";

    [Fact]
    public async Task Registration_sends_a_confirmation_email_and_does_not_sign_in()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();

        var result = await controller.Register(
            "driver@example.com",
            "driver1",
            Password,
            Password,
            "Ada",
            "Vault");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.CheckEmail), redirect.ActionName);
        Assert.False(controller.User.Identity?.IsAuthenticated);

        var user = await harness.Users.FindByEmailAsync("driver@example.com");
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Equal(new[] { AppRoles.Member }, await harness.Users.GetRolesAsync(user));

        var message = Assert.Single(harness.Sender.Sent);
        Assert.Equal("driver@example.com", message.To);
        Assert.Equal(AccountEmailContent.ConfirmationSubject, message.Subject);
        Assert.Contains("Welcome to AutoVault.", message.Html, StringComparison.Ordinal);
        Assert.Contains("Confirm Email", message.Html, StringComparison.Ordinal);
        Assert.Contains("ignore this email", message.Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(PublicBase + "/Account/ConfirmEmail?", message.Text.Split("Confirm Email: ")[1].Trim(), StringComparison.Ordinal);
        Assert.DoesNotContain(Password, message.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unconfirmed_user_cannot_sign_in_until_a_valid_link_is_used()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);

        var blocked = await controller.SignIn("driver@example.com", Password, false);
        Assert.Equal(nameof(AccountController.SignIn), Assert.IsType<RedirectToActionResult>(blocked).ActionName);
        Assert.Equal("Please confirm your email address before signing in.", controller.TempData["Error"]);
        Assert.False((await harness.Users.FindByEmailAsync("driver@example.com"))!.EmailConfirmed);

        var wrongPassword = await controller.SignIn("driver@example.com", "Wrongpass1", false);
        Assert.Equal(nameof(AccountController.SignIn), Assert.IsType<RedirectToActionResult>(wrongPassword).ActionName);
        Assert.Equal("The email, username, or password is incorrect.", controller.TempData["Error"]);
        Assert.Null(controller.TempData["UnconfirmedEmail"]);

        var (userId, token) = ReadLink(harness.Sender.Sent[0].Text);
        var confirmed = await controller.ConfirmEmail(userId, token);
        Assert.Equal("ConfirmEmail", Assert.IsType<ViewResult>(confirmed).ViewName);
        Assert.True((await harness.Users.FindByEmailAsync("driver@example.com"))!.EmailConfirmed);

        var signedIn = await controller.SignIn("driver@example.com", Password, false);
        var home = Assert.IsType<RedirectToActionResult>(signedIn);
        Assert.Equal("Index", home.ActionName);
        Assert.Equal("Home", home.ControllerName);
    }

    [Fact]
    public async Task Invalid_reused_and_swapped_tokens_do_not_confirm_an_account()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);
        var other = new ApplicationUser
        {
            UserName = "other1",
            Email = "other@example.com",
            CreatedAt = DateTime.UtcNow
        };
        Assert.True((await harness.Users.CreateAsync(other, Password)).Succeeded);

        var (userId, token) = ReadLink(harness.Sender.Sent[0].Text);
        var invalid = await controller.ConfirmEmail(userId, "not-a-valid-token");
        Assert.Equal("ConfirmEmailFailed", Assert.IsType<ViewResult>(invalid).ViewName);
        Assert.False((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);

        var swapped = await controller.ConfirmEmail(other.Id, token);
        Assert.Equal("ConfirmEmailFailed", Assert.IsType<ViewResult>(swapped).ViewName);
        Assert.False((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);
        Assert.False((await harness.Users.FindByIdAsync(other.Id))!.EmailConfirmed);

        var first = await controller.ConfirmEmail(userId, token);
        Assert.Equal("ConfirmEmail", Assert.IsType<ViewResult>(first).ViewName);
        var second = await controller.ConfirmEmail(userId, token);
        Assert.Equal("ConfirmEmailFailed", Assert.IsType<ViewResult>(second).ViewName);
        Assert.True((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);
        Assert.False((await harness.Users.FindByIdAsync(other.Id))!.EmailConfirmed);
    }

    [Fact]
    public async Task Resend_sends_a_new_message_without_revealing_whether_the_email_exists()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);
        Assert.Single(harness.Sender.Sent);

        var known = await controller.ResendConfirmation(new ResendConfirmationViewModel { Email = "driver@example.com" });
        Assert.Equal(nameof(AccountController.CheckEmail), Assert.IsType<RedirectToActionResult>(known).ActionName);
        var knownMessage = controller.TempData["Success"]?.ToString();
        Assert.Equal(2, harness.Sender.Sent.Count);

        var throttled = await controller.ResendConfirmation(new ResendConfirmationViewModel { Email = "driver@example.com" });
        Assert.IsType<RedirectToActionResult>(throttled);
        Assert.Equal(knownMessage, controller.TempData["Success"]?.ToString());
        Assert.Equal(2, harness.Sender.Sent.Count);

        var unknown = await controller.ResendConfirmation(new ResendConfirmationViewModel { Email = "missing@example.com" });
        Assert.IsType<RedirectToActionResult>(unknown);
        Assert.Equal(knownMessage, controller.TempData["Success"]?.ToString());
        Assert.Equal(2, harness.Sender.Sent.Count);
        Assert.Null(await harness.Users.FindByEmailAsync("missing@example.com"));
        Assert.DoesNotContain("does not exist", knownMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Existing_staff_keep_their_roles_and_can_still_sign_in()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var admin = await harness.CreateStaffAsync("admin@example.com", Password, AppRoles.Admin, emailConfirmed: true);
        var superAdmin = await harness.CreateStaffAsync("super@example.com", Password, AppRoles.SuperAdmin, emailConfirmed: false);
        var member = await harness.CreateStaffAsync("member@example.com", Password, AppRoles.Member, emailConfirmed: false);

        await IdentityRoleSeeder.SeedAsync(
            harness.Provider,
            [
                ("super@example.com", "Different1", AppRoles.SuperAdmin, false),
                ("admin@example.com", "Different1", AppRoles.Admin, false)
            ]);

        admin = (await harness.Users.FindByIdAsync(admin.Id))!;
        superAdmin = (await harness.Users.FindByIdAsync(superAdmin.Id))!;
        member = (await harness.Users.FindByIdAsync(member.Id))!;
        Assert.True(admin.EmailConfirmed);
        Assert.True(superAdmin.EmailConfirmed);
        Assert.False(member.EmailConfirmed);
        Assert.Equal(new[] { AppRoles.Admin }, await harness.Users.GetRolesAsync(admin));
        Assert.Equal(new[] { AppRoles.SuperAdmin }, await harness.Users.GetRolesAsync(superAdmin));
        Assert.Equal(new[] { AppRoles.Member }, await harness.Users.GetRolesAsync(member));
        Assert.True(await harness.Users.CheckPasswordAsync(admin, Password));
        Assert.True(await harness.Users.CheckPasswordAsync(superAdmin, Password));
        Assert.False(await harness.Users.CheckPasswordAsync(superAdmin, "Different1"));

        var adminSignIn = await harness.CreateController().SignIn("admin@example.com", Password, false);
        Assert.Equal("Home", Assert.IsType<RedirectToActionResult>(adminSignIn).ControllerName);
        var superSignIn = await harness.CreateController().SignIn("super@example.com", Password, false);
        Assert.Equal("Home", Assert.IsType<RedirectToActionResult>(superSignIn).ControllerName);
        Assert.Equal(new[] { AppRoles.Admin }, await harness.Users.GetRolesAsync(admin));
        Assert.Equal(new[] { AppRoles.SuperAdmin }, await harness.Users.GetRolesAsync(superAdmin));
    }

    [Fact]
    public void Confirmation_posts_keep_antiforgery_protection()
    {
        var register = typeof(AccountController).GetMethods()
            .Single(method => method.Name == nameof(AccountController.Register)
                && method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Any());
        Assert.NotNull(register.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: false).SingleOrDefault());
        var resend = typeof(AccountController).GetMethods()
            .Single(method => method.Name == nameof(AccountController.ResendConfirmation)
                && method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Any());
        Assert.NotNull(resend.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: false).SingleOrDefault());
        Assert.Null(resend.GetCustomAttribute<AuthorizeAttribute>());
    }

    private static (string UserId, string Token) ReadLink(string text)
    {
        var url = text.Split("Confirm Email: ", StringSplitOptions.None)[1].Trim().Split('\n', '\r')[0].Trim();
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        return (query["userId"].ToString(), query["token"].ToString());
    }

    private sealed class EmailHarness : IAsyncDisposable
    {
        private EmailHarness(ServiceProvider provider, RecordingEmailSender sender)
        {
            Provider = provider;
            Sender = sender;
            Users = provider.GetRequiredService<UserManager<ApplicationUser>>();
            SignIn = provider.GetRequiredService<SignInManager<ApplicationUser>>();
            Roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
            Cache = provider.GetRequiredService<IMemoryCache>();
        }

        public ServiceProvider Provider { get; }
        public RecordingEmailSender Sender { get; }
        public UserManager<ApplicationUser> Users { get; }
        public SignInManager<ApplicationUser> SignIn { get; }
        public RoleManager<IdentityRole> Roles { get; }
        public IMemoryCache Cache { get; }

        public static async Task<EmailHarness> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("email-confirm-" + Guid.NewGuid().ToString("N")));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IAuthenticationService, NoOpAuthenticationService>();
            var provider = services.BuildServiceProvider();
            var harness = new EmailHarness(provider, new RecordingEmailSender());
            foreach (var role in AppRoles.All)
            {
                await harness.Roles.CreateAsync(new IdentityRole(role));
            }

            return harness;
        }

        public async Task<ApplicationUser> CreateStaffAsync(string email, string password, string role, bool emailConfirmed)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = emailConfirmed,
                CreatedAt = DateTime.UtcNow
            };
            var created = await Users.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
            Assert.True((await Users.AddToRoleAsync(user, role)).Succeeded);
            return (await Users.FindByIdAsync(user.Id))!;
        }

        public AccountController CreateController()
        {
            var httpContext = new DefaultHttpContext { RequestServices = Provider };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            Provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
            return new AccountController(
                Users,
                SignIn,
                Roles,
                marketplaceApi: null!,
                notificationApi: null!,
                carApi: null!,
                tokenService: null!,
                emailSender: Sender,
                environment: new DevelopmentEnvironment(),
                cache: Cache,
                conversations: null!,
                logger: NullLogger<AccountController>.Instance,
                activityHistory: new AccountActivityHistory(Users),
                contactRequests: null!,
                shopCatalog: null!,
                carImages: null!,
                applicationSettings: Options.Create(new ApplicationSettings { PublicBaseUrl = PublicBase }))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext,
                    RouteData = new RouteData(),
                    ActionDescriptor = new ControllerActionDescriptor()
                },
                TempData = new TempDataDictionary(httpContext, new DictionaryTempDataProvider()),
                Url = new RecordingUrlHelper()
            };
        }

        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Html, string Text)> Sent { get; } = new();

        public bool IsConfigured => true;

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((toEmail, subject, htmlBody, textBody));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        public string? Action(UrlActionContext actionContext)
        {
            var values = new RouteValueDictionary(actionContext.Values);
            var query = string.Join("&", values
                .Where(pair => pair.Value != null)
                .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!.ToString() ?? string.Empty)}"));
            var path = $"/Account/{actionContext.Action}?{query}";
            if (string.IsNullOrEmpty(actionContext.Protocol))
            {
                return path;
            }

            var host = string.IsNullOrEmpty(actionContext.Host) ? "localhost" : actionContext.Host;
            return $"{actionContext.Protocol}://{host}{path}";
        }

        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true;
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
        public string? Link(string? routeName, object? values) => "/";
    }

    private sealed class DictionaryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
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
