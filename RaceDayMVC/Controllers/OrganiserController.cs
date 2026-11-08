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
                // 1. Convert the form data to JSON
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 2. Send it to the API to be saved in the database
                // (Assuming your API endpoint for creating an event is POST /api/events)
                var response = await _apiService.PostAsync("events", content);

                if (response.IsSuccessStatusCode)
                {
                    // Success! Go back to the dashboard
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"API Error ({(int)response.StatusCode}): {errorContent}");
                }

                    // If we get here, the API rejected it
                    ModelState.AddModelError("", "There was an error saving the event. Please check your data and try again.");
            }

            // If validation fails or API fails, reload the form with the data the user typed
            return View(model);
        }
    }
}
