using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers;

public class TeamController : Controller
{
    private readonly ITeamMemberService _teams;
    private readonly ITeamPageContentService _teamPage;
    private readonly IDealerService _dealers;

    public TeamController(
        ITeamMemberService teams,
        ITeamPageContentService teamPage,
        IDealerService dealers)
    {
        _teams = teams;
        _teamPage = teamPage;
        _dealers = dealers;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var page = _teamPage.GetPublic();
        ViewData["Title"] = page.SeoTitle;
        ViewData["MetaDescription"] = page.SeoDescription;
        ViewData["TeamPage"] = page;

        var members = await _teams.GetPublishedAsync(cancellationToken);
        var dealers = await _dealers.GetPublishedAsync(cancellationToken);
        ViewData["PublishedTeamCount"] = members.Count;
        ViewData["PublishedDealerCount"] = dealers.Count;
        return View(members);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var member = await _teams.GetByIdAsync(id, cancellationToken);
        if (member == null || !member.IsPublished)
        {
            return NotFound();
        }

        ViewData["Title"] = $"{member.Name} - Team - AutoVault";
        ViewData["MetaDescription"] = string.IsNullOrWhiteSpace(member.Biography)
            ? $"{member.Name}, {member.Position} at AutoVault."
            : member.Biography.Trim();
        if (ViewData["MetaDescription"] is string meta && meta.Length > 320)
        {
            ViewData["MetaDescription"] = meta[..317] + "...";
        }

        return View(member);
    }
}
