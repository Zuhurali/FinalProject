using FinalProject.MVC.Configuration;
using FinalProject.MVC.Data;
using FinalProject.MVC.Hosting;
using FinalProject.MVC.Hubs;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/SignIn";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
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
builder.Services.AddSingleton<IApiAccessTokenService, ApiAccessTokenService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPanel", policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy("CatalogCreate", policy => policy.RequireRole(AppRoles.SuperAdmin));
    options.AddPolicy("ManageUsers", policy => policy.RequireRole(AppRoles.SuperAdmin));
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<DealershipContactSettings>(
    builder.Configuration.GetSection(DealershipContactSettings.SectionName));
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IShopCatalogService, ShopCatalogService>();
builder.Services.AddScoped<IContactRequestService, ContactRequestService>();
builder.Services.AddScoped<IProductPopularityService, ProductPopularityService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<AccountActivityHistory>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentApiHost));
    await DevelopmentApiHost.EnsureStartedAsync(app.Environment.ContentRootPath, apiBaseUrl, app.Lifetime, startupLogger);
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
        var catalog = catalogScope.ServiceProvider.GetRequiredService<IShopCatalogService>();
        await catalog.EnsureSeedAsync();
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
