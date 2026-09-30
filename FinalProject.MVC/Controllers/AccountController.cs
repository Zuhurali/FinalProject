using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinalProject.MVC.Models;
using System.Threading.Tasks;

namespace FinalProject.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Account - AutoVault";

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var name = $"{user.FirstName} {user.LastName}".Trim();
                    ViewData["AccountName"] = string.IsNullOrWhiteSpace(name) ? user.Email : name;
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult SignIn(string? returnUrl = null)
        {
            ViewData["Title"] = "Sign In - AutoVault";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(string email, string password, bool rememberMe = true, string? returnUrl = null, bool fromAccount = false)
        {
            ViewData["Title"] = "Sign In - AutoVault";

            IActionResult InvalidLogin()
            {
                if (fromAccount)
                {
                    return RedirectToAction(nameof(Index));
                }

                return View();
            }
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return InvalidLogin();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(email);
            }

            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return InvalidLogin();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            
            TempData["Error"] = "Invalid email or password.";
            return InvalidLogin();
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Register - AutoVault";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string? firstName = null, string? lastName = null, bool fromAccount = false)
        {
            ViewData["Title"] = "Register - AutoVault";

            IActionResult InvalidRegistration()
            {
                if (fromAccount)
                {
                    return RedirectToAction(nameof(Index));
                }

                return View();
            }
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return InvalidRegistration();
            }

            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return InvalidRegistration();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                TempData["Success"] = "Account created successfully!";
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return InvalidRegistration();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("SignOut")]
        public async Task<IActionResult> SignOutUser()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AddCar()
        {
            ViewData["Title"] = "Add Car - AutoVault";
            return View();
        }

        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access Denied - AutoVault";
            return View();
        }
    }
}