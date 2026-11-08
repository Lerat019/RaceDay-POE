using Microsoft.AspNetCore.Mvc;

namespace RaceDayMVC.Controllers
{
    public class OrganiserController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is logged in and is an Organiser
            if (HttpContext.Session.GetString("UserRole") != "Organiser")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
    }
}
