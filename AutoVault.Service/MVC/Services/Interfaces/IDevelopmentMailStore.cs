namespace FinalProject.MVC.Services.Interfaces;

/// <summary>
/// In-process inbox used by the Development mailbox email sender.
/// Registered only when the MVC host is in Development and SMTP is not configured.
/// </summary>
public interface IDevelopmentMailStore
{
    DevelopmentMailMessage Add(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody);

    IReadOnlyList<DevelopmentMailMessage> List(string? toEmail = null);

    DevelopmentMailMessage? Find(Guid id);
}

public sealed class DevelopmentMailMessage
{
    public required Guid Id { get; init; }

    public required string ToEmail { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    public required string TextBody { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }
}
