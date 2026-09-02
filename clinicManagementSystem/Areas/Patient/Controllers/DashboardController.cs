using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Policy = "RequirePatient")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}