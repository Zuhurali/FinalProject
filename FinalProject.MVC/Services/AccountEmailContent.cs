using System.Net;

namespace FinalProject.MVC.Services;

/// <summary>
/// Branded message bodies sent through the existing <see cref="Interfaces.IEmailSender"/>.
/// </summary>
public static class AccountEmailContent
{
    public const string ConfirmationSubject = "Confirm your AutoVault account";

    public static (string Html, string Text) Confirmation(string confirmationUrl)
    {
        var safeUrl = WebUtility.HtmlEncode(confirmationUrl);
        var html = $"""
            <div style="margin:0;padding:24px;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:8px;overflow:hidden;">
                <tr>
                  <td style="background:#111111;padding:24px 32px;">
                    <p style="margin:0;font-size:22px;letter-spacing:0.08em;color:#ffffff;font-weight:700;">AUTOVAULT</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;">
                    <h1 style="margin:0 0 16px;font-size:22px;color:#111111;">Welcome to AutoVault.</h1>
                    <p style="margin:0 0 16px;line-height:1.5;">Someone registered an AutoVault account using this email address. Confirm the address to finish creating the account and sign in.</p>
                    <p style="margin:24px 0;">
                      <a href="{safeUrl}" style="display:inline-block;background:#FF3600;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:4px;font-weight:700;">Confirm Email</a>
                    </p>
                    <p style="margin:0 0 8px;line-height:1.5;">If the button does not open, copy this confirmation link into your browser:</p>
                    <p style="margin:0 0 16px;line-height:1.5;word-break:break-all;"><a href="{safeUrl}" style="color:#FF3600;">{safeUrl}</a></p>
                    <p style="margin:0;line-height:1.5;color:#555555;">If you did not register for AutoVault, you can ignore this email.</p>
                  </td>
                </tr>
              </table>
            </div>
            """;

        var text = $"""
            Welcome to AutoVault.

            Someone registered an AutoVault account using this email address. Confirm the address to finish creating the account and sign in.

            Confirm Email: {confirmationUrl}

            If you did not register for AutoVault, you can ignore this email.
            """;

        return (html, text);
    }
}
