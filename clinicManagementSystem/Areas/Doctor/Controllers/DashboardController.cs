using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}