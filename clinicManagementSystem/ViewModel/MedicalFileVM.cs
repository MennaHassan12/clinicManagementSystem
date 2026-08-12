using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class MedicalFileVM
    {
        public int MedicalFileId { get; set; }

        [Required]
        public int MedicalRecordId { get; set; }

        [Required(ErrorMessage = "Please select a file.")]
        public IFormFile? File { get; set; }

        public string? ExistingFilePath { get; set; }

        public string? ExistingFileName { get; set; }

        public DateTime UploadDate { get; set; }
    }
}