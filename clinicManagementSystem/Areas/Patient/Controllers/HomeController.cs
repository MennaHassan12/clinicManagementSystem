using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.ViewModels;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoctorModel = clinicManagementSystem.Models.Doctor;
using Microsoft.AspNetCore.Authorization;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<BlogPost> _blogRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public HomeController(
            IRepository<Department> departmentRepo,
            IRepository<BlogPost> blogRepo,
            IRepository<DoctorModel> doctorRepo,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _departmentRepo = departmentRepo;
            _blogRepo = blogRepo;
            _doctorRepo = doctorRepo;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Departments = await _departmentRepo.GetAsync();

            var allBlogs = await _blogRepo.GetAsync();
            ViewBag.LatestBlogs = allBlogs
                .OrderByDescending(b => b.CreatedDate)
                .Take(3)
                .ToList();

            var featuredDoctors = await _doctorRepo.GetAsync(
                includes: [d => d.ApplicationUser, d => d.Department]
            );

            return View(featuredDoctors.Take(4).ToList());
        }

        public async Task<IActionResult> AllDoctors(string? searchTerm, int? departmentId)
        {
            ViewBag.Departments = await _departmentRepo.GetAsync();
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentDepartment = departmentId;

            var doctors = await FilterDoctorsAsync(searchTerm, departmentId);
            return View(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> SearchDoctors(string? searchTerm, int? departmentId)
        {
            var doctors = await FilterDoctorsAsync(searchTerm, departmentId);
            return PartialView("_DoctorListPartial", doctors);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContactMessage(ContactMessageVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var adminEmail = _configuration["EmailSettings:Email"];

                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        TempData["error_notification"] = "Admin email configuration is missing.";
                        return RedirectToAction(nameof(Index));
                    }

                    string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center;'>
                            <h2 style='margin: 0;'>Clinic Management System</h2>
                            <p style='margin: 5px 0 0 0;'>New Patient Inquiry</p>
                        </div>
                        <div style='padding: 20px; color: #333;'>
                            <p><strong>From:</strong> {model.Name} ({model.Email})</p>
                            <div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin-top: 15px; border-radius: 4px;'>
                                <p style='margin: 0; font-weight: bold;'>Message:</p>
                                <p style='margin: 5px 0 0 0; white-space: pre-line;'>{model.Message}</p>
                            </div>
                        </div>
                    </div>";

                    await _emailSender.SendEmailAsync(adminEmail, $"New Contact Inquiry from {model.Name}", emailBody);

                    TempData["success_notification"] = "Thank you! Your message has been sent successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Email Error: {ex.Message}");
                    TempData["error_notification"] = "Failed to send email. Please try again.";
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["error_notification"] = "Please fill in all required fields.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<DoctorModel>> FilterDoctorsAsync(string? searchTerm, int? departmentId)
        {
            searchTerm = searchTerm?.Trim();

            return await _doctorRepo.GetAsync(
                expression: d => (string.IsNullOrEmpty(searchTerm) ||
                                 (d.ApplicationUser != null && d.ApplicationUser.FullName.Contains(searchTerm)) ||
                                 (d.Department != null && d.Department.Name.Contains(searchTerm))) &&
                                 (!departmentId.HasValue || departmentId.Value <= 0 || d.DepartmentId == departmentId.Value),
                includes: [d => d.ApplicationUser, d => d.Department]
            );
        }
    }
}