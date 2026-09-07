using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;
using PatientModel = clinicManagementSystem.Models.Patient;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area(SD.PATIENT_AREA)]
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<PatientModel> _patientRepository;

        public ReviewsController(
            IRepository<Review> reviewRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<PatientModel> patientRepository)
        {
            _reviewRepository = reviewRepository;
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
        }

        // =========================
        // INDEX (My Reviews)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return View(new List<Review>());
            }

            var reviews = await _reviewRepository.GetAsync(
                expression: r => r.Appointment != null && r.Appointment.PatientId == patient.PatientId,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!,
                    r => r.Appointment.Doctor,
                    r => r.Appointment.Doctor.ApplicationUser,
                    r => r.Appointment.Doctor.Department
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
        public async Task<IActionResult> Create(int? appointmentId)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                TempData["error_notification"] = "Patient profile not found.";
                return RedirectToAction("Index", "Home");
            }

            await LoadEligibleAppointmentsAsync(patient.PatientId, appointmentId);

            var model = new Review
            {
                AppointmentId = appointmentId ?? 0,
                Rating = 5
            };

            return View(model);
        }

        // =========================
        // CREATE - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                TempData["error_notification"] = "Patient profile not found.";
                return RedirectToAction("Index", "Home");
            }

            if (review.AppointmentId <= 0)
            {
                ModelState.AddModelError(nameof(Review.AppointmentId), "Please select an appointment to review.");
            }

            if (review.Rating < 1 || review.Rating > 5)
            {
                ModelState.AddModelError(nameof(Review.Rating), "Rating must be between 1 and 5 stars.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEligibleAppointmentsAsync(patient.PatientId, review.AppointmentId);
                return View(review);
            }

            // Verify the appointment belongs to the logged in patient and is completed
            var appointment = await _appointmentRepository.GetOneAsync(
                expression: a => a.AppointmentId == review.AppointmentId && a.PatientId == patient.PatientId,
                includes: new Expression<Func<Appointment, object>>[] { a => a.Review! }
            );

            if (appointment == null)
            {
                TempData["error_notification"] = "Selected appointment was not found in your records.";
                await LoadEligibleAppointmentsAsync(patient.PatientId, review.AppointmentId);
                return View(review);
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                TempData["error_notification"] = "You can only rate doctors for completed appointments.";
                await LoadEligibleAppointmentsAsync(patient.PatientId, review.AppointmentId);
                return View(review);
            }

            if (appointment.Review != null)
            {
                TempData["error_notification"] = "This appointment already has a review. You can edit your existing review.";
                return RedirectToAction(nameof(Index));
            }

            var newReview = new Review
            {
                AppointmentId = review.AppointmentId,
                Rating = review.Rating,
                Comment = review.Comment?.Trim(),
                ReviewDate = DateTime.Now
            };

            await _reviewRepository.CreateAsync(newReview);
            await _reviewRepository.CommitAsync();

            TempData["success_notification"] = "Thank you! Your review has been submitted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var review = await _reviewRepository.GetOneAsync(
                expression: r => r.ReviewId == id && r.Appointment != null && r.Appointment.PatientId == patient.PatientId,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!,
                    r => r.Appointment.Doctor,
                    r => r.Appointment.Doctor.ApplicationUser
                },
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
        public async Task<IActionResult> Edit(int id, Review review)
        {
            if (id != review.ReviewId)
                return BadRequest();

            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            if (review.Rating < 1 || review.Rating > 5)
            {
                ModelState.AddModelError(nameof(Review.Rating), "Rating must be between 1 and 5 stars.");
            }

            var existingReview = await _reviewRepository.GetOneAsync(
                expression: r => r.ReviewId == id && r.Appointment != null && r.Appointment.PatientId == patient.PatientId,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!,
                    r => r.Appointment.Doctor,
                    r => r.Appointment.Doctor.ApplicationUser
                }
            );

            if (existingReview == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                review.Appointment = existingReview.Appointment;
                return View(review);
            }

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment?.Trim();

            _reviewRepository.Update(existingReview);
            await _reviewRepository.CommitAsync();

            TempData["success_notification"] = "Your review has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var review = await _reviewRepository.GetOneAsync(
                expression: r => r.ReviewId == id && r.Appointment != null && r.Appointment.PatientId == patient.PatientId,
                includes: new Expression<Func<Review, object>>[]
                {
                    r => r.Appointment!,
                    r => r.Appointment.Doctor,
                    r => r.Appointment.Doctor.ApplicationUser
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var review = await _reviewRepository.GetOneAsync(
                expression: r => r.ReviewId == id && r.Appointment != null && r.Appointment.PatientId == patient.PatientId
            );

            if (review == null)
                return NotFound();

            _reviewRepository.Delete(review);
            await _reviewRepository.CommitAsync();

            TempData["success_notification"] = "Review deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private async Task<PatientModel?> GetCurrentPatientAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var patient = await _patientRepository.GetOneAsync(
                expression: p => p.ApplicationUserId == userId,
                includes: new Expression<Func<PatientModel, object>>[] { p => p.ApplicationUser }
            );

            if (patient == null)
            {
                patient = new PatientModel
                {
                    ApplicationUserId = userId,
                   
                };
                await _patientRepository.CreateAsync(patient);
                await _patientRepository.CommitAsync();
            }

            return patient;
        }

        private async Task LoadEligibleAppointmentsAsync(int patientId, int? preselectedAppointmentId)
        {
            var eligibleAppointments = await _appointmentRepository.GetAsync(
                expression: a => a.PatientId == patientId && a.Status == AppointmentStatus.Completed && (a.Review == null || (preselectedAppointmentId.HasValue && a.AppointmentId == preselectedAppointmentId.Value)),
                includes: new Expression<Func<Appointment, object>>[]
                {
                    a => a.Doctor,
                    a => a.Doctor.ApplicationUser,
                    a => a.Doctor.Department,
                    a => a.Review!
                },
                orderBy: q => q.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime),
                tracked: false
            );

            ViewBag.Appointments = eligibleAppointments.ToList();
            ViewBag.SelectedAppointmentId = preselectedAppointmentId;
        }
    }
}