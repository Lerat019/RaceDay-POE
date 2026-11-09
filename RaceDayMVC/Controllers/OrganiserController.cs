using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;
using RaceDayMVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly IApiService _apiService;

        public OrganiserController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Organiser (Dashboard)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _apiService.GetAsync("events");

            var eventsList = new List<EventViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                eventsList = JsonSerializer.Deserialize<List<EventViewModel>>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return View(eventsList);
        }

        // GET: /Organiser/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: /Organiser/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _apiService.PostAsync("events", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"API Error ({(int)response.StatusCode}): {errorContent}");
                }
            }
            return View(model);
        }

        // GET: /Organiser/ManageCategories/5
        public async Task<IActionResult> ManageCategories(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch the Event Details
            var eventResponse = await _apiService.GetAsync($"events/{id}");
            if (!eventResponse.IsSuccessStatusCode) return NotFound();

            var eventData = await eventResponse.Content.ReadAsStringAsync();
            var eventDetails = JsonSerializer.Deserialize<EventViewModel>(eventData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Fetch existing Categories
            var catResponse = await _apiService.GetAsync($"events/{id}/categories");
            var categoriesList = new List<CategoryViewModel>();

            if (catResponse.IsSuccessStatusCode)
            {
                var catData = await catResponse.Content.ReadAsStringAsync();
                categoriesList = JsonSerializer.Deserialize<List<CategoryViewModel>>(catData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            ViewBag.EventDetails = eventDetails;
            ViewBag.EventId = id;

            return View(categoriesList);
        }

        // POST: /Organiser/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(int eventId, string categoryName, double distance, int maxParticipants, decimal entryFee)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }

            var payload = new
            {
                CategoryName = categoryName,
                Distance = distance,
                MaxParticipants = maxParticipants,
                EntryFee = entryFee
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send to API
            var response = await _apiService.PostAsync($"events/{eventId}/categories", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Category added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add category. Please try again.";
            }

            return RedirectToAction(nameof(ManageCategories), new { id = eventId });
        }
    }
}