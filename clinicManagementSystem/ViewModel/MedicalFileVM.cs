using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class MedicalFileVM
    {
        public int MedicalRecordId { get; set; }

        public IFormFile? File { get; set; }

        public DateTime UploadDate { get; set; }
    }
}