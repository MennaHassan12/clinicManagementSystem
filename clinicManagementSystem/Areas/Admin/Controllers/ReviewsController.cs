using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class ReviewsController : Controller
    {
        private readonly IRepository<Review> _reviewRepository;

        public ReviewsController(IRepository<Review> reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

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

        [HttpPost]
        [ActionName("Delete")]
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
    }
}