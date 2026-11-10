using FinalProject.MVC.Repositories.Implementations;
using System.Diagnostics;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace FinalProject.Tests;

public class AzerbaijanPhoneNumberTests
{
    [Theory]
    [InlineData("0503546767", "+994503546767")]
    [InlineData("0501234567", "+994501234567")]
    [InlineData("+994503546767", "+994503546767")]
    [InlineData("+994501234567", "+994501234567")]
    public void Valid_azerbaijani_numbers_normalize_to_plus_994(string input, string expected)
    {
        Assert.True(AzerbaijanPhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
        Assert.Matches(@"^\+994[0-9]{9}$", normalized);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("05012abc567")]
    [InlineData("050123456")]
    [InlineData("05012345678")]
    [InlineData("05035467678")]
    [InlineData("+99450123")]
    [InlineData("+9945035467673")]
    [InlineData("+994503546767abc")]
    [InlineData("+9945035467673re344343434")]
    [InlineData("+994 503546767")]
    [InlineData("501234567")]
    [InlineData("++994501234567")]
    public void Invalid_phone_numbers_are_rejected_without_truncation(string input)
    {
        Assert.False(AzerbaijanPhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public async Task Team_create_and_edit_store_a_normalized_number_and_reject_invalid_numbers()
    {
        await using var db = CreateDb();
        var service = new TeamMemberService(new ApplicationRepository(db), new TestEnvironment());
        var created = Member();
        created.Phone = "0501234567";
        created.ImagePath = "/uploads/team/ada.webp";

        var (ok, error, id) = await service.CreateAsync(created, null);
        Assert.True(ok, error);
        Assert.Equal("+994501234567", (await db.TeamMembers.SingleAsync()).Phone);

        var invalid = Member();
        invalid.Phone = "05012345";
        invalid.ImagePath = "/uploads/team/ada.webp";
        var (failed, failedError, failedId) = await service.CreateAsync(invalid, null);
        Assert.False(failed);
        Assert.Equal(AzerbaijanPhoneNumber.ErrorMessage, failedError);
        Assert.Null(failedId);
        Assert.Equal(1, await db.TeamMembers.CountAsync());

        var edit = Member();
        edit.Id = id!.Value;
        edit.Phone = "letters-0501234567";
        edit.ImagePath = "/uploads/team/ada.webp";
        var (updated, updateError) = await service.UpdateAsync(edit, null);
        Assert.False(updated);
        Assert.Equal(AzerbaijanPhoneNumber.ErrorMessage, updateError);
        Assert.Equal("+994501234567", (await db.TeamMembers.SingleAsync()).Phone);
    }

    [Fact]
    public async Task Edit_keeps_the_stored_phone_until_a_valid_number_is_saved()
    {
        await using var db = CreateDb();
        var service = new TeamMemberService(new ApplicationRepository(db), new TestEnvironment());
        var created = Member();
        created.Phone = "+994503546767";
        created.ImagePath = "/uploads/team/ada.webp";
        var (ok, error, id) = await service.CreateAsync(created, null);
        Assert.True(ok, error);

        var loaded = await service.GetByIdAsync(id!.Value);
        Assert.Equal("+994503546767", loaded!.Phone);

        var edit = Member();
        edit.Id = id.Value;
        edit.Phone = "0503546767";
        edit.ImagePath = "/uploads/team/ada.webp";
        var (updated, updateError) = await service.UpdateAsync(edit, null);
        Assert.True(updated, updateError);
        Assert.Equal("+994503546767", (await db.TeamMembers.SingleAsync()).Phone);

        edit.Phone = "05035467678";
        var (rejected, rejectedError) = await service.UpdateAsync(edit, null);
        Assert.False(rejected);
        Assert.Equal(AzerbaijanPhoneNumber.ErrorMessage, rejectedError);
        Assert.Equal("+994503546767", (await db.TeamMembers.SingleAsync()).Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("050354676")]
    [InlineData("+99450354676")]
    [InlineData("05035467678")]
    [InlineData("+9945035467673")]
    [InlineData("+994503546767abc")]
    public async Task Save_rejects_incomplete_or_invalid_numbers_and_keeps_the_stored_phone(string phone)
    {
        await using var db = CreateDb();
        var service = new TeamMemberService(new ApplicationRepository(db), new TestEnvironment());
        var created = Member();
        created.Phone = "+994503546767";
        created.ImagePath = "/uploads/team/ada.webp";
        var (ok, error, id) = await service.CreateAsync(created, null);
        Assert.True(ok, error);

        var edit = Member();
        edit.Id = id!.Value;
        edit.Phone = phone;
        edit.ImagePath = "/uploads/team/ada.webp";
        var (updated, updateError) = await service.UpdateAsync(edit, null);
        Assert.False(updated);
        Assert.Equal(AzerbaijanPhoneNumber.ErrorMessage, updateError);
        Assert.Equal("+994503546767", (await db.TeamMembers.SingleAsync()).Phone);
    }

    [Fact]
    public void Team_phone_field_does_not_block_digit_keystrokes()
    {
        var form = Read("FinalProject.MVC", "Views", "AdminTeams", "_TeamForm.cshtml");
        var script = Read("FinalProject.MVC", "wwwroot", "js", "team-member-phone.js");
        Assert.DoesNotContain("beforeinput", form, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("beforeinput", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keydown", form, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("keydown", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("preventDefault", form, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("preventDefault", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maxlength", form, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-team-phone=\"true\"", form, StringComparison.Ordinal);
        Assert.Contains("team-member-phone.js", form, StringComparison.Ordinal);
        Assert.Contains("asp-validation-for=\"Phone\"", form, StringComparison.Ordinal);
        Assert.Contains("pattern=\"^(0[0-9]{9}|\\+994[0-9]{9})$\"", form, StringComparison.Ordinal);
        Assert.Contains("inputmode=\"tel\"", form, StringComparison.Ordinal);
        Assert.Contains("addEventListener(\"input\"", script, StringComparison.Ordinal);
        Assert.Contains("name=\"_TeamForm\"", Read("FinalProject.MVC", "Views", "AdminTeams", "Create.cshtml"), StringComparison.Ordinal);
        Assert.Contains("name=\"_TeamForm\"", Read("FinalProject.MVC", "Views", "AdminTeams", "Edit.cshtml"), StringComparison.Ordinal);
        Assert.Contains("Phone = member.Phone", Read("FinalProject.MVC", "Controllers", "AdminTeamsController.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Team_phone_input_allows_clearing_replacement_and_blocks_a_thirteenth_digit()
    {
        var root = FindRoot();
        var script = Path.Combine(root, "FinalProject.MVC", "wwwroot", "js", "team-member-phone.js");
        var probe = Path.Combine(root, "FinalProject.Tests", "team-member-phone-input.test.js");
        var start = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"\"{probe}\" \"{script}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(start);
        Assert.NotNull(process);
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error + output);
        Assert.Contains("ok", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Team_details_keep_phone_and_email_dark_on_hover()
    {
        var view = Read("FinalProject.MVC", "Views", "Team", "Details.cshtml");
        Assert.Contains("color: #111111;", view, StringComparison.Ordinal);
        Assert.Contains("a:hover", view, StringComparison.Ordinal);
        Assert.Contains("a:focus", view, StringComparison.Ordinal);
        Assert.Contains("a:active", view, StringComparison.Ordinal);
    }

    private static TeamMemberEditViewModel Member() => new()
    {
        Name = "Ada Tester",
        Position = "Advisor",
        Department = "Sales",
        Experience = "5 years",
        Biography = "Helps buyers compare cars.",
        Phone = "+994501112233",
        Email = "ada@example.com",
        SortOrder = 10,
        IsPublished = true,
        ImagePath = "/uploads/team/ada.webp"
    };

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("az-phone-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the solution root.");
    }

    private static string Read(params string[] parts)
    {
        return File.ReadAllText(Path.Combine(new[] { FindRoot() }.Concat(parts).ToArray()));
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
