using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModel
{
    public class DoctorFormVM
    {
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be at least 3 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "License number must be a positive number.")]
        public int LicenseNumber { get; set; }

        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "Consultation fee is required.")]
        [Range(0, 100000, ErrorMessage = "Consultation fee cannot be negative.")]
        public decimal ConsultationFee { get; set; }

        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0, 70, ErrorMessage = "Years of experience cannot be negative.")]
        public int YearsOfExperience { get; set; }

        [MaxLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
        public string? Bio { get; set; }

        public IFormFile? Photo { get; set; }
        public string? ExistingImagePath { get; set; }
        public IEnumerable<SelectListItem>? Departments { get; set; }
    }
}