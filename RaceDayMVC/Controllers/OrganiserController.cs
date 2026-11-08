using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;
using RaceDayMVC.ViewModels;
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
    }
}
