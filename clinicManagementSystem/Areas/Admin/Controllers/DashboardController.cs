using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}