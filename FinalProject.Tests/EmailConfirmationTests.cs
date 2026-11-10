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
    public async Task Expired_confirmation_tokens_are_rejected()
    {
        await using var harness = await EmailHarness.CreateAsync(tokenLifespan: TimeSpan.FromMilliseconds(1));
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);
        var (userId, token) = ReadLink(harness.Sender.Sent[0].Text);

        await Task.Delay(50);

        var expired = await controller.ConfirmEmail(userId, token);
        Assert.Equal("ConfirmEmailFailed", Assert.IsType<ViewResult>(expired).ViewName);
        Assert.False((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);

        var blocked = await controller.SignIn("driver@example.com", Password, false);
        Assert.Equal(nameof(AccountController.SignIn), Assert.IsType<RedirectToActionResult>(blocked).ActionName);
        Assert.Equal("Please confirm your email address before signing in.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Production_does_not_expose_development_confirmation_links()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var unconfigured = new UnconfiguredEmailSender();
        var controller = harness.CreateController(
            emailSender: unconfigured,
            environment: new FixedEnvironment(Environments.Production));

        var result = await controller.Register("driver@example.com", "driver1", Password, Password);
        Assert.Equal(nameof(AccountController.CheckEmail), Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Null(controller.TempData["DevelopmentConfirmationLink"]);
        Assert.Empty(unconfigured.Sent);

        var checkEmail = controller.CheckEmail("driver@example.com");
        var view = Assert.IsType<ViewResult>(checkEmail);
        Assert.Null(view.ViewData["DevelopmentConfirmationLink"]);
        Assert.False(view.ViewData["EmailConfigured"] as bool?);
        Assert.False(view.ViewData["DevelopmentMailboxEnabled"] as bool?);
        Assert.False((await harness.Users.FindByEmailAsync("driver@example.com"))!.EmailConfirmed);
    }

    [Fact]
    public async Task Development_mailbox_delivers_confirmation_mail_without_shortcut_links()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var store = new DevelopmentMailStore();
        var mailboxSender = new DevelopmentMailboxEmailSender(
            store,
            NullLogger<DevelopmentMailboxEmailSender>.Instance);
        var requestServices = new ServiceCollection()
            .AddSingleton<IDevelopmentMailStore>(store)
            .BuildServiceProvider();

        var controller = harness.CreateController(
            emailSender: mailboxSender,
            environment: new FixedEnvironment(Environments.Development));
        controller.ControllerContext.HttpContext.RequestServices = requestServices;

        var result = await controller.Register("driver@example.com", "driver1", Password, Password);
        Assert.Equal(nameof(AccountController.CheckEmail), Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Null(controller.TempData["DevelopmentConfirmationLink"]);
        Assert.Equal(false, controller.TempData["ExternalEmailDelivered"]);
        Assert.Equal(true, controller.TempData["LocalMailboxStored"]);

        var delivered = Assert.Single(store.List("driver@example.com"));
        Assert.Equal(AccountEmailContent.ConfirmationSubject, delivered.Subject);
        Assert.Contains("/Account/ConfirmEmail?", delivered.TextBody, StringComparison.Ordinal);

        var checkEmail = controller.CheckEmail("driver@example.com");
        var view = Assert.IsType<ViewResult>(checkEmail);
        Assert.True(view.ViewData["DevelopmentMailboxEnabled"] as bool?);
        Assert.True(view.ViewData["EmailConfigured"] as bool?);
        Assert.False(view.ViewData["ExternalProviderConfigured"] as bool?);
        Assert.False(view.ViewData["ExternalEmailDelivered"] as bool?);
        Assert.Null(view.ViewData["DevelopmentConfirmationLink"]);

        var (userId, token) = ReadLink(delivered.TextBody);
        var confirmed = await controller.ConfirmEmail(userId, token);
        Assert.Equal("ConfirmEmail", Assert.IsType<ViewResult>(confirmed).ViewName);
        Assert.True((await harness.Users.FindByEmailAsync("driver@example.com"))!.EmailConfirmed);
    }

    [Fact]
    public async Task External_provider_success_is_reported_separately_from_mailbox_copies()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var store = new DevelopmentMailStore();
        var external = new RecordingEmailSender();
        var composite = new CompositeEmailSender(
            external,
            new FixedEnvironment(Environments.Development),
            Options.Create(new EmailSettings
            {
                Host = "smtp.resend.com",
                Port = 465,
                Username = "resend",
                Password = "re_test_key_not_real",
                FromEmail = "beth.t@example.com",
                Provider = "Resend",
                Encryption = "SslOnConnect"
            }),
            NullLogger<CompositeEmailSender>.Instance,
            new[] { store });

        var requestServices = new ServiceCollection()
            .AddSingleton<IDevelopmentMailStore>(store)
            .BuildServiceProvider();
        var controller = harness.CreateController(
            emailSender: composite,
            environment: new FixedEnvironment(Environments.Development));
        controller.ControllerContext.HttpContext.RequestServices = requestServices;

        await controller.Register("member@temp-mail.org", "tempuser1", Password, Password);
        Assert.Equal(true, controller.TempData["ExternalEmailDelivered"]);
        Assert.Equal(true, controller.TempData["ExternalProviderConfigured"]);
        Assert.Equal(true, controller.TempData["LocalMailboxStored"]);
        Assert.Null(controller.TempData["Error"]);
        Assert.Single(external.Sent);
        Assert.Equal("member@temp-mail.org", external.Sent[0].To);
        Assert.Single(store.List("member@temp-mail.org"));

        var checkEmail = controller.CheckEmail("member@temp-mail.org");
        var view = Assert.IsType<ViewResult>(checkEmail);
        Assert.True(view.ViewData["ExternalEmailDelivered"] as bool?);
        Assert.True(view.ViewData["DevelopmentMailboxEnabled"] as bool?);
    }

    [Fact]
    public async Task External_provider_failure_is_not_reported_as_successful_delivery()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var store = new DevelopmentMailStore();
        var requestServices = new ServiceCollection()
            .AddSingleton<IDevelopmentMailStore>(store)
            .BuildServiceProvider();
        var controller = harness.CreateController(
            emailSender: new FailingExternalEmailSender(),
            environment: new FixedEnvironment(Environments.Development));
        controller.ControllerContext.HttpContext.RequestServices = requestServices;

        await controller.Register("driver@example.com", "driver1", Password, Password);
        Assert.Equal(false, controller.TempData["ExternalEmailDelivered"]);
        Assert.Equal(true, controller.TempData["ExternalProviderConfigured"]);
        Assert.Equal(
            "The email could not be delivered through the configured SMTP server.",
            controller.TempData["Error"]);
        Assert.False((await harness.Users.FindByEmailAsync("driver@example.com"))!.EmailConfirmed);
    }

    [Fact]
    public async Task Resend_sends_a_new_message_without_revealing_whether_the_email_exists()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);
        Assert.Single(harness.Sender.Sent);

        var known = await controller.ResendConfirmation(new ResendConfirmationViewModel { Email = "driver@example.com" });
        var knownRedirect = Assert.IsType<RedirectToActionResult>(known);
        Assert.Equal(nameof(AccountController.CheckEmail), knownRedirect.ActionName);
        Assert.Equal("driver@example.com", knownRedirect.RouteValues?["email"]);
        Assert.Equal(true, controller.TempData["ExternalEmailDelivered"]);
        Assert.Null(controller.TempData["Success"]);
        Assert.Equal(2, harness.Sender.Sent.Count);

        // Fresh controller: in-app TempData persists across calls in this harness.
        var throttledController = harness.CreateController();
        var throttled = await throttledController.ResendConfirmation(new ResendConfirmationViewModel { Email = "driver@example.com" });
        Assert.IsType<RedirectToActionResult>(throttled);
        var throttledMessage = throttledController.TempData["Success"]?.ToString();
        Assert.Equal("If driver@example.com is waiting for confirmation, check that inbox and spam folder.", throttledMessage);
        Assert.Equal(2, harness.Sender.Sent.Count);

        var unknownController = harness.CreateController();
        var unknown = await unknownController.ResendConfirmation(new ResendConfirmationViewModel { Email = "missing@example.com" });
        Assert.IsType<RedirectToActionResult>(unknown);
        var unknownMessage = unknownController.TempData["Success"]?.ToString();
        Assert.Equal("If missing@example.com is waiting for confirmation, check that inbox and spam folder.", unknownMessage);
        Assert.Equal(2, harness.Sender.Sent.Count);
        Assert.Null(await harness.Users.FindByEmailAsync("missing@example.com"));
        Assert.DoesNotContain("does not exist", unknownMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resend_issues_a_new_token_that_confirms_and_retires_the_previous_link()
    {
        await using var harness = await EmailHarness.CreateAsync();
        var controller = harness.CreateController();
        await controller.Register("driver@example.com", "driver1", Password, Password);
        var (userId, originalToken) = ReadLink(harness.Sender.Sent[0].Text);

        await controller.ResendConfirmation(new ResendConfirmationViewModel { Email = "driver@example.com" });
        var (resentUserId, freshToken) = ReadLink(harness.Sender.Sent[1].Text);
        Assert.Equal(userId, resentUserId);
        Assert.NotEqual(originalToken, freshToken);

        var stale = await controller.ConfirmEmail(userId, originalToken);
        Assert.Equal("ConfirmEmailFailed", Assert.IsType<ViewResult>(stale).ViewName);
        Assert.False((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);

        var blocked = await controller.SignIn("driver@example.com", Password, false);
        Assert.Equal(nameof(AccountController.SignIn), Assert.IsType<RedirectToActionResult>(blocked).ActionName);
        Assert.Equal("Please confirm your email address before signing in.", controller.TempData["Error"]);

        var confirmed = await controller.ConfirmEmail(userId, freshToken);
        Assert.Equal("ConfirmEmail", Assert.IsType<ViewResult>(confirmed).ViewName);
        Assert.True((await harness.Users.FindByIdAsync(userId))!.EmailConfirmed);

        var signedIn = await controller.SignIn("driver@example.com", Password, false);
        Assert.Equal("Home", Assert.IsType<RedirectToActionResult>(signedIn).ControllerName);
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

    [Fact]
    public void Public_auth_pages_do_not_link_the_development_mailbox()
    {
        var root = FindRepositoryRoot();
        var account = Path.Combine(root, "FinalProject.MVC", "Views", "Account");
        foreach (var name in new[] { "CheckEmail.cshtml", "ConfirmEmail.cshtml", "ConfirmEmailFailed.cshtml", "ResendConfirmation.cshtml", "SignIn.cshtml", "Register.cshtml" })
        {
            var text = File.ReadAllText(Path.Combine(account, name));
            Assert.DoesNotContain("DevMailbox", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Open Development Mailbox", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Development Mailbox", text, StringComparison.Ordinal);
        }

        var signIn = File.ReadAllText(Path.Combine(account, "SignIn.cshtml"));
        Assert.Contains("Resend confirmation email", signIn, StringComparison.Ordinal);
        var resend = File.ReadAllText(Path.Combine(account, "ResendConfirmation.cshtml"));
        Assert.Contains("Send confirmation email", resend, StringComparison.Ordinal);
        var check = File.ReadAllText(Path.Combine(account, "CheckEmail.cshtml"));
        Assert.Contains("Confirm your account", check, StringComparison.Ordinal);

        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "AccountController.cs"));
        Assert.DoesNotContain("open the Development Mailbox", controller, StringComparison.Ordinal);
        Assert.Contains("class DevMailboxController", File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "DevMailboxController.cs")), StringComparison.Ordinal);
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

        public static async Task<EmailHarness> CreateAsync(TimeSpan? tokenLifespan = null)
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
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = tokenLifespan ?? TimeSpan.FromDays(1);
            });
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

        public AccountController CreateController(
            IEmailSender? emailSender = null,
            IWebHostEnvironment? environment = null)
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
                emailSender: emailSender ?? Sender,
                environment: environment ?? new FixedEnvironment(Environments.Development),
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

        public bool IsExternalConfigured => true;

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

    private sealed class UnconfiguredEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Html, string Text)> Sent { get; } = new();

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

    private sealed class FailingExternalEmailSender : IEmailSender
    {
        public bool IsConfigured => true;

        public bool IsExternalConfigured => true;

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The email could not be delivered through the configured SMTP server.");
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

    private sealed class FixedEnvironment : IWebHostEnvironment
    {
        public FixedEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string ApplicationName { get; set; } = "FinalProject.MVC";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; }
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
