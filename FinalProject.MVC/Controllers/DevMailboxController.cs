using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

/// <summary>
/// Reads messages delivered by <see cref="Services.DevelopmentMailboxEmailSender"/>.
/// Returns 404 unless the host is Development and the caller is a signed-in Admin or SuperAdmin.
/// </summary>
public sealed class DevMailboxController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IDevelopmentMailStore? _mailStore;

    public DevMailboxController(
        IWebHostEnvironment environment,
        IEnumerable<IDevelopmentMailStore> mailStores)
    {
        _environment = environment;
        _mailStore = mailStores.FirstOrDefault();
    }

    [HttpGet]
    public IActionResult Index(string? email = null)
    {
        if (!TryGetStore(out var store))
        {
            return NotFound();
        }

        ViewData["Title"] = "Development Mailbox - AutoVault";
        ViewData["FilterEmail"] = email;
        return View(store.List(email));
    }

    [HttpGet]
    public IActionResult Message(Guid id)
    {
        if (!TryGetStore(out var store))
        {
            return NotFound();
        }

        var message = store.Find(id);
        if (message == null)
        {
            return NotFound();
        }

        ViewData["Title"] = message.Subject + " - Development Mailbox";
        ViewData["DisplayBody"] = DevelopmentMailDisplay.RedactSensitiveLinks(message.TextBody);
        ViewData["SecureLinkCount"] = DevelopmentMailDisplay.SensitiveLinks(message.TextBody).Count;
        return View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult OpenLink(Guid id, int index)
    {
        if (!TryGetStore(out var store))
        {
            return NotFound();
        }

        var message = store.Find(id);
        if (message == null)
        {
            return NotFound();
        }

        var links = DevelopmentMailDisplay.SensitiveLinks(message.TextBody);
        if (index < 0 || index >= links.Count || !DevelopmentMailDisplay.CanFollow(links[index]))
        {
            return NotFound();
        }

        return Redirect(links[index]);
    }

    private bool TryGetStore(out IDevelopmentMailStore store)
    {
        store = _mailStore!;
        return _environment.IsDevelopment()
            && _mailStore != null
            && (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"));
    }
}
