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

        // GET: /Participant (Upcoming Races)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Participant")
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

        // GET: /Participant/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            var eventResponse = await _apiService.GetAsync($"events/{id}");
            if (!eventResponse.IsSuccessStatusCode) return NotFound();

            var eventData = await eventResponse.Content.ReadAsStringAsync();
            var eventDetails = JsonSerializer.Deserialize<EventViewModel>(eventData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var categoryResponse = await _apiService.GetAsync($"events/{id}/categories");
            var categoriesList = new List<CategoryViewModel>();

            if (categoryResponse.IsSuccessStatusCode)
            {
                var categoryData = await categoryResponse.Content.ReadAsStringAsync();
                categoriesList = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoryData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var viewModel = new EventDetailsViewModel
            {
                Event = eventDetails,
                Categories = categoriesList
            };

            return View(viewModel);
        }

        // POST: /Participant/Enroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int eventId, int selectedCategoryId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedCategoryId == 0)
            {
                TempData["ErrorMessage"] = "Please select a category first.";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var response = await _apiService.PostAsync($"categories/{selectedCategoryId}/enrol", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Successfully enrolled!";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not enroll. You may already be enrolled, or the category is full.";
            }

            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        // GET: /Participant/MyEvents
        public async Task<IActionResult> MyEvents()
        {
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }

            int? participantId = HttpContext.Session.GetInt32("UserId");
            if (participantId == null) return RedirectToAction("Login", "Account");

            var response = await _apiService.GetAsync($"participants/{participantId}/enrolments");

            var enrolmentsList = new List<EnrolmentViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                enrolmentsList = JsonSerializer.Deserialize<List<EnrolmentViewModel>>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return View(enrolmentsList);
        }
    }
}