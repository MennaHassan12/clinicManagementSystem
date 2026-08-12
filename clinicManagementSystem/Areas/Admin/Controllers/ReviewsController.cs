using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
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

        // GET: Admin/Reviews
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewRepository.GetAsync(
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment
                },
                orderBy: q => q.OrderByDescending(r => r.ReviewDate),
                tracked: false
            );

            return View(reviews);
        }

        // GET: Admin/Reviews/Create
        public async Task<IActionResult> Create()
        {
            await LoadAvailableAppointments();

            return View();
        }

        // POST: Admin/Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            var existingReview = await _reviewRepository.GetOneAsync(
                r => r.AppointmentId == review.AppointmentId
            );

            if (existingReview != null)
            {
                ModelState.AddModelError(
                    "AppointmentId",
                    "This appointment already has a review."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableAppointments();
                return View(review);
            }

            review.ReviewDate = DateTime.Now;

            await _reviewRepository.CreateAsync(review);
            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Reviews/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id,
                tracked: false
            );

            if (review == null)
            {
                return NotFound();
            }

            await LoadAvailableAppointments(review.AppointmentId);

            return View(review);
        }

        // POST: Admin/Reviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review review)
        {
            if (id != review.ReviewId)
            {
                return NotFound();
            }

            // Check if another review already exists for this appointment
            var existingReview = await _reviewRepository.GetOneAsync(
                r => r.AppointmentId == review.AppointmentId
                     && r.ReviewId != review.ReviewId
            );

            if (existingReview != null)
            {
                ModelState.AddModelError(
                    "AppointmentId",
                    "This appointment already has a review."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableAppointments(review.AppointmentId);

                return View(review);
            }

            _reviewRepository.Update(review);
            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Reviews/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment
                },
                tracked: false
            );

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Admin/Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _reviewRepository.GetOneAsync(
                r => r.ReviewId == id
            );

            if (review == null)
            {
                return NotFound();
            }

            _reviewRepository.Delete(review);
            await _reviewRepository.CommitAsync();

            TempData["Success"] = "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Load appointments that don't already have reviews
        private async Task LoadAvailableAppointments(
            int? currentAppointmentId = null)
        {
            var appointments = await _appointmentRepository.GetAsync(
                includes: new Expression<Func<Appointment, object>>[]
                {
                    a => a.Doctor,
                    a => a.Patient
                },
                orderBy: q => q.OrderByDescending(a => a.AppointmentDate),
                tracked: false
            );

            var existingReviews = await _reviewRepository.GetAsync(
                tracked: false
            );

            var reviewedAppointmentIds = existingReviews
                .Select(r => r.AppointmentId)
                .ToHashSet();

            var availableAppointments = appointments
                .Where(a =>
                    !reviewedAppointmentIds.Contains(a.AppointmentId)
                    || a.AppointmentId == currentAppointmentId
                )
                .ToList();

            ViewBag.Appointments = availableAppointments;
        }
    }
}