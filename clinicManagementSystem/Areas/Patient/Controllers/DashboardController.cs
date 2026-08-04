using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}