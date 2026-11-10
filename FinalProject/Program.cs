using FinalProject.Data;
using FinalProject.Hosting;
using FinalProject.Repositories;
using FinalProject.Repositories.Implementations;
using FinalProject.Security;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

DevelopmentHostAnnouncement.Announce();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FinalProjectDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.MigrationsHistoryTable(FinalProjectDbContext.MigrationsHistoryTable);
            // FinalProjectDbContext owns the car, favorite, review, and messaging tables.
            // Shared shop and Identity tables are excluded from these migrations.
            sql.MigrationsAssembly(typeof(DevelopmentHostAnnouncement).Assembly.GetName().Name);
        }));

builder.Services.AddScoped<FinalProjectRepository>();
builder.Services.AddScoped<ICarRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<IProductReviewRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<IProductRatingRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<IMarketplaceRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<INotificationRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<IPartsRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<IShowroomRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());
builder.Services.AddScoped<ICatalogAdminRepository>(sp => sp.GetRequiredService<FinalProjectRepository>());

builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<ICarDiscoveryService, CarDiscoveryService>();
builder.Services.AddScoped<IProductRatingService, ProductRatingService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
builder.Services.AddScoped<IPartsAdminService, PartsAdminService>();
builder.Services.AddScoped<IShowroomAdminService, ShowroomAdminService>();
builder.Services.AddAuthorization();
var jwtKey = ApiSigningKey.RequireConfigured(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["ApiAuthentication:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["ApiAuthentication:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many view requests. Try again later." },
            cancellationToken);
    };
    options.AddPolicy(CarViewsRateLimit.PolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = CarViewsRateLimit.PermitLimitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.DescribeAllParametersInCamelCase();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "MVC-issued JWT for an authenticated application user. Issuer: FinalProject.MVC. Audience: FinalProject.API. Lifetime: 5 minutes."
    });
    options.OperationFilter<SwaggerAuthorizeOperationFilter>();
});

var app = builder.Build();

// Anonymous readiness probe used by the MVC development host. It does not touch the database.
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            var host = context.Request.Host.Host;
            var localHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
            var remote = context.Connection.RemoteIpAddress;
            var localRemote = remote is null || System.Net.IPAddress.IsLoopback(remote);
            if (!localHost || !localRemote)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }

        await next();
    });
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Match FinalProject.MVC: production-only exception handling and HSTS.
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinalProjectDbContext>();
    await VerifiedCatalogSeeder.SeedAsync(db);
}

app.Run();
