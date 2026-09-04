using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class BlogController : Controller
    {
        private readonly IRepository<BlogPost> _blogRepo;

        public BlogController(IRepository<BlogPost> blogRepo)
        {
            _blogRepo = blogRepo;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? category, int page = 1)
        {
            int pageSize = 6;

            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentCategory = category;

            var allBlogs = await _blogRepo.GetAsync();
            ViewBag.Categories = allBlogs.Select(b => b.Category).Distinct().ToList();

            searchTerm = searchTerm?.Trim();

            var filteredBlogs = await _blogRepo.GetAsync(
                expression: b => (string.IsNullOrEmpty(searchTerm) || b.Title.Contains(searchTerm) || b.Content.Contains(searchTerm)) &&
                                 (string.IsNullOrEmpty(category) || b.Category == category)
            );

            int totalItems = filteredBlogs.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paginatedBlogs = filteredBlogs
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(paginatedBlogs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var blog = await _blogRepo.GetOneAsync(expression: b => b.Id == id);

            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }
    }
}