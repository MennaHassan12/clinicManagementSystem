using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class BlogPostCreateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an author.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        public List<SelectListItem> DoctorsList { get; set; } = new List<SelectListItem>();
    }
}