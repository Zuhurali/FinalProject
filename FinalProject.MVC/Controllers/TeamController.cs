using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class TeamController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Team - AutoVault";
            return View();
        }
    }
}