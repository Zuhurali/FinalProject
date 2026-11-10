using System.Text.Json;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Mirrors the contact-form.js contract: parse JSON bodies and expose only
/// human-readable messages / field errors — never the raw response text.
/// </summary>
public static class ContactFormAjaxResponse
{
    public const string GenericError = "Something went wrong. Please try again or use the contact page.";
    public const string ValidationSummary = "Please correct the highlighted fields.";
    public const string SuccessFallback = "Thank you. Your message was sent.";

    public static bool TryParse(
        string? responseBody,
        out bool success,
        out string message,
        out IReadOnlyDictionary<string, string> errors)
    {
        success = false;
        message = GenericError;
        errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(responseBody) || LooksLikeHtml(responseBody))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            success = root.TryGetProperty("success", out var successNode)
                      && successNode.ValueKind == JsonValueKind.True;

            var parsedErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("errors", out var errorsNode) && errorsNode.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsNode.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var text = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(text) && !LooksLikeJson(text))
                        {
                            parsedErrors[property.Name.ToLowerInvariant()] = text.Trim();
                        }
                    }
                }
            }

            errors = parsedErrors;

            var rawMessage = root.TryGetProperty("message", out var messageNode)
                             && messageNode.ValueKind == JsonValueKind.String
                ? messageNode.GetString()
                : null;

            message = SanitizeMessage(
                rawMessage,
                success
                    ? SuccessFallback
                    : parsedErrors.Count > 0 ? ValidationSummary : GenericError);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string DisplayMessageOrGeneric(string? responseBody)
    {
        if (TryParse(responseBody, out _, out var message, out _))
        {
            return message;
        }

        return GenericError;
    }

    private static string SanitizeMessage(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value) || LooksLikeHtml(value) || LooksLikeJson(value))
        {
            return fallback;
        }

        return value.Trim();
    }

    private static bool LooksLikeHtml(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }
}
