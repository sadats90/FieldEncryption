using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Auth01.Models;
using Auth01.Services;
using System.Security.Claims;

namespace Auth01.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        #region Login / Register

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation("=== LOGIN ACTION CALLED ===");
            _logger.LogInformation($"Email: {model.Email}");
            _logger.LogInformation($"Password length: {model.Password?.Length ?? 0}");
            _logger.LogInformation($"ModelState valid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid) 
            {
                _logger.LogWarning("ModelState is invalid");
                return View(model);
            }

            var user = await _authService.ValidateUserAsync(model.Email, model.Password);
            if (user == null)
            {
                _logger.LogWarning("User validation failed");
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            _logger.LogInformation($"Login successful for user: {user.Email}");
            await SignInLocalUser(user, model.RememberMe);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _authService.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "A user with this email already exists.");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var success = await _authService.CreateUserAsync(user, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to create user account.");
                return View(model);
            }

            await SignInLocalUser(user, false);
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Logout

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Only sign out the registered "CustomAuth" scheme
            await HttpContext.SignOutAsync("CustomAuth");
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            return await Logout();
        }

        #endregion

        #region Helper

        private async Task SignInLocalUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CustomAuth", principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTime.UtcNow.AddHours(12)
            });
        }

        #endregion
    }
}
