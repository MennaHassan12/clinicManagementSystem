using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctor")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}