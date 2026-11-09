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

            // GET: /Organiser/Edit/5
public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
                return RedirectToAction("Login", "Account");

            var response = await _apiService.GetAsync($"events/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var responseData = await response.Content.ReadAsStringAsync();
            var eventDetails = JsonSerializer.Deserialize<EventViewModel>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(eventDetails);
        }

        // POST: /Organiser/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _apiService.PutAsync($"events/{id}", content);

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

        // POST: /Organiser/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
                return RedirectToAction("Login", "Account");

            var response = await _apiService.DeleteAsync($"events/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Event deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not delete event. It might have categories attached.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Organiser/EnterResults/5
        public async Task<IActionResult> EnterResults(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
                return RedirectToAction("Login", "Account");

            // Fetch the Event Details
            var eventResponse = await _apiService.GetAsync($"events/{id}");
            if (!eventResponse.IsSuccessStatusCode) return NotFound();
            var eventData = await eventResponse.Content.ReadAsStringAsync();
            var eventDetails = JsonSerializer.Deserialize<EventViewModel>(eventData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Fetch the enrolments for this event
            var enrolResponse = await _apiService.GetAsync($"events/{id}/enrolments");
            var enrolmentsList = new List<EnrolmentViewModel>();

            if (enrolResponse.IsSuccessStatusCode)
            {
                var enrolData = await enrolResponse.Content.ReadAsStringAsync();
                enrolmentsList = JsonSerializer.Deserialize<List<EnrolmentViewModel>>(enrolData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            ViewBag.EventDetails = eventDetails;
            ViewBag.EventId = id;

            return View(enrolmentsList);
        }

            // POST: /Organiser/SubmitResults/5
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> SubmitResults(int id, List<ResultViewModel> results)
            {
                if (HttpContext.Session.GetString("UserRole") != "Organiser")
                    return RedirectToAction("Login", "Account");

                int successCount = 0;

                // Loop through each row and send the result to the API
                foreach (var result in results)
                {
                    // Skip if the organiser left the fields blank
                    if (result.Position == null && string.IsNullOrEmpty(result.FinishTime.ToString()))
                        continue;

                    var payload = new
                    {
                        EnrolmentId = result.EnrolmentId,
                        FinishTime = result.FinishTime,
                        Position = result.Position
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _apiService.PostAsync($"results", content);

                    if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                }

                TempData["SuccessMessage"] = $"Successfully published {successCount} results!";
                return RedirectToAction(nameof(Index));
            }
        }
    }