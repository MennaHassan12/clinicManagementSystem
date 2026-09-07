using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;
using DoctorModel = clinicManagementSystem.Models.Doctor;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area(SD.DOCTOR_AREA)]
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<DoctorModel> _doctorRepository;

        public ReviewsController(
            IRepository<Review> reviewRepository,
            IRepository<DoctorModel> doctorRepository)
        {
            _reviewRepository = reviewRepository;
            _doctorRepository = doctorRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DoctorModel? doctor = null;

            if (!string.IsNullOrEmpty(userId))
            {
                doctor = await _doctorRepository.GetOneAsync(
                    expression: d => d.ApplicationUserId == userId
                );
            }

            // Fallback for testing: if current user is not a doctor, load the first available doctor
            if (doctor == null)
            {
                doctor = await _doctorRepository.GetOneAsync(tracked: false);
            }

            if (doctor == null)
            {
                ViewBag.TotalReviews = 0;
                ViewBag.AverageRating = 0.0;
                return View(new List<Review>());
            }

            var reviews = await _reviewRepository.GetAsync(
                expression: r => r.Appointment != null && r.Appointment.DoctorId == doctor.DoctorId,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!,
                    r => r.Appointment.Patient,
                    r => r.Appointment.Patient.ApplicationUser
                },
                orderBy: q => q.OrderByDescending(r => r.ReviewDate),
                tracked: false
            );

            var reviewsList = reviews?.ToList() ?? new List<Review>();

            ViewBag.TotalReviews = reviewsList.Count;
            ViewBag.AverageRating = reviewsList.Any() ? Math.Round(reviewsList.Average(r => r.Rating), 1) : 0.0;
            ViewBag.FiveStarCount = reviewsList.Count(r => r.Rating == 5);
            ViewBag.FourStarCount = reviewsList.Count(r => r.Rating == 4);
            ViewBag.ThreeStarCount = reviewsList.Count(r => r.Rating == 3);
            ViewBag.TwoStarCount = reviewsList.Count(r => r.Rating == 2);
            ViewBag.OneStarCount = reviewsList.Count(r => r.Rating == 1);

            return View(reviewsList);
        }
    }
}