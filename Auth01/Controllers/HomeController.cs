using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Auth01.Models;

namespace Auth01.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Home Index action called");
            _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated ?? false);
            _logger.LogInformation("User.Identity.Name: {UserName}", User.Identity?.Name ?? "null");
            _logger.LogInformation("User.Claims.Count: {ClaimsCount}", User.Claims.Count());

            // Log some key claims
            var nameClaim = User.FindFirst(ClaimTypes.Name);
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            _logger.LogInformation("Name claim: {NameClaim}", nameClaim?.Value ?? "null");
            _logger.LogInformation("Email claim: {EmailClaim}", emailClaim?.Value ?? "null");

            return View();
        }

        [Authorize]
        public IActionResult Profile()
        {
            var user = User;
            var claims = user.Claims.Select(c => new ClaimViewModel { Type = c.Type, Value = c.Value }).ToList();

            // Create a dynamic object with user information
            var profileViewModel = new
            {
                FirstName = user.FindFirstValue("FirstName") ?? "N/A",
                LastName = user.FindFirstValue("LastName") ?? "N/A",
                Email = user.FindFirstValue(ClaimTypes.Email) ?? "N/A",
                Role = user.FindFirstValue(ClaimTypes.Role) ?? "User",
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "N/A",
                Claims = claims
            };

            return View(profileViewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
