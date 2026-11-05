using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FinalProject.MVC.Security;

/// <summary>
/// URL-safe encoding for ASP.NET Core Identity tokens placed in email links.
/// </summary>
public static class IdentityTokenCodec
{
    public static string Encode(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static bool TryDecode(string? encoded, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return !string.IsNullOrEmpty(token);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
