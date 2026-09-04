using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Policy = "RequirePatient")]
    public class ReviewsController : Controller
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Appointment> _appointmentRepository;

        public ReviewsController(
            IRepository<Review> reviewRepository,
            IRepository<Appointment> appointmentRepository)
        {
            _reviewRepository = reviewRepository;
            _appointmentRepository = appointmentRepository;
        }

        // =========================
        // INDEX
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewRepository.GetAsync(
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!
                },
                orderBy: q => q.OrderByDescending(r => r.ReviewDate),
                tracked: false
            );

            return View(reviews);
        }

        // =========================
        // CREATE - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadAppointments();
            return View();
        }

        // =========================
        // CREATE - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int AppointmentId,
            int Rating,
            string? Comment)
        {
            if (AppointmentId <= 0)
            {
                TempData["Error"] = "Please select an appointment.";
                await LoadAppointments();
                return View();
            }

            if (Rating < 1 || Rating > 5)
            {
                TempData["Error"] = "Please select a rating.";
                await LoadAppointments();
                return View();
            }

            // Prevent duplicate review for the same appointment
            var existingReview = await _reviewRepository.GetOneAsync(
                r => r.AppointmentId == AppointmentId,
                tracked: false
            );

            if (existingReview != null)
            {
                TempData["Error"] =
                    "This appointment already has a review. Please edit the existing review.";

                await LoadAppointments();
                return View();
            }

            var review = new Review
            {
                AppointmentId = AppointmentId,
                Rating = Rating,
                Comment = Comment,
                ReviewDate = DateTime.Now
            };

            await _reviewRepository.CreateAsync(review);
            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id,
                tracked: false
            );

            if (review == null)
                return NotFound();

            return View(review);
        }

        // =========================
        // EDIT - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int ReviewId,
            int AppointmentId,
            int Rating,
            string? Comment)
        {
            if (id != ReviewId)
                return BadRequest();

            if (Rating < 1 || Rating > 5)
            {
                TempData["Error"] = "Please select a valid rating.";

                var invalidReview = new Review
                {
                    ReviewId = ReviewId,
                    AppointmentId = AppointmentId,
                    Rating = Rating,
                    Comment = Comment
                };

                return View(invalidReview);
            }

            var existingReview = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id
            );

            if (existingReview == null)
                return NotFound();

            existingReview.Rating = Rating;
            existingReview.Comment = Comment;

            _reviewRepository.Update(existingReview);

            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!
                },
                tracked: false
            );

            if (review == null)
                return NotFound();

            return View(review);
        }

        // =========================
        // DELETE - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id
            );

            if (review == null)
                return NotFound();

            _reviewRepository.Delete(review);

            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOAD APPOINTMENTS
        // =========================
        private async Task LoadAppointments()
        {
            var appointments = await _appointmentRepository.GetAsync(
                orderBy: q => q.OrderByDescending(a => a.AppointmentDate),
                tracked: false
            );

            ViewBag.Appointments = appointments;
        }
    }
}