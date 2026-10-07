using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using System.Threading.Tasks;

namespace FinalProject.MVC.Controllers
{
    [TypeFilter(typeof(RedirectAuthenticatedAuthPageOnAntiforgeryFailureFilter))]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMarketplaceApiService _marketplaceApi;
        private readonly ICarApiService _carApi;
        private readonly IApiAccessTokenService _tokenService;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IMarketplaceApiService marketplaceApi,
            ICarApiService carApi,
            IApiAccessTokenService tokenService,
            IEmailSender emailSender,
            IWebHostEnvironment environment,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _marketplaceApi = marketplaceApi;
            _carApi = carApi;
            _tokenService = tokenService;
            _emailSender = emailSender;
            _environment = environment;
            _cache = cache;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Account - AutoVault";
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            var model = new AccountDashboardViewModel
            {
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? user.Email ?? user.UserName ?? "User"
                    : displayName
            };
            try
            {
                model.Summary = await _marketplaceApi.GetSummaryAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult SignIn(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Sign In - AutoVault";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(string email, string password, bool rememberMe = true, string? returnUrl = null, bool fromAccount = false)
        {
            ViewData["Title"] = "Sign In - AutoVault";

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            IActionResult InvalidLogin()
            {
                if (fromAccount)
                {
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction(nameof(SignIn), new { returnUrl });
            }

            ViewData["ReturnUrl"] = returnUrl;

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

            if (!user.EmailConfirmed)
            {
                TempData["Error"] = "Please confirm your email before signing in.";
                TempData["UnconfirmedEmail"] = user.Email;
                return InvalidLogin();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                if (IsSafeReturnUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl!);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsNotAllowed)
            {
                TempData["Error"] = "Please confirm your email before signing in.";
                TempData["UnconfirmedEmail"] = user.Email;
                return InvalidLogin();
            }
            
            TempData["Error"] = "Invalid email or password.";
            return InvalidLogin();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Register - AutoVault";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string? firstName = null, string? lastName = null, bool fromAccount = false)
        {
            ViewData["Title"] = "Register - AutoVault";

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            IActionResult InvalidRegistration()
            {
                if (fromAccount)
                {
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction(nameof(Register));
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
                if (!await _roleManager.RoleExistsAsync(AppRoles.Member))
                {
                    await _userManager.DeleteAsync(user);
                    TempData["Error"] = "The member role is not available. Please try again.";
                    return InvalidRegistration();
                }

                var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Member);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    TempData["Error"] = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return InvalidRegistration();
                }

                await SendConfirmationAsync(user);
                return RedirectToAction(nameof(CheckEmail), new { email = user.Email });
            }

            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return InvalidRegistration();
        }

        [HttpGet]
        public IActionResult CheckEmail(string? email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Check Your Email - AutoVault";
            ViewData["Email"] = email;
            if (!string.IsNullOrWhiteSpace(email)
                && _cache.TryGetValue(DevelopmentLinkKey(email), out string? link))
            {
                ViewData["DevelopmentConfirmationLink"] = link;
            }

            ViewData["EmailConfigured"] = _emailSender.IsConfigured;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            ViewData["Title"] = "Confirm Email - AutoVault";
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(token))
            {
                ViewData["Confirmed"] = false;
                return View();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            ViewData["Confirmed"] = result.Succeeded;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(SignIn));
            }

            var cacheKey = $"confirm-resend:{email.Trim().ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                TempData["Error"] = "Please wait a minute before requesting another confirmation email.";
                return RedirectToAction(nameof(CheckEmail), new { email });
            }

            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !user.EmailConfirmed)
            {
                await SendConfirmationAsync(user);
            }

            TempData["Success"] = _emailSender.IsConfigured
                ? "If the account exists and is not confirmed, a new confirmation email has been sent."
                : "Email delivery is not configured. A new confirmation link was prepared for development.";
            return RedirectToAction(nameof(CheckEmail), new { email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("SignOut")]
        public async Task<IActionResult> SignOutUser()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
            if (User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                return Forbid();
            }
            ViewData["Title"] = "Add Car - AutoVault";
            try
            {
                return View(new CarEditorViewModel
                {
                    Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted)
                });
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
                return View(new CarEditorViewModel());
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCar(CarEditorViewModel model)
        {
            if (User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                await _carApi.CreateCarAsync(
                    model.Car,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                TempData["Success"] = "Your car listing was created.";
                return RedirectToAction(nameof(Listings));
            }
            catch (CarApiException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Listings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ViewData["Title"] = "My Listings - AutoVault";
            try
            {
                var cars = await _carApi.GetMyCarsAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return View(new AccountListingsViewModel { Cars = cars });
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
                return View(new AccountListingsViewModel());
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditListing(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var cars = await _carApi.GetMyCarsAsync(
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
            var car = cars.SingleOrDefault(item => item.Id == id);
            if (car == null && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.SuperAdmin))
            {
                return Forbid();
            }

            car ??= await _carApi.GetCarByIdAsync(id, HttpContext.RequestAborted);
            if (car == null)
            {
                return NotFound();
            }

            return View(new CarEditorViewModel
            {
                Id = id,
                Car = new SaveCarDto
                {
                    BrandId = car.Brand?.Id ?? 0,
                    CategoryId = car.Category?.Id ?? 0,
                    Model = car.Model,
                    Year = car.Year,
                    Price = car.Price,
                    Mileage = car.Mileage,
                    FuelType = car.FuelType,
                    Transmission = car.Transmission,
                    Color = car.Color,
                    Description = car.Description,
                    ImageUrl = car.MainImage,
                    Status = car.Status
                },
                Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted)
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListing(int id, CarEditorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }

            try
            {
                await _carApi.UpdateCarAsync(
                    id,
                    model.Car,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                TempData["Success"] = "Your listing was updated.";
                return RedirectToAction(nameof(Listings));
            }
            catch (CarApiException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                model.Id = id;
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                await _carApi.DeleteCarAsync(
                    id,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                TempData["Success"] = "The listing was deleted.";
            }
            catch (CarApiException exception)
            {
                TempData["Error"] = exception.Message;
            }

            return RedirectToAction(nameof(Listings));
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ViewData["Title"] = "Favorites - AutoVault";
            try
            {
                var favorites = await _marketplaceApi.GetFavoritesAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return View(new AccountFavoritesViewModel { Favorites = favorites });
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
                return View(new AccountFavoritesViewModel());
            }
        }

        [Authorize]
        public async Task<IActionResult> Messages()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ViewData["Title"] = "Messages - AutoVault";
            try
            {
                var conversations = await _marketplaceApi.GetConversationsAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return View(new AccountMessagesViewModel { Conversations = conversations });
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
                return View(new AccountMessagesViewModel());
            }
        }

        [Authorize]
        [HttpGet("Account/Messages/{id:int}")]
        public async Task<IActionResult> Conversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var conversation = await _marketplaceApi.GetConversationAsync(
                id,
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
            return conversation == null ? NotFound() : View(conversation);
        }

        [Authorize]
        [HttpPost("Account/Messages/{id:int}/Reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Enter a message.";
                return RedirectToAction(nameof(Conversation), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _marketplaceApi.ReplyAsync(
                id,
                content,
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
            return RedirectToAction(nameof(Conversation), new { id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            return View(new AccountProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AccountProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            model.Email = user.Email ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Your profile was updated.";
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access Denied - AutoVault";
            return View();
        }

        private async Task SendConfirmationAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);
            if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            if (!_emailSender.IsConfigured)
            {
                if (_environment.IsDevelopment())
                {
                    _cache.Set(DevelopmentLinkKey(user.Email), link, TimeSpan.FromMinutes(30));
                }

                return;
            }

            var text = $"Confirm your AutoVault account: {link}";
            var html = $"<p>Confirm your AutoVault account.</p><p><a href=\"{link}\">Confirm email</a></p>";
            await _emailSender.SendAsync(user.Email, "Confirm your AutoVault account", html, text);
        }

        private static string DevelopmentLinkKey(string email) =>
            $"dev-confirm:{email.Trim().ToLowerInvariant()}";

        private bool IsSafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return false;
            }

            var path = returnUrl.Split('?', '#')[0];
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            path = path.TrimEnd('/');
            return !path.Equals("/Account/SignIn", StringComparison.OrdinalIgnoreCase)
                && !path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> CreateAccessTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return _tokenService.CreateToken(User, roles);
        }
    }

    public sealed class RedirectAuthenticatedAuthPageOnAntiforgeryFailureFilter : IAlwaysRunResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not AntiforgeryValidationFailedResult)
            {
                return;
            }

            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var action = context.RouteData.Values["action"]?.ToString();
            if (!string.Equals(action, "SignIn", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(action, "Register", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            context.Result = new RedirectToActionResult("Index", "Account", null);
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}