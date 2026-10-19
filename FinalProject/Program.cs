using FinalProject.Data;
using FinalProject.Hosting;
using FinalProject.Services;
using FinalProject.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

DevelopmentHostAnnouncement.Announce();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FinalProjectDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsHistoryTable(FinalProjectDbContext.MigrationsHistoryTable)));

builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IProductRatingService, ProductRatingService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
builder.Services.AddScoped<IPartsAdminService, PartsAdminService>();
builder.Services.AddAuthorization();
var jwtKey = builder.Configuration["ApiAuthentication:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("ApiAuthentication:SigningKey must contain at least 32 characters.");
}

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
        Description = "MVC-issued access token for an authenticated application user."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinalProjectDbContext>();
    await VerifiedCatalogSeeder.SeedAsync(db);
}

app.Run();
