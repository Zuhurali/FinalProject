using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Xunit;
using MvcApiSigningKey = FinalProject.MVC.Security.ApiSigningKey;
using ApiApiSigningKey = FinalProject.Security.ApiSigningKey;

namespace FinalProject.Tests;

public class JwtSigningKeyConfigurationTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Theory]
    [InlineData("FinalProject", "appsettings.json")]
    [InlineData("FinalProject.MVC", "appsettings.json")]
    [InlineData("FinalProject", "appsettings.Development.json")]
    [InlineData("FinalProject.MVC", "appsettings.Development.json")]
    public void Committed_appsettings_do_not_contain_SigningKey(string project, string fileName)
    {
        var path = Path.Combine(RepoRoot, project, fileName);
        Assert.True(File.Exists(path), $"Missing {path}");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("ApiAuthentication", out var auth))
        {
            return;
        }

        Assert.False(
            auth.TryGetProperty("SigningKey", out _),
            $"{project}/{fileName} must not commit ApiAuthentication:SigningKey. Use user secrets or {MvcApiSigningKey.EnvironmentVariable}.");
    }

    [Fact]
    public void Committed_configuration_does_not_contain_previously_exposed_development_key_literal()
    {
        // The previously committed key string must not remain in source appsettings.
        // Do not assert the literal in failure messages beyond confirming absence.
        var exposedFragment = "AutoVault-Rating-API-Development-Key";
        foreach (var relative in new[]
                 {
                     Path.Combine("FinalProject", "appsettings.json"),
                     Path.Combine("FinalProject.MVC", "appsettings.json"),
                     Path.Combine("FinalProject", "appsettings.Development.json"),
                     Path.Combine("FinalProject.MVC", "appsettings.Development.json")
                 })
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot, relative));
            Assert.DoesNotContain(exposedFragment, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RequireConfigured_rejects_missing_or_short_keys_without_echoing_secret_material()
    {
        var empty = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var shortKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [MvcApiSigningKey.ConfigurationKey] = "too-short"
            })
            .Build();

        var missing = Assert.Throws<InvalidOperationException>(() => MvcApiSigningKey.RequireConfigured(empty));
        var tooShort = Assert.Throws<InvalidOperationException>(() => ApiApiSigningKey.RequireConfigured(shortKey));

        Assert.Contains("user secrets", missing.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(MvcApiSigningKey.EnvironmentVariable, missing.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("too-short", tooShort.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireConfigured_accepts_environment_style_configuration()
    {
        var key = new string('k', MvcApiSigningKey.MinimumLength);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [MvcApiSigningKey.ConfigurationKey] = key
            })
            .Build();

        Assert.Equal(key, MvcApiSigningKey.RequireConfigured(configuration));
        Assert.Equal(key, ApiApiSigningKey.RequireConfigured(configuration));
    }
}
