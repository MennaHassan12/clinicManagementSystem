using clinicManagementSystem.Models;
using clinicManagementSystem.Utilities;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Identity.Controllers
{
    [Area(SD.IDENTITY_AREA)]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            ApplicationUserVM applicationUserVM = new()
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(applicationUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(ApplicationUserVM applicationUserVM)
        {
            if (!ModelState.IsValid)
                return View("Index", applicationUserVM);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            user.FullName = applicationUserVM.FullName;
            user.PhoneNumber = applicationUserVM.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", applicationUserVM);
            }

            TempData["success_notification"] = "Profile updated successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
