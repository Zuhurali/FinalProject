using FinalProject.MVC.Data;
using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class TeamController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Team - AutoVault";
            return View(TeamCatalog.Members);
        }

        public IActionResult Details(int id)
        {
            var member = TeamCatalog.Find(id);
            if (member == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"{member.Name} - Team - AutoVault";
            return View(member);
        }
    }
}
