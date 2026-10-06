using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IShopCatalogService, ShopCatalogService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Enable authentication
app.UseAuthorization();

app.UseSession(); // Enable session

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await SeedApplicationAsync(app);

app.Run();

static async Task SeedApplicationAsync(WebApplication app)
{
    await using (var catalogScope = app.Services.CreateAsyncScope())
    {
        var catalog = catalogScope.ServiceProvider.GetRequiredService<IShopCatalogService>();
        await catalog.EnsureSeedAsync();
    }

    await using var identityScope = app.Services.CreateAsyncScope();
    await IdentityRoleSeeder.SeedAsync(identityScope.ServiceProvider, app.Configuration);
}
