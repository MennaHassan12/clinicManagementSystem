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
    public class MedicalFilesController : Controller
    {
        private readonly IRepository<MedicalFile> _medicalFileRepository;
        private readonly IRepository<PatientModel> _patientRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MedicalFilesController(
            IRepository<MedicalFile> medicalFileRepository,
            IRepository<PatientModel> patientRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _medicalFileRepository = medicalFileRepository;
            _patientRepository = patientRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // =========================
        // INDEX (My Medical Files)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return View(new List<MedicalFile>());
            }

            var medicalFiles = await _medicalFileRepository.GetAsync(
                expression: f => f.MedicalRecord != null &&
                                 f.MedicalRecord.Appointment != null &&
                                 f.MedicalRecord.Appointment.PatientId == patient.PatientId,
                includes: new Expression<Func<MedicalFile, object>>[]
                {
                    f => f.MedicalRecord!,
                    f => f.MedicalRecord!.Appointment!,
                    f => f.MedicalRecord!.Appointment!.Doctor!,
                    f => f.MedicalRecord!.Appointment!.Doctor!.ApplicationUser!,
                    f => f.MedicalRecord!.Appointment!.Doctor!.Department!
                },
                orderBy: q => q.OrderByDescending(f => f.UploadDate),
                tracked: false
            );

            return View(medicalFiles);
        }

        // =========================
        // DOWNLOAD / VIEW FILE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return NotFound("Patient not found.");
            }

            var medicalFile = await _medicalFileRepository.GetOneAsync(
                expression: f => f.MedicalFileId == id &&
                                 f.MedicalRecord != null &&
                                 f.MedicalRecord.Appointment != null &&
                                 f.MedicalRecord.Appointment.PatientId == patient.PatientId,
                tracked: false
            );

            if (medicalFile == null)
            {
                return NotFound("Medical file not found or unauthorized access.");
            }

            var filePath = Path.Combine(
                _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                medicalFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
            );

            if (!System.IO.File.Exists(filePath))
            {
                TempData["error_notification"] = "File could not be found on server.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(medicalFile.FileType ?? Path.GetExtension(medicalFile.FileName));

            return File(fileBytes, contentType, medicalFile.FileName);
        }

        // =========================
        // HELPERS
        // =========================
        private async Task<PatientModel?> GetCurrentPatientAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _patientRepository.GetOneAsync(
                expression: p => p.ApplicationUserId == userId,
                includes: new Expression<Func<PatientModel, object>>[] { p => p.ApplicationUser }
            );
        }

        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}