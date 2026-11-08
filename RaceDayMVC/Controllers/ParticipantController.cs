using Microsoft.AspNetCore.Mvc;

namespace RaceDayMVC.Controllers
{
    public class ParticipantController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is logged in and is a Participant
            if (HttpContext.Session.GetString("UserRole") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
    }
}