using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
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
    }
}