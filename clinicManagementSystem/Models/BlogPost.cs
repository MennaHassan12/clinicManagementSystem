using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class BlogPost
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public string BadgeClass { get; set; } = "bg-primary-subtle text-primary";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Author name is required")]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [StringLength(100)]
        public string Role { get; set; } = "Medical Specialist";

        public string? Image { get; set; }

        public string ReadTime { get; set; } = "5 min read";

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;
    }
}