using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IContactInquiryMailer
{
    Task<(bool Sent, string? Error)> SendAsync(ContactFormModel model, CancellationToken cancellationToken = default);
}
