using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class CampaignFormHttpTests
{
    private const string BaseUrl = "https://localhost:7255";
    private const string Email = "campaigne2e@autovault.local";
    private const string Password = "CampaignE2E-2026!";

    [Fact]
    public async Task Create_form_posts_to_save_and_inserts_the_campaign()
    {
        if (!await MvcIsReachableAsync())
        {
            return;
        }

        await EnsureAdminAsync();

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var signInPage = await client.GetAsync("/Account/SignIn?ReturnUrl=%2FAdmin%2FCampaigns%2FCreate");
        var signInHtml = await signInPage.Content.ReadAsStringAsync();
        var loginToken = Antiforgery(signInHtml);
        Assert.False(string.IsNullOrWhiteSpace(loginToken));

        var login = await client.PostAsync("/Account/SignIn", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["identifier"] = Email,
            ["password"] = Password,
            ["rememberMe"] = "false",
            ["returnUrl"] = "/Admin/Campaigns/Create",
            ["__RequestVerificationToken"] = loginToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Admin/Campaigns/Create", login.Headers.Location?.OriginalString);

        var createPage = await client.GetAsync("/Admin/Campaigns/Create");
        Assert.Equal(HttpStatusCode.OK, createPage.StatusCode);
        var createHtml = await createPage.Content.ReadAsStringAsync();
        Assert.Contains("action=\"/Admin/Campaigns/Save\"", createHtml);
        Assert.Contains("type=\"submit\"", createHtml);
        Assert.Contains("Create Campaign", createHtml);
        Assert.DoesNotContain("preventDefault", createHtml);
        var saveToken = Antiforgery(createHtml);
        Assert.False(string.IsNullOrWhiteSpace(saveToken));

        var today = DateTime.UtcNow.Date;
        var save = await client.PostAsync("/Admin/Campaigns/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = saveToken,
            ["Id"] = "0",
            ["AppliesToAllParts"] = "false",
            ["ProductId"] = "12",
            ["DiscountPercent"] = "15",
            ["StartsAt"] = today.ToString("yyyy-MM-dd"),
            ["EndsAt"] = today.AddDays(10).ToString("yyyy-MM-dd"),
            ["Name"] = "E2E HTTP Timing Belt",
            ["isActive"] = "true",
            ["createCampaign"] = "1"
        }));
        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        Assert.Equal("/Admin/Campaigns", save.Headers.Location?.OriginalString);

        var list = await client.GetAsync("/Admin/Campaigns");
        var listHtml = await list.Content.ReadAsStringAsync();
        Assert.Contains("E2E HTTP Timing Belt", listHtml);
        Assert.Contains("Timing Belt", listHtml);

        var invalid = await client.PostAsync("/Admin/Campaigns/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = saveToken,
            ["Id"] = "0",
            ["AppliesToAllParts"] = "false",
            ["ProductId"] = "",
            ["DiscountPercent"] = "0",
            ["StartsAt"] = today.ToString("yyyy-MM-dd"),
            ["EndsAt"] = today.AddDays(-1).ToString("yyyy-MM-dd"),
            ["Name"] = "E2E HTTP invalid",
            ["isActive"] = "true"
        }));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("Select a spare part", invalidHtml);
        Assert.Contains("Enter a discount percentage", invalidHtml);

        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
        var inserted = await db.ProductCampaigns.SingleAsync(campaign => campaign.Name == "E2E HTTP Timing Belt");
        Assert.Equal(12, inserted.ProductId);
        Assert.Equal(15m, inserted.DiscountPercent);
        db.ProductCampaigns.Remove(inserted);
        await db.SaveChangesAsync();
    }

    private static async Task<bool> MvcIsReachableAsync()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync("/Account/SignIn");
            return response.IsSuccessStatusCode || (int)response.StatusCode is >= 300 and < 400;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private static string Antiforgery(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static async Task EnsureAdminAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True"));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AppRoles.Admin))
        {
            Assert.True((await roles.CreateAsync(new IdentityRole(AppRoles.Admin))).Succeeded);
        }

        var user = await users.FindByEmailAsync(Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = "campaigne2e",
                Email = Email,
                EmailConfirmed = true,
                FirstName = "Campaign",
                LastName = "E2E"
            };
            var created = await users.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        }
        else
        {
            user.EmailConfirmed = true;
            await users.UpdateAsync(user);
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var reset = await users.ResetPasswordAsync(user, token, Password);
            Assert.True(reset.Succeeded, string.Join("; ", reset.Errors.Select(error => error.Description)));
        }

        if (!await users.IsInRoleAsync(user, AppRoles.Admin))
        {
            Assert.True((await users.AddToRoleAsync(user, AppRoles.Admin)).Succeeded);
        }
    }
}
