using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;
using RaceDayMVC.ViewModels;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly IApiService _apiService;

        public ParticipantController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in and is a Participant
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch upcoming events from the API
            var response = await _apiService.GetAsync("events");

            var eventsList = new List<EventViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                eventsList = JsonSerializer.Deserialize<List<EventViewModel>>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return View(eventsList);
        }
    }
}