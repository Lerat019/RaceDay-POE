using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;
using RaceDayMVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _apiService;

        public AccountController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // NOTE: Make sure "auth/login" matches your actual API route!
            var response = await _apiService.PostAsync("auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserViewModel>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (user != null)
                {
                    // Save user data to Session (Requirement 2)
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                    HttpContext.Session.SetInt32("UserId", user.Id);

                    // Redirect based on Role
                    if (user.Role == "Organiser")
                        return RedirectToAction("Index", "Organiser"); // We will create this later
                    else
                        return RedirectToAction("Index", "Participant"); // We will create this later
                }
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // NOTE: Make sure "auth/register" matches your actual API route!
            var response = await _apiService.PostAsync("auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                // Redirect to login after successful registration
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Registration failed. Email might already be in use.");
            return View(model);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}