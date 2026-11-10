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

/// <summary>
/// Live-MVC Admin CMS CRUD against disposable records only. Skips when MVC is not reachable.
/// Uses the shared campaign E2E admin account; never touches SuperAdmin credentials.
/// </summary>
[Collection("CampaignSql")]
public class AdminCmsCrudHttpTests
{
    private const string BaseUrl = "https://localhost:7255";
    private const string Email = "campaigne2e@autovault.local";
    private const string Password = "CampaignE2E-2026!";
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Account_Login_get_redirects_to_SignIn()
    {
        if (!await MvcIsReachableAsync())
        {
            return;
        }

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync("/Account/Login?returnUrl=%2FAdmin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/SignIn", response.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_home_faq_and_partners_crud_persist_to_sql_and_public_pages()
    {
        if (!await MvcIsReachableAsync())
        {
            return;
        }

        await EnsureAdminAsync();

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(60) };

        await SignInAsync(client);

        await using var db = CreateDb();
        var originalHero = await db.HomePageContents.AsNoTracking()
            .Where(row => row.Id == HomePageContent.SingletonId)
            .Select(row => row.HeroTitle)
            .SingleAsync();

        var marker = $"E2E Home {Guid.NewGuid():N}"[..28];
        try
        {
            // --- Home page via Admin UI ---
            var homeGet = await client.GetAsync("/Admin/Home");
            var homeHtml = await homeGet.Content.ReadAsStringAsync();
            Assert.True(homeGet.IsSuccessStatusCode, $"Admin/Home GET {(int)homeGet.StatusCode}");
            var homeFields = FormFields(homeHtml);
            Assert.True(homeFields.ContainsKey("__RequestVerificationToken"));
            homeFields["HeroTitle"] = marker;

            var homePost = await client.PostAsync("/Admin/Home", new FormUrlEncodedContent(homeFields));
            var homePostHtml = await homePost.Content.ReadAsStringAsync();
            Assert.True(homePost.IsSuccessStatusCode, $"Admin/Home POST {(int)homePost.StatusCode}");
            Assert.DoesNotContain("could not be saved", homePostHtml, StringComparison.OrdinalIgnoreCase);

            var sqlHero = await db.HomePageContents.AsNoTracking()
                .Where(row => row.Id == HomePageContent.SingletonId)
                .Select(row => row.HeroTitle)
                .SingleAsync();
            Assert.Equal(marker, sqlHero);

            var publicHome = await (await client.GetAsync("/")).Content.ReadAsStringAsync();
            Assert.Contains(marker, publicHome, StringComparison.Ordinal);

            // restore via Admin UI
            homeFields = FormFields(await (await client.GetAsync("/Admin/Home")).Content.ReadAsStringAsync());
            homeFields["HeroTitle"] = originalHero;
            var restore = await client.PostAsync("/Admin/Home", new FormUrlEncodedContent(homeFields));
            Assert.True(restore.IsSuccessStatusCode);
            var restoredHero = await db.HomePageContents.AsNoTracking()
                .Where(row => row.Id == HomePageContent.SingletonId)
                .Select(row => row.HeroTitle)
                .SingleAsync();
            Assert.Equal(originalHero, restoredHero);

            // --- Disposable FAQ ---
            var faqQuestion = $"E2E FAQ question {Guid.NewGuid():N}";
            var faqAnswer = $"E2E FAQ answer body {Guid.NewGuid():N} for verification.";
            var faqCreatePage = await (await client.GetAsync("/Admin/Faqs/Create")).Content.ReadAsStringAsync();
            var faqToken = Antiforgery(faqCreatePage);
            Assert.False(string.IsNullOrWhiteSpace(faqToken));

            var faqCreate = await client.PostAsync("/Admin/Faqs/Create", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = faqToken!,
                ["Question"] = faqQuestion,
                ["Answer"] = faqAnswer,
                ["SortOrder"] = "99990",
                ["IsPublished"] = "true",
                ["ShowOnShop"] = "false"
            }));
            Assert.True(faqCreate.IsSuccessStatusCode, $"FAQ create {(int)faqCreate.StatusCode}");

            var faqRow = await db.FaqItems.AsNoTracking().SingleAsync(item => item.Question == faqQuestion);
            Assert.Equal(faqAnswer, faqRow.Answer);
            Assert.True(faqRow.IsPublished);

            var faqPublic = await (await client.GetAsync("/Faq")).Content.ReadAsStringAsync();
            Assert.Contains(faqQuestion, faqPublic, StringComparison.Ordinal);

            var faqIndex = await (await client.GetAsync("/Admin/Faqs")).Content.ReadAsStringAsync();
            var deleteToken = Antiforgery(faqIndex);
            Assert.False(string.IsNullOrWhiteSpace(deleteToken));
            var deleteFaq = await client.PostAsync($"/Admin/Faqs/Delete/{faqRow.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = deleteToken!
            }));
            Assert.True(deleteFaq.IsSuccessStatusCode);
            Assert.False(await db.FaqItems.AnyAsync(item => item.Id == faqRow.Id));

            // --- Disposable partner ---
            var partnerName = $"E2E Partner {Guid.NewGuid():N}"[..24];
            var logoPath = await db.PartnerStripItems.AsNoTracking()
                .Select(item => item.LogoPath)
                .FirstAsync();
            var partnerCreatePage = await (await client.GetAsync("/Admin/Partners/Create")).Content.ReadAsStringAsync();
            var partnerToken = Antiforgery(partnerCreatePage);
            Assert.False(string.IsNullOrWhiteSpace(partnerToken));

            var partnerCreate = await client.PostAsync("/Admin/Partners/Create", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = partnerToken!,
                ["Name"] = partnerName,
                ["LogoPath"] = logoPath,
                ["LinkUrl"] = "",
                ["SortOrder"] = "99990",
                ["IsPublished"] = "true"
            }));
            Assert.True(partnerCreate.IsSuccessStatusCode, $"Partner create {(int)partnerCreate.StatusCode}");

            var partnerRow = await db.PartnerStripItems.AsNoTracking().SingleAsync(item => item.Name == partnerName);
            Assert.True(partnerRow.IsPublished);

            var partnersPublic = await (await client.GetAsync("/")).Content.ReadAsStringAsync();
            Assert.Contains(partnerName, partnersPublic, StringComparison.Ordinal);

            var partnerIndex = await (await client.GetAsync("/Admin/Partners")).Content.ReadAsStringAsync();
            var partnerDeleteToken = Antiforgery(partnerIndex);
            Assert.False(string.IsNullOrWhiteSpace(partnerDeleteToken));
            var deletePartner = await client.PostAsync($"/Admin/Partners/Delete/{partnerRow.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = partnerDeleteToken!
            }));
            Assert.True(deletePartner.IsSuccessStatusCode);
            Assert.False(await db.PartnerStripItems.AnyAsync(item => item.Id == partnerRow.Id));
        }
        finally
        {
            // Safety net: never leave hero marker behind if a mid-flow assert failed.
            var current = await db.HomePageContents.SingleAsync(row => row.Id == HomePageContent.SingletonId);
            if (!string.Equals(current.HeroTitle, originalHero, StringComparison.Ordinal))
            {
                current.HeroTitle = originalHero;
                current.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await db.FaqItems.Where(item => item.Question.StartsWith("E2E FAQ question "))
                .ExecuteDeleteAsync();
            await db.PartnerStripItems.Where(item => item.Name.StartsWith("E2E Partner "))
                .ExecuteDeleteAsync();
        }
    }

    private static async Task SignInAsync(HttpClient client)
    {
        var signInPage = await client.GetAsync("/Account/SignIn?ReturnUrl=%2FAdmin");
        var signInHtml = await signInPage.Content.ReadAsStringAsync();
        var loginToken = Antiforgery(signInHtml);
        Assert.False(string.IsNullOrWhiteSpace(loginToken));

        var login = await client.PostAsync("/Account/SignIn", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["identifier"] = Email,
            ["password"] = Password,
            ["rememberMe"] = "false",
            ["returnUrl"] = "/Admin",
            ["__RequestVerificationToken"] = loginToken!
        }));
        Assert.True(login.IsSuccessStatusCode || (int)login.StatusCode is >= 300 and < 400);

        var dashboard = await client.GetAsync("/Admin");
        Assert.True(dashboard.IsSuccessStatusCode, $"Admin after sign-in {(int)dashboard.StatusCode}");
        var html = await dashboard.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard", html, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> FormFields(string html)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(
                     html,
                     "<input\\b([^>]*)>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var attrs = match.Groups[1].Value;
            var type = Attr(attrs, "type") ?? "text";
            if (type.Equals("file", StringComparison.OrdinalIgnoreCase)
                || type.Equals("submit", StringComparison.OrdinalIgnoreCase)
                || type.Equals("button", StringComparison.OrdinalIgnoreCase)
                || type.Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = Attr(attrs, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (type.Equals("checkbox", StringComparison.OrdinalIgnoreCase)
                || type.Equals("radio", StringComparison.OrdinalIgnoreCase))
            {
                if (attrs.Contains("checked", StringComparison.OrdinalIgnoreCase))
                {
                    fields[name] = Attr(attrs, "value") ?? "true";
                }
                else if (!fields.ContainsKey(name))
                {
                    // ASP.NET checkbox helpers emit a hidden false before the checkbox.
                    continue;
                }

                continue;
            }

            fields[name] = Attr(attrs, "value") ?? string.Empty;
        }

        foreach (Match match in Regex.Matches(
                     html,
                     "<textarea\\b([^>]*)>(.*?)</textarea>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var name = Attr(match.Groups[1].Value, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                fields[name] = WebUtility.HtmlDecode(match.Groups[2].Value.Trim());
            }
        }

        foreach (Match match in Regex.Matches(
                     html,
                     "<select\\b([^>]*)>(.*?)</select>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var name = Attr(match.Groups[1].Value, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var selected = Regex.Match(
                match.Groups[2].Value,
                "<option\\b([^>]*selected[^>]*)>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!selected.Success)
            {
                selected = Regex.Match(
                    match.Groups[2].Value,
                    "<option\\b([^>]*)>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            fields[name] = selected.Success ? (Attr(selected.Groups[1].Value, "value") ?? string.Empty) : string.Empty;
        }

        return fields;
    }

    private static string? Attr(string attrs, string name)
    {
        var match = Regex.Match(
            attrs,
            $"{Regex.Escape(name)}\\s*=\\s*\"([^\"]*)\"",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return WebUtility.HtmlDecode(match.Groups[1].Value);
        }

        match = Regex.Match(
            attrs,
            $"{Regex.Escape(name)}\\s*=\\s*'([^']*)'",
            RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
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
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect;
        }
        catch
        {
            return false;
        }
    }

    private static string? Antiforgery(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(
            html,
            "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    private static async Task EnsureAdminAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(ConnectionString));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        var user = await users.FindByEmailAsync(Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var created = await users.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        }
        else
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var reset = await users.ResetPasswordAsync(user, token, Password);
            Assert.True(reset.Succeeded, string.Join(", ", reset.Errors.Select(error => error.Description)));
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await users.UpdateAsync(user);
            }
        }

        var current = await users.GetRolesAsync(user);
        foreach (var role in current.Where(role => role != AppRoles.Admin && role != AppRoles.SuperAdmin))
        {
            await users.RemoveFromRoleAsync(user, role);
        }

        if (!await users.IsInRoleAsync(user, AppRoles.Admin)
            && !await users.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            await users.AddToRoleAsync(user, AppRoles.Admin);
        }
    }
}
