using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FinalProject.MVC.Configuration;
using FinalProject.MVC.Hubs;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Exceptions;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services;
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
        private readonly INotificationApiService _notificationApi;
        private readonly ICarApiService _carApi;
        private readonly IApiAccessTokenService _tokenService;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<ConversationHub> _conversations;
        private readonly ILogger<AccountController> _logger;
        private readonly AccountActivityHistory _activityHistory;
        private readonly IContactRequestService _contactRequests;
        private readonly IShopCatalogService _shopCatalog;
        private readonly ICarImageUploadService _carImages;
        private readonly ApplicationSettings _applicationSettings;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IMarketplaceApiService marketplaceApi,
            INotificationApiService notificationApi,
            ICarApiService carApi,
            IApiAccessTokenService tokenService,
            IEmailSender emailSender,
            IWebHostEnvironment environment,
            IMemoryCache cache,
            IHubContext<ConversationHub> conversations,
            ILogger<AccountController> logger,
            AccountActivityHistory activityHistory,
            IContactRequestService contactRequests,
            IShopCatalogService shopCatalog,
            ICarImageUploadService carImages,
            IOptions<ApplicationSettings>? applicationSettings = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _marketplaceApi = marketplaceApi;
            _notificationApi = notificationApi;
            _carApi = carApi;
            _tokenService = tokenService;
            _emailSender = emailSender;
            _environment = environment;
            _cache = cache;
            _conversations = conversations;
            _logger = logger;
            _activityHistory = activityHistory;
            _contactRequests = contactRequests;
            _shopCatalog = shopCatalog;
            _carImages = carImages;
            _applicationSettings = applicationSettings?.Value ?? new ApplicationSettings();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var displayName = AccountDisplayName.Resolve(user.FirstName, user.LastName, user.UserName, user.Email);
            ViewData["Title"] = $"{displayName} - AutoVault";
            var model = new AccountDashboardViewModel
            {
                DisplayName = displayName
            };
            try
            {
                model.Summary = await _marketplaceApi.GetSummaryAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                model.Summary.RecentActivity = (await _activityHistory.FilterAsync(
                    user,
                    model.Summary.RecentActivity,
                    HttpContext.RequestAborted)).ToList();
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("Account/Activity/Clear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearActivity()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _activityHistory.ClearAllAsync(user, HttpContext.RequestAborted);
            return Json(new { cleared = true });
        }

        [Authorize]
        [HttpPost("Account/Activity/Hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideActivity(string key)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!await _activityHistory.HideAsync(user, key, HttpContext.RequestAborted))
            {
                return BadRequest(new { message = "That activity entry could not be removed." });
            }

            return Json(new { hidden = true, key = key?.Trim() });
        }

        [HttpGet]
        public async Task<IActionResult> SignIn(string? returnUrl = null)
        {
            if (await SignedInUserMayEnterAsync())
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Sign In - AutoVault";
            ViewData["ReturnUrl"] = returnUrl;
            // Keep the confirmed email visible (?email=... or TempData). Do not add a second
            // string parameter here — it collides with POST SignIn(identifier, password).
            var rememberedEmail = Request.Query["email"].FirstOrDefault()
                ?? TempData["ConfirmedEmail"] as string;
            if (!string.IsNullOrWhiteSpace(rememberedEmail))
            {
                ViewData["Identifier"] = rememberedEmail.Trim();
            }

            return View();
        }

        /// <summary>
        /// Compatible alias for older bookmarks and external links that still use /Account/Login.
        /// Canonical route remains /Account/SignIn (cookie LoginPath).
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null) =>
            RedirectToAction(nameof(SignIn), new { returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Login(
            string? identifier,
            string password,
            bool rememberMe = true,
            string? returnUrl = null,
            bool fromAccount = false,
            string? email = null) =>
            SignIn(identifier, password, rememberMe, returnUrl, fromAccount, email);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(string? identifier, string password, bool rememberMe = true, string? returnUrl = null, bool fromAccount = false, string? email = null)
        {
            ViewData["Title"] = "Sign In - AutoVault";

            if (await SignedInUserMayEnterAsync())
            {
                return RedirectToAction(nameof(Index));
            }

            IActionResult InvalidLogin() =>
                RedirectToAction(nameof(SignIn), new { returnUrl });

            ViewData["ReturnUrl"] = returnUrl;
            identifier = string.IsNullOrWhiteSpace(identifier) ? email : identifier;
            TempData.Remove("UnconfirmedEmail");

            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Enter your email or username and your password.";
                return InvalidLogin();
            }

            var user = await FindByIdentifierAsync(identifier);

            if (user == null)
            {
                TempData["Error"] = "The email, username, or password is incorrect.";
                return InvalidLogin();
            }

            if (user.IsDeleted)
            {
                TempData["Error"] = "This account has been deactivated and can no longer sign in.";
                return InvalidLogin();
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                TempData["Error"] = "This account is temporarily locked after too many failed sign-in attempts. Try again later.";
                return InvalidLogin();
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    TempData["Error"] = "This account is temporarily locked after too many failed sign-in attempts. Try again later.";
                    return InvalidLogin();
                }

                TempData["Error"] = "The email, username, or password is incorrect.";
                return InvalidLogin();
            }

            if (!user.EmailConfirmed)
            {
                TempData["Error"] = "Please confirm your email address before signing in.";
                TempData["UnconfirmedEmail"] = user.Email;
                return InvalidLogin();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

            if (result.IsNotAllowed || !user.EmailConfirmed)
            {
                await _signInManager.SignOutAsync();
                TempData["Error"] = "Please confirm your email address before signing in.";
                TempData["UnconfirmedEmail"] = user.Email;
                return InvalidLogin();
            }

            if (result.Succeeded)
            {
                var fresh = await _userManager.FindByIdAsync(user.Id);
                if (fresh == null || !fresh.EmailConfirmed)
                {
                    await _signInManager.SignOutAsync();
                    TempData["Error"] = "Please confirm your email address before signing in.";
                    TempData["UnconfirmedEmail"] = user.Email;
                    return InvalidLogin();
                }

                fresh.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(fresh);

                if (IsSafeReturnUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl!);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                TempData["Error"] = "This account is temporarily locked after too many failed sign-in attempts. Try again later.";
                return InvalidLogin();
            }

            TempData["Error"] = "The email, username, or password is incorrect.";
            return InvalidLogin();
        }

        /// <summary>
        /// Confirmed, active accounts may continue. Anyone else is signed out first
        /// so an old cookie cannot skip the email-confirmation check.
        /// </summary>
        private async Task<bool> SignedInUserMayEnterAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var current = await _userManager.GetUserAsync(User);
            if (current is { EmailConfirmed: true, IsDeleted: false })
            {
                return true;
            }

            await _signInManager.SignOutAsync();
            return false;
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
        public async Task<IActionResult> Register(string email, string username, string password, string confirmPassword, string? firstName = null, string? lastName = null, bool fromAccount = false)
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

            var usernameError = AccountNameRules.ValidateUsername(username);
            if (usernameError != null)
            {
                TempData["Error"] = usernameError;
                return InvalidRegistration();
            }

            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return InvalidRegistration();
            }

            email = email.Trim();
            username = username.Trim();

            // Soft-deleted accounts still occupy AspNetUsers rows for history. Reclaim the
            // same email instead of blocking registration with "already registered".
            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null)
            {
                if (existingByEmail.IsDeleted)
                {
                    return await CompleteRegistrationForExistingSoftDeletedAsync(
                        existingByEmail,
                        email,
                        username,
                        password,
                        firstName,
                        lastName,
                        InvalidRegistration);
                }

                if (!existingByEmail.EmailConfirmed)
                {
                    TempData["Success"] =
                        "That email is already registered but not confirmed yet. Open the confirmation email, or resend it below.";
                    var pending = await SendConfirmationAsync(existingByEmail);
                    ApplyConfirmationTempData(pending);
                    return RedirectToAction(nameof(CheckEmail), new { email });
                }

                TempData["Error"] = "That email address is already registered.";
                return InvalidRegistration();
            }

            var existingByUsername = await _userManager.FindByNameAsync(username);
            if (existingByUsername != null)
            {
                if (!existingByUsername.IsDeleted)
                {
                    TempData["Error"] = "That username is already taken.";
                    return InvalidRegistration();
                }

                SoftDeletedAccountIdentity.ReleaseLoginIdentifiers(existingByUsername);
                await _userManager.UpdateAsync(existingByUsername);
            }

            var user = new ApplicationUser
            {
                UserName = username,
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

                var confirmation = await SendConfirmationAsync(user);
                ApplyConfirmationTempData(confirmation);
                return RedirectToAction(nameof(CheckEmail), new { email = user.Email });
            }

            TempData["Error"] = string.Join(" ", result.Errors.Select(RegistrationMessage));
            return InvalidRegistration();
        }

        private async Task<IActionResult> CompleteRegistrationForExistingSoftDeletedAsync(
            ApplicationUser user,
            string email,
            string username,
            string password,
            string? firstName,
            string? lastName,
            Func<IActionResult> invalidRegistration)
        {
            var usernameOwner = await _userManager.FindByNameAsync(username);
            if (usernameOwner != null
                && !string.Equals(usernameOwner.Id, user.Id, StringComparison.Ordinal)
                && !usernameOwner.IsDeleted)
            {
                TempData["Error"] = "That username is already taken.";
                return invalidRegistration();
            }

            if (usernameOwner != null
                && !string.Equals(usernameOwner.Id, user.Id, StringComparison.Ordinal)
                && usernameOwner.IsDeleted)
            {
                SoftDeletedAccountIdentity.ReleaseLoginIdentifiers(usernameOwner);
                await _userManager.UpdateAsync(usernameOwner);
            }

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.LockoutEnd = null;
            user.LockoutEnabled = true;
            user.AccessFailedCount = 0;
            user.Email = email;
            user.UserName = username;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.EmailConfirmed = false;
            user.NormalizedEmail = _userManager.NormalizeEmail(email);
            user.NormalizedUserName = _userManager.NormalizeName(username);

            if (await _userManager.HasPasswordAsync(user))
            {
                await _userManager.RemovePasswordAsync(user);
            }

            var passwordResult = await _userManager.AddPasswordAsync(user, password);
            if (!passwordResult.Succeeded)
            {
                TempData["Error"] = string.Join(" ", passwordResult.Errors.Select(RegistrationMessage));
                return invalidRegistration();
            }

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                TempData["Error"] = string.Join(" ", update.Errors.Select(RegistrationMessage));
                return invalidRegistration();
            }

            if (!await _roleManager.RoleExistsAsync(AppRoles.Member))
            {
                TempData["Error"] = "The member role is not available. Please try again.";
                return invalidRegistration();
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Member))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Member);
                if (!roleResult.Succeeded)
                {
                    TempData["Error"] = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return invalidRegistration();
                }
            }

            await _userManager.UpdateSecurityStampAsync(user);
            var confirmation = await SendConfirmationAsync(user);
            ApplyConfirmationTempData(confirmation);
            return RedirectToAction(nameof(CheckEmail), new { email = user.Email });
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
            var developmentMailbox = HttpContext.RequestServices.GetService<IDevelopmentMailStore>();
            ViewData["DevelopmentMailboxEnabled"] = developmentMailbox != null && _environment.IsDevelopment();
            ViewData["ExternalProviderConfigured"] = _emailSender.IsExternalConfigured;
            ViewData["ExternalEmailDelivered"] = TempData["ExternalEmailDelivered"] as bool? == true;
            ViewData["LocalMailboxStored"] = TempData["LocalMailboxStored"] as bool? == true;
            // Discard any leftover shortcut confirmation link — confirm only via the email.
            _ = TempData["DevelopmentConfirmationLink"];

            ViewData["EmailConfigured"] = _emailSender.IsConfigured;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            ViewData["Title"] = "Confirm Email - AutoVault";
            const string invalidMessage = "This confirmation link is invalid, expired, or has already been used.";
            var user = string.IsNullOrWhiteSpace(userId)
                ? null
                : await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(token))
            {
                ViewData["ErrorMessage"] = invalidMessage;
                return View("ConfirmEmailFailed");
            }

            if (!await TryConfirmEmailAsync(user, token))
            {
                ViewData["ErrorMessage"] = invalidMessage;
                return View("ConfirmEmailFailed");
            }

            await _userManager.UpdateSecurityStampAsync(user);

            // If the member skipped optional names, keep a readable name from the confirmed email.
            if (string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                && !string.IsNullOrWhiteSpace(user.Email))
            {
                var localPart = user.Email.Split('@')[0].Trim();
                if (!string.IsNullOrWhiteSpace(localPart))
                {
                    user.FirstName = localPart;
                    await _userManager.UpdateAsync(user);
                }
            }

            TempData["ConfirmedEmail"] = user.Email;
            ViewData["ConfirmedEmail"] = user.Email;
            return View("ConfirmEmail");
        }

        [HttpGet]
        public IActionResult ResendConfirmation()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Resend Confirmation Email - AutoVault";
            return View(new ResendConfirmationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Resend Confirmation Email - AutoVault";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var cacheKey = $"confirm-resend:{email.ToLowerInvariant()}";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && !user.EmailConfirmed && !user.IsDeleted)
                {
                    var confirmation = await SendConfirmationAsync(user);
                    ApplyConfirmationTempData(confirmation);
                }
            }

            // Do not claim the message was sent when the external provider did not deliver it.
            if (TempData["Error"] == null
                && TempData["ExternalEmailDelivered"] as bool? != true
                && _emailSender.IsExternalConfigured)
            {
                TempData["Success"] = $"If {email} is waiting for confirmation, check that inbox and spam folder.";
            }

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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Forgot Password - AutoVault";
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Forgot Password - AutoVault";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var cacheKey = $"password-reset:{email.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                TempData["Error"] = "Please wait a minute before requesting another password reset.";
                return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email });
            }

            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !user.IsDeleted)
            {
                await SendPasswordResetAsync(user);
            }

            // Same outcome whether or not the account exists — do not reveal membership.
            return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email });
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation(string? email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Check Your Email - AutoVault";
            ViewData["Email"] = email;
            ViewData["EmailConfigured"] = _emailSender.IsConfigured;
            if (!string.IsNullOrWhiteSpace(email)
                && _cache.TryGetValue(DevelopmentResetLinkKey(email), out string? link))
            {
                ViewData["DevelopmentResetLink"] = link;
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Reset Password - AutoVault";
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "That password reset link is invalid or incomplete.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Reset Password - AutoVault";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || user.IsDeleted)
            {
                // Do not reveal whether the user id was valid; deleted accounts cannot regain access.
                TempData["Success"] = "If the account exists, the password has been updated. You can sign in now.";
                return RedirectToAction(nameof(SignIn));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, PasswordResetMessage(error));
                }

                return View(model);
            }

            user.LastPasswordResetAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["Success"] = "Your password was updated. You can sign in now.";
            return RedirectToAction(nameof(SignIn));
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] = "Change Password - AutoVault";
            ViewData["BodyClass"] = "account-page-body";
            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            ViewData["Title"] = "Change Password - AutoVault";
            ViewData["BodyClass"] = "account-page-body";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, PasswordResetMessage(error));
                }

                return View(model);
            }

            user.LastPasswordChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Your password was changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
        [HttpGet]
        public IActionResult AddSparePart()
        {
            return RedirectToAction("Create", "AdminParts");
        }

        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
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

        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 64 * 1024 * 1024)]
        public async Task<IActionResult> AddCar(CarEditorViewModel model, List<IFormFile>? vehiclePhotos)
        {
            var uploaded = await PrepareListingImagesAsync(model, vehiclePhotos, requireImage: true);
            if (!ModelState.IsValid)
            {
                DiscardUploads(model, uploaded);
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                DiscardUploads(model, uploaded);
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
                DiscardUploads(model, uploaded);
                ModelState.AddModelError(string.Empty, exception.Message);
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                return View(model);
            }
        }

        private async Task<List<string>> PrepareListingImagesAsync(
            CarEditorViewModel model,
            IEnumerable<IFormFile>? vehiclePhotos,
            bool requireImage)
        {
            var uploaded = new List<string>();
            var previousImage = model.Car.ImageUrl;
            var photos = (vehiclePhotos ?? Array.Empty<IFormFile>())
                .Where(file => file is { Length: > 0 })
                .ToList();
            var error = await _carImages.ApplyVehiclePhotosAsync(
                model.Car,
                photos,
                requireImage,
                HttpContext.RequestAborted);
            if (error != null)
            {
                ModelState.AddModelError("Car.ImageUrl", error);
            }
            else if (!string.IsNullOrWhiteSpace(model.Car.ImageUrl))
            {
                ModelState.Remove("Car.ImageUrl");
            }

            if (photos.Count > 0 && ModelState["Car.ImageUrl"]?.Errors.Count is null or 0)
            {
                uploaded.Add(model.Car.ImageUrl);
                uploaded.AddRange(model.Car.AdditionalImageUrls);
                if (model.Car.AdditionalImageUrls.Count > 0)
                {
                    model.Car.GallerySpecified = true;
                }
            }

            HttpContext.Items["listing-original-image"] = previousImage;
            return uploaded;
        }

        private void DiscardUploads(CarEditorViewModel model, IReadOnlyList<string> uploaded)
        {
            _carImages.DeleteUploads(uploaded);
            if (uploaded.Any(path => string.Equals(path, model.Car.ImageUrl, StringComparison.OrdinalIgnoreCase)))
            {
                var original = HttpContext.Items["listing-original-image"] as string ?? string.Empty;
                model.Car.ImageUrl = uploaded.Any(path => string.Equals(path, original, StringComparison.OrdinalIgnoreCase))
                    ? string.Empty
                    : original;
            }

            model.Car.AdditionalImageUrls.Clear();
            model.Car.GallerySpecified = model.Id != null;
        }

        private async Task ReloadListingGalleryAsync(CarEditorViewModel model, ApplicationUser user)
        {
            if (model.Id is not int id)
            {
                return;
            }

            try
            {
                var car = await _carApi.GetCarByIdAsync(
                    id,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                model.Gallery = car?.Images
                    .OrderBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .ToList() ?? new List<CarImageApiDto>();
            }
            catch (CarApiException)
            {
                // Keep posted gallery state if reload fails.
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

            car ??= await _carApi.GetCarByIdAsync(
                id,
                await CreateAccessTokenAsync(user),
                HttpContext.RequestAborted);
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
                    Status = car.Status,
                    PrimaryImageId = car.Images.FirstOrDefault(image => image.IsMain)?.Id
                        ?? car.Images.OrderBy(image => image.DisplayOrder).ThenBy(image => image.Id).FirstOrDefault()?.Id,
                    Seats = car.Seats,
                    DriveType = car.DriveType,
                    Engine = car.Engine,
                    MileageUnit = car.MileageUnit,
                    Horsepower = car.Horsepower,
                    Doors = car.Doors,
                    Cylinders = car.Cylinders,
                    Steering = car.Steering,
                    Vin = car.Vin,
                    AccidentHistory = car.AccidentHistory,
                    RepaintedParts = car.RepaintedParts,
                    ReplacedParts = car.ReplacedParts,
                    CustomsStatus = car.CustomsStatus,
                    MarketOrigin = car.MarketOrigin,
                    Condition = car.IsNew ? "New" : "Used",
                    Features = car.Features ?? new List<string>()
                },
                Gallery = car.Images
                    .OrderBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .ToList(),
                Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted)
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 64 * 1024 * 1024)]
        public async Task<IActionResult> EditListing(int id, CarEditorViewModel model, List<IFormFile>? vehiclePhotos)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            model.Id = id;
            model.Car.GallerySpecified = true;
            var uploaded = await PrepareListingImagesAsync(model, vehiclePhotos, requireImage: true);
            if (!ModelState.IsValid)
            {
                DiscardUploads(model, uploaded);
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                await ReloadListingGalleryAsync(model, user);
                return View(model);
            }

            try
            {
                var update = await _carApi.UpdateCarAsync(
                    id,
                    model.Car,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                _carImages.DeleteUploads(update.RemovedImageUrls);
                await BroadcastNotificationsAsync(update.Notifications);
                TempData["Success"] = "Your listing was updated.";
                return RedirectToAction(nameof(Listings));
            }
            catch (CarApiException exception)
            {
                DiscardUploads(model, uploaded);
                ModelState.AddModelError(string.Empty, exception.Message);
                model.Id = id;
                model.Lookups = await _carApi.GetLookupsAsync(HttpContext.RequestAborted);
                await ReloadListingGalleryAsync(model, user);
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
                var token = await CreateAccessTokenAsync(user);
                var favorites = await _marketplaceApi.GetFavoritesAsync(token, HttpContext.RequestAborted);
                var productFavorites = await _marketplaceApi.GetProductFavoritesAsync(
                    token,
                    HttpContext.RequestAborted);
                return View(new AccountFavoritesViewModel
                {
                    Favorites = favorites,
                    ProductFavorites = productFavorites
                });
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
            var wantsJson = WantsJson();
            if (string.IsNullOrWhiteSpace(content))
            {
                return MessageFailure(id, wantsJson, "Enter a message.");
            }

            if (content.Trim().Length > 4000)
            {
                return MessageFailure(id, wantsJson, "A message cannot exceed 4000 characters.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var token = await CreateAccessTokenAsync(user);
                var message = await _marketplaceApi.ReplyAsync(
                    id,
                    content,
                    token,
                    HttpContext.RequestAborted);
                try
                {
                    await _conversations.Clients
                        .Group(ConversationHub.GroupName(id))
                        .SendAsync(
                            ConversationHub.MessageReceivedEvent,
                            message,
                            HttpContext.RequestAborted);

                    var details = await _marketplaceApi.GetConversationAsync(
                        id,
                        token,
                        HttpContext.RequestAborted);
                    if (details != null)
                    {
                        await BroadcastConversationRestoredAsync(details, message);
                    }

                    if (message.Notification != null)
                    {
                        await BroadcastNotificationAsync(message.Notification);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Conversation {ConversationId} was saved but not broadcast.", id);
                }

                return wantsJson
                    ? Json(message)
                    : RedirectToAction(nameof(Conversation), new { id });
            }
            catch (CarApiException exception)
            {
                return MessageFailure(id, wantsJson, exception.Message);
            }
        }

        [Authorize]
        [HttpGet("Account/Notifications")]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ViewData["Title"] = "Notifications - AutoVault";
            try
            {
                var token = await CreateAccessTokenAsync(user);
                var list = await _notificationApi.GetAsync(token, 50, HttpContext.RequestAborted);
                // Persist read for items the user is viewing; badge JS clears counts without a reload.
                if (list.Items.Exists(item => !item.IsRead))
                {
                    await _notificationApi.MarkAllReadAsync(token, HttpContext.RequestAborted);
                    ViewData["NotificationsMarkedRead"] = true;
                }

                return View(list);
            }
            catch (CarApiException)
            {
                ViewData["AccountServiceUnavailable"] = true;
                return View(new NotificationListDto());
            }
        }

        [Authorize]
        [HttpGet("Account/Notifications/Count")]
        public async Task<IActionResult> NotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                var counts = await _notificationApi.GetUnreadCountsAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return Json(counts);
            }
            catch (CarApiException)
            {
                return Json(new NotificationUnreadCountsDto());
            }
        }

        [Authorize]
        [HttpPost("Account/Notifications/{id:int}/Read")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id, string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var counts = await _notificationApi.MarkReadAsync(
                    id,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);

                if (WantsJson())
                {
                    return Json(new
                    {
                        read = true,
                        counts,
                        returnUrl = IsSafeLocalUrl(returnUrl) ? returnUrl : null
                    });
                }

                if (IsSafeLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl!);
                }

                return RedirectToAction(nameof(Notifications));
            }
            catch (CarApiException exception)
            {
                if (WantsJson())
                {
                    return BadRequest(new { message = exception.Message });
                }

                TempData["Error"] = exception.Message;
                return RedirectToAction(nameof(Notifications));
            }
        }

        [Authorize]
        [HttpPost("Account/Notifications/ReadAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var counts = await _notificationApi.MarkAllReadAsync(
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                if (WantsJson())
                {
                    return Json(new { marked = true, counts });
                }

                TempData["Success"] = "All notifications marked as read.";
            }
            catch (CarApiException exception)
            {
                if (WantsJson())
                {
                    return BadRequest(new { message = exception.Message });
                }

                TempData["Error"] = exception.Message;
            }

            return RedirectToAction(nameof(Notifications));
        }

        private bool IsSafeLocalUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && Url.IsLocalUrl(url)
            && url.StartsWith('/')
            && !url.StartsWith("//", StringComparison.Ordinal);

        [Authorize]
        [HttpPost("Account/Messages/{id:int}/Hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                await _marketplaceApi.HideConversationAsync(
                    id,
                    await CreateAccessTokenAsync(user),
                    HttpContext.RequestAborted);
                return Json(new { hidden = true, id });
            }
            catch (CarApiException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ContactRequests(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "My Contact Requests - AutoVault";
            ViewData["BodyClass"] = "account-page-body";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Only claim orphans whose form email matches this account's email.
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _contactRequests.ClaimOrphanedByVerifiedEmailAsync(
                    user.Id,
                    user.Email,
                    cancellationToken);
            }

            var requests = await _contactRequests.GetForUserAsync(user.Id, cancellationToken);
            return View(requests);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProductQuestions(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "My Product Questions - AutoVault";
            ViewData["BodyClass"] = "account-page-body";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _shopCatalog.ClaimOrphanedInquiriesByEmailAsync(
                    user.Id,
                    user.Email,
                    cancellationToken);
            }

            var questions = await _shopCatalog.GetInquiriesForUserAsync(user.Id, cancellationToken);
            return View(questions);
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

            await PrepareProfilePageAsync(user);
            return View(await BuildProfileViewModelAsync(user));
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

            await PrepareProfilePageAsync(user);
            model.Email = user.Email ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
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

        /// <summary>
        /// Soft-deletes the signed-in Member's own account after password confirmation.
        /// Admin/SuperAdmin cannot use this endpoint. Historical records are preserved.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (await IsStaffAccountAsync(user))
            {
                return Forbid();
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Member))
            {
                return Forbid();
            }

            if (user.IsDeleted)
            {
                await _signInManager.SignOutAsync();
                TempData["Success"] = "This account is already deactivated.";
                return RedirectToAction(nameof(SignIn));
            }

            await PrepareProfilePageAsync(user);
            ViewData["DeleteAccount"] = model;
            ViewData["DeleteAccountAttempted"] = true;

            // Re-assert confirmation rules so property errors stay visible even if
            // binder/attribute edge cases leave ModelState incomplete.
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.Remove(nameof(DeleteAccountViewModel.CurrentPassword));
                ModelState.AddModelError(
                    nameof(DeleteAccountViewModel.CurrentPassword),
                    "Enter your current password.");
            }

            if (!model.ConfirmDeactivation)
            {
                ModelState.Remove(nameof(DeleteAccountViewModel.ConfirmDeactivation));
                ModelState.AddModelError(
                    nameof(DeleteAccountViewModel.ConfirmDeactivation),
                    "Confirm that you want to deactivate this account.");
            }

            if (!ModelState.IsValid)
            {
                return View(nameof(Profile), await BuildProfileViewModelAsync(user));
            }

            if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
            {
                ModelState.AddModelError(
                    nameof(DeleteAccountViewModel.CurrentPassword),
                    "The password is incorrect.");
                return View(nameof(Profile), await BuildProfileViewModelAsync(user));
            }

            // Soft-delete only: keep AspNetUsers row and all historical associations,
            // but release email/username so a new member can register with them.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            SoftDeletedAccountIdentity.ReleaseLoginIdentifiers(user);
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var error in update.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(nameof(Profile), await BuildProfileViewModelAsync(user));
            }

            // Invalidate any existing cookie/session immediately.
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync();

            TempData["Success"] = "Your account has been deactivated. You can no longer sign in.";
            return RedirectToAction(nameof(SignIn));
        }

        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access Denied - AutoVault";
            return View();
        }

        private async Task<bool> IsStaffAccountAsync(ApplicationUser user) =>
            await _userManager.IsInRoleAsync(user, AppRoles.Admin)
            || await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);

        private async Task PrepareProfilePageAsync(ApplicationUser user)
        {
            ViewData["Title"] = "Profile Settings - AutoVault";
            ViewData["BodyClass"] = "account-page-body";
            var isMember = await _userManager.IsInRoleAsync(user, AppRoles.Member);
            var isStaff = await IsStaffAccountAsync(user);
            ViewData["ShowDeleteAccount"] = isMember && !isStaff && !user.IsDeleted;
            ViewData["DeleteAccount"] ??= new DeleteAccountViewModel();
        }

        private static Task<AccountProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user) =>
            Task.FromResult(new AccountProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            });

        private async Task<ApplicationUser?> FindByIdentifierAsync(string identifier)
        {
            var value = identifier.Trim();
            if (value.Contains('@'))
            {
                return await _userManager.FindByEmailAsync(value)
                    ?? await _userManager.FindByNameAsync(value);
            }

            return await _userManager.FindByNameAsync(value)
                ?? await _userManager.FindByEmailAsync(value);
        }

        private static string RegistrationMessage(IdentityError error)
        {
            return error.Code switch
            {
                "DuplicateUserName" => "That username is already taken.",
                "DuplicateEmail" => "That email address is already registered.",
                "InvalidUserName" => "Use 3 to 32 characters: letters, numbers, dots, underscores, or hyphens.",
                "PasswordTooShort" => "Password must be at least 6 characters.",
                "PasswordRequiresDigit" => "Password must include a number.",
                "PasswordRequiresLower" => "Password must include a lowercase letter.",
                "PasswordRequiresUpper" => "Password must include an uppercase letter.",
                _ => "The account could not be created. Check the username, email, and password."
            };
        }

        private const string ConfirmationDeliveryFailedMessage =
            "The confirmation email could not be delivered by the email provider. Please try Resend confirmation in a few minutes, and check spam if a later attempt succeeds.";

        private void ApplyConfirmationTempData(ConfirmationSendResult confirmation)
        {
            TempData["ExternalEmailDelivered"] = confirmation.ExternalDelivered;
            TempData["LocalMailboxStored"] = confirmation.LocalMailboxStored;
            TempData["ExternalProviderConfigured"] = confirmation.ExternalProviderConfigured;

            if (confirmation.ExternalProviderConfigured && !confirmation.ExternalDelivered)
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(confirmation.ProviderError)
                    ? ConfirmationDeliveryFailedMessage
                    : confirmation.ProviderError;
            }
            else if (!confirmation.ExternalProviderConfigured && !confirmation.LocalMailboxStored)
            {
                TempData["Error"] = "Email delivery is not configured. No confirmation email was sent.";
            }
        }

        private async Task<ConfirmationSendResult> SendConfirmationAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return ConfirmationSendResult.Failed(externalConfigured: false);
            }

            // Rotate the stamp so only the link from this send can confirm the account.
            await _userManager.UpdateSecurityStampAsync(user);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = BuildPublicLink(nameof(ConfirmEmail), new { userId = user.Id, token = IdentityTokenCodec.Encode(token) });
            if (string.IsNullOrWhiteSpace(link))
            {
                return ConfirmationSendResult.Failed(_emailSender.IsExternalConfigured);
            }

            var externalConfigured = _emailSender.IsExternalConfigured;
            if (!_emailSender.IsConfigured)
            {
                return ConfirmationSendResult.NotConfigured();
            }

            try
            {
                var (html, text) = AccountEmailContent.Confirmation(link);
                await _emailSender.SendAsync(user.Email, AccountEmailContent.ConfirmationSubject, html, text);

                var localStored = _environment.IsDevelopment()
                    && HttpContext.RequestServices.GetService<IDevelopmentMailStore>() != null;
                if (externalConfigured)
                {
                    return ConfirmationSendResult.ExternalSent(localStored);
                }

                return ConfirmationSendResult.LocalMailboxOnly();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Confirmation email could not be sent to {Recipient}.", user.Email);
                var localStored = _environment.IsDevelopment()
                    && HttpContext.RequestServices.GetService<IDevelopmentMailStore>() is IDevelopmentMailStore store
                    && store.List(user.Email).Count > 0;
                return ConfirmationSendResult.Failed(
                    externalConfigured,
                    localStored,
                    SanitizeProviderError(ex));
            }
        }

        private static string SanitizeProviderError(Exception exception)
        {
            var message = exception.Message?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                return ConfirmationDeliveryFailedMessage;
            }

            // Never echo values that look like secrets.
            if (message.Contains("re_", StringComparison.OrdinalIgnoreCase)
                || (message.Contains("Password", StringComparison.OrdinalIgnoreCase) && message.Contains('=')))
            {
                return ConfirmationDeliveryFailedMessage + " See server logs for the provider response.";
            }

            return message.Length <= 400
                ? message
                : message[..400] + "…";
        }

        private readonly record struct ConfirmationSendResult(
            bool ExternalDelivered,
            bool LocalMailboxStored,
            bool ExternalProviderConfigured,
            string? ProviderError = null)
        {
            public static ConfirmationSendResult ExternalSent(bool localMailboxStored) =>
                new(true, localMailboxStored, true);

            public static ConfirmationSendResult LocalMailboxOnly() =>
                new(false, true, false);

            public static ConfirmationSendResult NotConfigured() =>
                new(false, false, false);

            public static ConfirmationSendResult Failed(
                bool externalConfigured,
                bool localMailboxStored = false,
                string? providerError = null) =>
                new(false, localMailboxStored, externalConfigured, providerError);
        }

        private async Task<bool> TryConfirmEmailAsync(ApplicationUser user, string token)
        {
            if (user.EmailConfirmed)
            {
                return false;
            }

            var candidate = token.Trim();
            var confirmed = false;
            if (IdentityTokenCodec.TryDecode(candidate, out var decoded)
                && !string.Equals(decoded, candidate, StringComparison.Ordinal))
            {
                confirmed = (await _userManager.ConfirmEmailAsync(user, decoded)).Succeeded;
            }

            if (!confirmed && !user.EmailConfirmed)
            {
                confirmed = (await _userManager.ConfirmEmailAsync(user, candidate)).Succeeded;
            }

            if (confirmed)
            {
                return true;
            }

            // A rejected token must not leave the account confirmed.
            var fresh = await _userManager.FindByIdAsync(user.Id);
            if (fresh is { EmailConfirmed: true })
            {
                fresh.EmailConfirmed = false;
                await _userManager.UpdateAsync(fresh);
                user.EmailConfirmed = false;
            }

            return false;
        }

        private string? BuildPublicLink(string action, object values)
        {
            var path = Url.Action(action, "Account", values);
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var configured = _applicationSettings.PublicBaseUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(configured)
                && Uri.TryCreate(configured, UriKind.Absolute, out var baseUri)
                && string.IsNullOrEmpty(baseUri.UserInfo)
                && (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp))
            {
                return new Uri(new Uri(configured.TrimEnd('/') + "/"), path.TrimStart('/')).AbsoluteUri;
            }

            if (!string.IsNullOrWhiteSpace(configured))
            {
                _logger.LogWarning("Application:PublicBaseUrl is not an absolute http or https URL. Email links used the current request host.");
            }

            return Url.Action(action, "Account", values, Request.Scheme, Request.Host.Value);
        }

        private async Task SendPasswordResetAsync(ApplicationUser user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(
                nameof(ResetPassword),
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
                    _cache.Set(DevelopmentResetLinkKey(user.Email), link, TimeSpan.FromMinutes(30));
                }

                return;
            }

            var text = $"Reset your AutoVault password: {link}";
            var html = $"<p>Reset your AutoVault password.</p><p><a href=\"{link}\">Reset password</a></p>";
            await _emailSender.SendAsync(user.Email, "Reset your AutoVault password", html, text);
        }

        private static string DevelopmentResetLinkKey(string email) =>
            $"dev-reset:{email.Trim().ToLowerInvariant()}";

        private static string PasswordResetMessage(IdentityError error) =>
            error.Code switch
            {
                "PasswordMismatch" => "The current password is incorrect.",
                "PasswordTooShort" => "Password must be at least 6 characters.",
                "PasswordRequiresDigit" => "Password must include a number.",
                "PasswordRequiresLower" => "Password must include a lowercase letter.",
                "PasswordRequiresUpper" => "Password must include an uppercase letter.",
                "InvalidToken" => "That password reset link is invalid or has expired.",
                _ => error.Description
            };

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
                && !path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
                && !path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase)
                && !path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase)
                && !path.Equals("/Account/ResetPassword", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> CreateAccessTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return _tokenService.CreateToken(User, roles);
        }

        private bool WantsJson() =>
            string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || Request.Headers.Accept.Any(value =>
                value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

        private IActionResult MessageFailure(int id, bool wantsJson, string message)
        {
            if (wantsJson)
            {
                return BadRequest(new { message });
            }

            TempData["Error"] = message;
            return RedirectToAction(nameof(Conversation), new { id });
        }

        private async Task BroadcastConversationRestoredAsync(
            ConversationDetailsDto details,
            MessageDto message)
        {
            var payload = new
            {
                details.Id,
                details.CarId,
                details.CarName,
                details.CarImageUrl,
                details.BuyerId,
                details.SellerId,
                details.BuyerDisplayName,
                details.SellerDisplayName,
                lastMessage = message.Content,
                updatedAt = message.CreatedAt,
                receiverId = message.ReceiverId
            };

            await _conversations.Clients
                .Group(ConversationHub.InboxGroupName(details.BuyerId))
                .SendAsync(
                    ConversationHub.ConversationRestoredEvent,
                    payload,
                    HttpContext.RequestAborted);
            await _conversations.Clients
                .Group(ConversationHub.InboxGroupName(details.SellerId))
                .SendAsync(
                    ConversationHub.ConversationRestoredEvent,
                    payload,
                    HttpContext.RequestAborted);
        }

        private async Task BroadcastNotificationAsync(NotificationDto notification)
        {
            await _conversations.Clients
                .Group(ConversationHub.InboxGroupName(notification.UserId))
                .SendAsync(
                    ConversationHub.NotificationReceivedEvent,
                    notification,
                    HttpContext.RequestAborted);
        }

        private async Task BroadcastNotificationsAsync(IEnumerable<NotificationDto> notifications)
        {
            foreach (var notification in notifications)
            {
                try
                {
                    await BroadcastNotificationAsync(notification);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Notification {NotificationId} was saved but not broadcast.",
                        notification.Id);
                }
            }
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
                && !string.Equals(action, "Login", StringComparison.OrdinalIgnoreCase)
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