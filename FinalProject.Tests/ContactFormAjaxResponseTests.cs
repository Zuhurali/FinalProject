using System.Text.Json;
using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class ContactFormAjaxResponseTests
{
    [Fact]
    public void DisplayMessage_uses_human_message_not_raw_json_body()
    {
        var body = JsonSerializer.Serialize(new
        {
            success = false,
            message = "Please correct the highlighted fields.",
            errors = new Dictionary<string, string>
            {
                ["phone"] = "Enter a valid phone number."
            }
        });

        var displayed = ContactFormAjaxResponse.DisplayMessageOrGeneric(body);

        Assert.Equal("Please correct the highlighted fields.", displayed);
        Assert.DoesNotContain("\"success\"", displayed, StringComparison.Ordinal);
        Assert.DoesNotContain("\"errors\"", displayed, StringComparison.Ordinal);
        Assert.NotEqual(body, displayed);
    }

    [Fact]
    public void TryParse_exposes_field_errors_for_invalid_phone()
    {
        var body = """{"success":false,"message":"Please correct the highlighted fields.","errors":{"phone":"Enter a valid phone number."}}""";

        var parsed = ContactFormAjaxResponse.TryParse(body, out var success, out var message, out var errors);

        Assert.True(parsed);
        Assert.False(success);
        Assert.Equal("Please correct the highlighted fields.", message);
        Assert.Equal("Enter a valid phone number.", errors["phone"]);
    }

    [Fact]
    public void TryParse_success_payload_returns_thank_you_message()
    {
        var body = """{"success":true,"message":"Thank you. Your message was sent. Our team will get back to you shortly."}""";

        var parsed = ContactFormAjaxResponse.TryParse(body, out var success, out var message, out var errors);

        Assert.True(parsed);
        Assert.True(success);
        Assert.StartsWith("Thank you.", message, StringComparison.Ordinal);
        Assert.Empty(errors);
    }

    [Fact]
    public void DisplayMessage_never_returns_html_or_unparsed_json_blob()
    {
        Assert.Equal(
            ContactFormAjaxResponse.GenericError,
            ContactFormAjaxResponse.DisplayMessageOrGeneric("<!DOCTYPE html><html><body>error</body></html>"));

        Assert.Equal(
            ContactFormAjaxResponse.GenericError,
            ContactFormAjaxResponse.DisplayMessageOrGeneric("not-json-at-all"));
    }

    [Fact]
    public void Contact_form_scripts_do_not_assign_responseText_to_the_message_box()
    {
        var assets = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "FinalProject.MVC", "wwwroot", "assets", "js", "plugins", "contact-form.js"));
        var legacy = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "FinalProject.MVC", "wwwroot", "js", "plugins", "contact-form.js"));

        Assert.DoesNotContain(".text(xhr.responseText)", assets, StringComparison.Ordinal);
        Assert.DoesNotContain(".text(data.responseText)", assets, StringComparison.Ordinal);
        Assert.DoesNotContain(".text(response)", assets, StringComparison.Ordinal);
        Assert.Contains("dataType: 'text'", assets, StringComparison.Ordinal);
        Assert.Contains("looksLikeJson", assets, StringComparison.Ordinal);

        Assert.Equal(assets, legacy);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "FinalProject.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for contact-form.js checks.");
    }
}
