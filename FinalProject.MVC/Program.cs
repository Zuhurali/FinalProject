using FinalProject.MVC.Configuration;
using FinalProject.MVC.Data;
using FinalProject.MVC.Hosting;
using FinalProject.MVC.Hubs;
using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Repositories.Implementations;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024 * 1024;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 64 * 1024 * 1024;
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            // ApplicationDbContext owns Identity, shop, sales, and CMS tables.
            // Its history table stays __EFMigrationsHistory. Do not apply API migrations here.
            sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
        }));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings (you can adjust these)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = true;

    // Lock out after repeated failed password attempts.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Email confirmation and password-reset tokens expire after one day.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(1);
});

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/SignIn";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Validate the security stamp on every request and replace the principal with
// claims loaded from the store. Role edits do not rotate the stamp, so the
// session continues and the next request picks up the new roles. A stamp
// mismatch still rejects the cookie.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");
}

builder.Services.AddHttpClient<ICarApiService, CarApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddHttpClient<IProductRatingApiService, ProductRatingApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddHttpClient<IProductReviewApiService, ProductReviewApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddHttpClient<IMarketplaceApiService, MarketplaceApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddHttpClient<INotificationApiService, NotificationApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddHttpClient<IAdminApiService, AdminApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(30),
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
_ = ApiSigningKey.RequireConfigured(builder.Configuration);
builder.Services.AddSingleton<IApiAccessTokenService, ApiAccessTokenService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPanel", policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy("CatalogCreate", policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy("ManageUsers", policy => policy.RequireRole(AppRoles.SuperAdmin));
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.PostConfigure<EmailSettings>(settings => settings.ApplyKnownProviderDefaults());
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection(ApplicationSettings.SectionName));
builder.Services.Configure<DealershipContactSettings>(
    builder.Configuration.GetSection(DealershipContactSettings.SectionName));

// MailKit SMTP (Gmail/Resend/Brevo/etc.) plus, in Development, an in-app mailbox copy.
// External delivery is attempted only when EmailSettings is configured; the mailbox
// never pretends to be an external provider response.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IDevelopmentMailStore, DevelopmentMailStore>();
}

builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<IEmailSender>(sp =>
{
    var smtp = sp.GetRequiredService<EmailSender>();
    return new CompositeEmailSender(
        smtp,
        sp.GetRequiredService<IWebHostEnvironment>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>(),
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeEmailSender>>(),
        sp.GetServices<IDevelopmentMailStore>());
});

builder.Services.AddScoped<IContactInquiryMailer, ContactInquiryMailer>();
builder.Services.AddScoped<ICarImageUploadService, CarImageUploadService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApplicationRepository>();
builder.Services.AddScoped<IApplicationRepository>(sp => sp.GetRequiredService<ApplicationRepository>());
builder.Services.AddScoped<IShopCatalogRepository>(sp => sp.GetRequiredService<ApplicationRepository>());
builder.Services.AddScoped<IOrderRepository>(sp => sp.GetRequiredService<ApplicationRepository>());
builder.Services.AddScoped<ICouponRepository>(sp => sp.GetRequiredService<ApplicationRepository>());
builder.Services.AddScoped<IContactRequestRepository>(sp => sp.GetRequiredService<ApplicationRepository>());

builder.Services.AddScoped<IShopCatalogService, ShopCatalogService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IContactRequestService, ContactRequestService>();
builder.Services.AddScoped<IProductPopularityService, ProductPopularityService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<ICurrentSiteSettings, CurrentSiteSettings>();
builder.Services.AddScoped<IHomePageContentService, HomePageContentService>();
builder.Services.AddScoped<ICurrentHomePageContent, CurrentHomePageContent>();
builder.Services.AddScoped<IAboutPageContentService, AboutPageContentService>();
builder.Services.AddScoped<ICurrentAboutPageContent, CurrentAboutPageContent>();
builder.Services.AddScoped<IContactPageContentService, ContactPageContentService>();
builder.Services.AddScoped<ICurrentContactPageContent, CurrentContactPageContent>();
builder.Services.AddScoped<ILegalPageContentService, LegalPageContentService>();
builder.Services.AddScoped<ICurrentLegalPageContent, CurrentLegalPageContent>();
builder.Services.AddScoped<IShopMarketingContentService, ShopMarketingContentService>();
builder.Services.AddScoped<ICurrentShopMarketingContent, CurrentShopMarketingContent>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IDealerService, DealerService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IPartnerStripService, PartnerStripService>();
builder.Services.AddScoped<ICurrentPartnerStrip, CurrentPartnerStrip>();
builder.Services.AddScoped<IFaqPageContentService, FaqPageContentService>();
builder.Services.AddScoped<ICurrentFaqPageContent, CurrentFaqPageContent>();
builder.Services.AddScoped<ITeamPageContentService, TeamPageContentService>();
builder.Services.AddScoped<ICurrentTeamPageContent, CurrentTeamPageContent>();
builder.Services.AddScoped<IDealerPageContentService, DealerPageContentService>();
builder.Services.AddScoped<ICurrentDealerPageContent, CurrentDealerPageContent>();
builder.Services.AddScoped<ICalculatorPageContentService, CalculatorPageContentService>();
builder.Services.AddScoped<ICurrentCalculatorPageContent, CurrentCalculatorPageContent>();
builder.Services.AddScoped<AccountActivityHistory>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

EmailDeliveryStartupDiagnostics.Log(
    app.Services,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AutoVault.Email"));

if (app.Environment.IsDevelopment())
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentApiHost));
    var apiSigningKey = ApiSigningKey.RequireConfigured(app.Configuration);
    await DevelopmentApiHost.EnsureStartedAsync(
        app.Environment.ContentRootPath,
        apiBaseUrl,
        app.Lifetime,
        startupLogger,
        apiSigningKey);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<UnconfirmedAccountMiddleware>();
app.UseAuthorization();

app.UseSession(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ConversationHub>("/hubs/conversations");

await SeedApplicationAsync(app);

app.Run();

static async Task SeedApplicationAsync(WebApplication app)
{
    await using (var catalogScope = app.Services.CreateAsyncScope())
    {
        var services = catalogScope.ServiceProvider;
        await services.GetRequiredService<IShopCatalogService>().EnsureSeedAsync();
        await services.GetRequiredService<ICouponService>().EnsureSeedAsync();
        await services.GetRequiredService<ISiteSettingsService>().EnsureSeedAsync();
        await services.GetRequiredService<IHomePageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<IAboutPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<IContactPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<ILegalPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<IShopMarketingContentService>().EnsureSeedAsync();
        await services.GetRequiredService<ITeamMemberService>().EnsureSeedAsync();
        await services.GetRequiredService<IDealerService>().EnsureSeedAsync();
        await services.GetRequiredService<IFaqService>().EnsureSeedAsync();
        await services.GetRequiredService<IPartnerStripService>().EnsureSeedAsync();
        await services.GetRequiredService<IFaqPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<ITeamPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<IDealerPageContentService>().EnsureSeedAsync();
        await services.GetRequiredService<ICalculatorPageContentService>().EnsureSeedAsync();
    }

    var seed = app.Configuration.GetSection("AdminSeed");
    var accounts = new (string Email, string Password, string Role, bool ResetPassword)[]
    {
        (
            seed["SuperAdminEmail"] ?? string.Empty,
            seed["SuperAdminPassword"] ?? string.Empty,
            AppRoles.SuperAdmin,
            seed.GetValue<bool?>("ResetSuperAdminPassword") ?? false),
        (
            seed["AdminEmail"] ?? string.Empty,
            seed["AdminPassword"] ?? string.Empty,
            AppRoles.Admin,
            seed.GetValue<bool?>("ResetAdminPassword") ?? false),
        (
            seed["MemberEmail"] ?? string.Empty,
            seed["MemberPassword"] ?? string.Empty,
            AppRoles.Member,
            seed.GetValue<bool?>("ResetMemberPassword") ?? false)
    };

    await using var identityScope = app.Services.CreateAsyncScope();
    await IdentityRoleSeeder.SeedAsync(identityScope.ServiceProvider, accounts);
    await ExactDevAccountRemover.RemoveAsync(identityScope.ServiceProvider);
    await PopularityDemoSeeder.SeedAsync(identityScope.ServiceProvider);
}
