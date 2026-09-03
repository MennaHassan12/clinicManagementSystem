using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using DoctorModel = clinicManagementSystem.Models.Doctor;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogController : Controller
    {
        private readonly IRepository<BlogPost> _blogRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BlogController(
            IRepository<BlogPost> blogRepo,
            IRepository<DoctorModel> doctorRepo,
            IWebHostEnvironment webHostEnvironment)
        {
            _blogRepo = blogRepo;
            _doctorRepo = doctorRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var blogs = await _blogRepo.GetAsync();
            return View(blogs);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new BlogPostCreateVM
            {
                DoctorsList = await GetDoctorsSelectListAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostCreateVM model)
        {
            ModelState.Remove("DoctorsList");

            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Please upload an article banner image.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string imagePath = SaveImageFile(model.ImageFile!);

                    var blogPost = new BlogPost
                    {
                        Title = model.Title,
                        Category = model.Category,
                        BadgeClass = "bg-primary",
                        Author = model.Author,
                        Role = "Doctor",
                        ReadTime = "5 min read",
                        Content = model.Content,
                        CreatedDate = DateTime.Now,
                        Image = imagePath
                    };

                    await _blogRepo.CreateAsync(blogPost);
                    await _blogRepo.CommitAsync();

                    TempData["success_notification"] = "Article created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["error_notification"] = "Failed to create article. Please try again.";
                }
            }
            else
            {
                TempData["error_notification"] = "Please fill all required fields correctly.";
            }

            model.DoctorsList = await GetDoctorsSelectListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogRepo.GetOneAsync(expression: b => b.Id == id);
            if (blog == null) return NotFound();

            var viewModel = new BlogPostCreateVM
            {
                Id = blog.Id,
                Title = blog.Title,
                Category = blog.Category,
                Author = blog.Author,
                Content = blog.Content,
                ExistingImagePath = blog.Image,
                DoctorsList = await GetDoctorsSelectListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogPostCreateVM model)
        {
            ModelState.Remove("DoctorsList");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    var blog = await _blogRepo.GetOneAsync(expression: b => b.Id == model.Id);
                    if (blog == null) return NotFound();

                    blog.Title = model.Title;
                    blog.Category = model.Category;
                    blog.Author = model.Author;
                    blog.Content = model.Content;

                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(blog.Image))
                        {
                            DeleteImageFile(blog.Image);
                        }
                        blog.Image = SaveImageFile(model.ImageFile);
                    }

                    _blogRepo.Update(blog);
                    await _blogRepo.CommitAsync();

                    TempData["success_notification"] = "Article updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["error_notification"] = "Failed to update article!";
                }
            }
            else
            {
                TempData["error_notification"] = "Validation failed. Please check inputs.";
            }

            model.DoctorsList = await GetDoctorsSelectListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var blog = await _blogRepo.GetOneAsync(expression: b => b.Id == id);
                if (blog != null)
                {
                    if (!string.IsNullOrEmpty(blog.Image))
                    {
                        DeleteImageFile(blog.Image);
                    }

                    _blogRepo.Delete(blog);
                    await _blogRepo.CommitAsync();

                    TempData["success_notification"] = "Article deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Article not found!";
                }
            }
            catch (Exception)
            {
                TempData["error_notification"] = "Cannot delete article.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetDoctorsSelectListAsync()
        {
            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            return doctors
                .Where(d => d.ApplicationUser != null)
                .Select(d => new SelectListItem
                {
                    Value = "Dr. " + d.ApplicationUser!.FullName,
                    Text = "Dr. " + d.ApplicationUser.FullName
                })
                .ToList();
        }

        private string SaveImageFile(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "blogs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return "/images/blogs/" + uniqueFileName;
        }

        private void DeleteImageFile(string imagePath)
        {
            string relativePath = imagePath.TrimStart('/');
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}