using System.Collections.Concurrent;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class DevelopmentMailStore : IDevelopmentMailStore
{
    private const int Capacity = 200;
    private readonly ConcurrentQueue<DevelopmentMailMessage> _messages = new();

    public DevelopmentMailMessage Add(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody)
    {
        var message = new DevelopmentMailMessage
        {
            Id = Guid.NewGuid(),
            ToEmail = toEmail.Trim(),
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _messages.Enqueue(message);
        while (_messages.Count > Capacity && _messages.TryDequeue(out _))
        {
        }

        return message;
    }

    public IReadOnlyList<DevelopmentMailMessage> List(string? toEmail = null)
    {
        IEnumerable<DevelopmentMailMessage> query = _messages.Reverse();
        if (!string.IsNullOrWhiteSpace(toEmail))
        {
            var needle = toEmail.Trim();
            query = query.Where(message =>
                string.Equals(message.ToEmail, needle, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    public DevelopmentMailMessage? Find(Guid id) =>
        _messages.FirstOrDefault(message => message.Id == id);
}
