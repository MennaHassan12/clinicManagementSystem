using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

using PatientModel = clinicManagementSystem.Models.Patient;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class MedicalFilesController : Controller
    {
        private readonly IRepository<MedicalFile> _medicalFileRepository;
        private readonly IRepository<MedicalRecord> _medicalRecordRepository;
        private readonly IRepository<PatientModel> _patientRepository;

        public MedicalFilesController(
            IRepository<MedicalFile> medicalFileRepository,
            IRepository<MedicalRecord> medicalRecordRepository,
            IRepository<PatientModel> patientRepository)
        {
            _medicalFileRepository = medicalFileRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _patientRepository = patientRepository;
        }
        // =========================
        // INDEX
        // =========================

        [HttpGet]
        public async Task<IActionResult> Index(int? patientId)
        {
            // نفس طريقة المشروع الحالية في Patient Area
            int targetPatientId =
                patientId.HasValue && patientId.Value > 0
                    ? patientId.Value
                    : 1;

            ViewBag.PatientId = targetPatientId;

            var patient = await _patientRepository.GetOneAsync(
                p => p.PatientId == targetPatientId,
                tracked: false
            );

            if (patient == null)
            {
                return NotFound();
            }

            // Get only files that belong to this patient
            var medicalFiles = await _medicalFileRepository.GetAsync(
                expression: f =>
                    f.MedicalRecord.Appointment.PatientId
                    == targetPatientId,

                includes:
                    new Expression<Func<MedicalFile, object>>[]
                    {
                        f => f.MedicalRecord
                    },

                orderBy:
                    q => q.OrderByDescending(f => f.UploadDate),

                tracked: false
            );

            return View(medicalFiles);
        }

        // =========================
        // DOWNLOAD / VIEW FILE
        // =========================

        [HttpGet]
        public async Task<IActionResult> Download(
            int id,
            int? patientId)
        {
            int targetPatientId =
                patientId.HasValue && patientId.Value > 0
                    ? patientId.Value
                    : 1;

            var medicalFile = await _medicalFileRepository.GetOneAsync(
                f =>
                    f.MedicalFileId == id &&
                    f.MedicalRecord.Appointment.PatientId
                    == targetPatientId,

                tracked: false
            );

            // لو الملف مش موجود أو مش بتاع الـ Patient
            if (medicalFile == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                medicalFile.FilePath.TrimStart('/')
            );

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(
                filePath
            );

            return File(
                fileBytes,
                GetContentType(medicalFile.FileType),
                medicalFile.FileName
            );
        }

        // =========================
        // FILE CONTENT TYPE
        // =========================

        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}