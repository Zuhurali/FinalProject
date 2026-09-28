using Microsoft.AspNetCore.Mvc;

namespace FinalProject.MVC.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Blog - AutoVault";
            return View();
        }

        public IActionResult BlogTwo()
        {
            ViewData["Title"] = "Blog Two - AutoVault";
            return View();
        }

        public IActionResult Details(int? id)
        {
            ViewData["Title"] = "Blog Details - AutoVault";
            ViewData["PostId"] = id ?? 1;
            return View();
        }
    }
}