using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Utilities;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area(SD.DOCTOR_AREA)]
    [Authorize]
    public class MedicalFilesController : Controller
    {
        private readonly IRepository<MedicalFile> _medicalFileRepository;
        private readonly IRepository<MedicalRecord> _medicalRecordRepository;
        private readonly IRepository<Models.Doctor> _doctorRepository;
        private readonly IWebHostEnvironment _environment;

        public MedicalFilesController(
            IRepository<MedicalFile> medicalFileRepository,
            IRepository<MedicalRecord> medicalRecordRepository,
            IRepository<Models.Doctor> doctorRepository,
            IWebHostEnvironment environment)
        {
            _medicalFileRepository = medicalFileRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _doctorRepository = doctorRepository;
            _environment = environment;
        }

        // =========================
        // GET CURRENT DOCTOR
        // =========================

        private async Task<Models.Doctor?> GetCurrentDoctorAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _doctorRepository.GetOneAsync(
                d => d.ApplicationUserId == userId,
                tracked: false
            );
        }

        // =========================
        // INDEX
        // =========================

        public async Task<IActionResult> Index()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                TempData["Error"] = "Doctor profile not found.";
                return View(new List<MedicalFile>());
            }

            // Only show medical files linked to THIS doctor's medical records / appointments
            var medicalFiles = await _medicalFileRepository.GetAsync(
                expression: f =>
                    f.MedicalRecord != null &&
                    f.MedicalRecord.Appointment != null &&
                    f.MedicalRecord.Appointment.DoctorId == doctor.DoctorId,
                includes: new Expression<Func<MedicalFile, object>>[]
                {
                    f => f.MedicalRecord,
                    f => f.MedicalRecord.Appointment
                },
                orderBy: q => q.OrderByDescending(f => f.UploadDate),
                tracked: false
            );

            return View(medicalFiles);
        }

        // =========================
        // CREATE - GET
        // =========================

        public async Task<IActionResult> Create()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            await LoadMedicalRecords(doctor.DoctorId);

            return View(new MedicalFileVM
            {
                UploadDate = DateTime.Now
            });
        }

        // =========================
        // CREATE - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            DateTime UploadDate,
            IFormFile? uploadedFile)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            // Get MedicalRecordId directly from Form
            var medicalRecordIdValue =
                Request.Form["MedicalRecordId"].ToString();

            if (!int.TryParse(
                    medicalRecordIdValue,
                    out int medicalRecordId))
            {
                medicalRecordId = 0;
            }

            // =========================
            // CHECK MEDICAL RECORD
            // =========================

            if (medicalRecordId <= 0)
            {
                TempData["Error"] =
                    "Please select a medical record.";

                await LoadMedicalRecords(doctor.DoctorId, medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }

            // Verify this medical record belongs to this doctor
            var medicalRecord = await _medicalRecordRepository.GetOneAsync(
                r => r.MedicalRecordId == medicalRecordId &&
                     r.Appointment != null &&
                     r.Appointment.DoctorId == doctor.DoctorId,
                tracked: false
            );

            if (medicalRecord == null)
            {
                TempData["Error"] =
                    "Invalid medical record or you don't have permission.";

                await LoadMedicalRecords(doctor.DoctorId);
                return View(new MedicalFileVM { UploadDate = UploadDate });
            }

            // =========================
            // CHECK FILE
            // =========================

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["Error"] =
                    "Please select a file.";

                await LoadMedicalRecords(doctor.DoctorId, medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }

            // =========================
            // CHECK EXTENSION
            // =========================

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(uploadedFile.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] =
                    "Only PDF, JPG, JPEG and PNG files are allowed.";

                await LoadMedicalRecords(doctor.DoctorId, medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }

            // =========================
            // CHECK FILE SIZE
            // =========================

            const long maxFileSize = 10 * 1024 * 1024;

            if (uploadedFile.Length > maxFileSize)
            {
                TempData["Error"] =
                    "File size must not exceed 10 MB.";

                await LoadMedicalRecords(doctor.DoctorId, medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }

            // =========================
            // SAVE FILE
            // =========================

            try
            {
                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "medical-files"
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName =
                    $"{Guid.NewGuid()}{extension}";

                var physicalFilePath = Path.Combine(
                    uploadsFolder,
                    uniqueFileName
                );

                using (var stream = new FileStream(
                    physicalFilePath,
                    FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                // =========================
                // CREATE MEDICAL FILE
                // =========================

                var medicalFile = new MedicalFile
                {
                    MedicalRecordId = medicalRecordId,

                    FileName =
                        Path.GetFileName(uploadedFile.FileName),

                    FilePath =
                        $"/uploads/medical-files/{uniqueFileName}",

                    FileType = extension,

                    UploadDate =
                        UploadDate == default
                            ? DateTime.Now
                            : UploadDate
                };

                await _medicalFileRepository
                    .CreateAsync(medicalFile);

                await _medicalFileRepository
                    .CommitAsync();

                TempData["Success"] =
                    "Medical file uploaded successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Failed to upload medical file: {ex.Message}";

                await LoadMedicalRecords(doctor.DoctorId, medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }
        }

        // =========================
        // EDIT - GET
        // =========================

        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            var medicalFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id &&
                         f.MedicalRecord != null &&
                         f.MedicalRecord.Appointment != null &&
                         f.MedicalRecord.Appointment.DoctorId == doctor.DoctorId,
                    tracked: false
                );

            if (medicalFile == null)
            {
                return NotFound();
            }

            await LoadMedicalRecords(
                doctor.DoctorId,
                medicalFile.MedicalRecordId);

            return View(medicalFile);
        }

        // =========================
        // EDIT - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int MedicalRecordId,
            DateTime UploadDate,
            IFormFile? uploadedFile)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            // =========================
            // GET EXISTING FILE (doctor-scoped)
            // =========================

            var existingFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id &&
                         f.MedicalRecord != null &&
                         f.MedicalRecord.Appointment != null &&
                         f.MedicalRecord.Appointment.DoctorId == doctor.DoctorId
                );

            if (existingFile == null)
            {
                return NotFound();
            }

            // =========================
            // CHECK MEDICAL RECORD
            // =========================

            if (MedicalRecordId <= 0)
            {
                TempData["Error"] =
                    "Please select a medical record.";

                await LoadMedicalRecords(doctor.DoctorId, MedicalRecordId);

                return View(existingFile);
            }

            // Verify new medical record also belongs to this doctor
            var medicalRecord = await _medicalRecordRepository.GetOneAsync(
                r => r.MedicalRecordId == MedicalRecordId &&
                     r.Appointment != null &&
                     r.Appointment.DoctorId == doctor.DoctorId,
                tracked: false
            );

            if (medicalRecord == null)
            {
                TempData["Error"] =
                    "Invalid medical record or you don't have permission.";

                await LoadMedicalRecords(doctor.DoctorId, existingFile.MedicalRecordId);
                return View(existingFile);
            }

            // =========================
            // UPDATE MEDICAL RECORD
            // =========================

            existingFile.MedicalRecordId =
                MedicalRecordId;

            // =========================
            // UPDATE DATE
            // =========================

            existingFile.UploadDate =
                UploadDate == default
                    ? DateTime.Now
                    : UploadDate;

            // =========================
            // REPLACE FILE
            // =========================

            if (uploadedFile != null &&
                uploadedFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".pdf",
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                var extension =
                    Path.GetExtension(
                        uploadedFile.FileName)
                    .ToLowerInvariant();

                // Check extension
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] =
                        "Only PDF, JPG, JPEG and PNG files are allowed.";

                    await LoadMedicalRecords(doctor.DoctorId, MedicalRecordId);

                    return View(existingFile);
                }

                // Check size
                const long maxFileSize =
                    10 * 1024 * 1024;

                if (uploadedFile.Length > maxFileSize)
                {
                    TempData["Error"] =
                        "File size must not exceed 10 MB.";

                    await LoadMedicalRecords(doctor.DoctorId, MedicalRecordId);

                    return View(existingFile);
                }

                // =========================
                // UPLOADS FOLDER
                // =========================

                var uploadsFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "medical-files"
                    );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                // =========================
                // NEW FILE NAME
                // =========================

                var uniqueFileName =
                    $"{Guid.NewGuid()}{extension}";

                var physicalFilePath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName
                    );

                // =========================
                // SAVE NEW FILE
                // =========================

                using (var stream =
                    new FileStream(
                        physicalFilePath,
                        FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                // =========================
                // DELETE OLD FILE
                // =========================

                if (!string.IsNullOrEmpty(
                    existingFile.FilePath))
                {
                    var oldRelativePath =
                        existingFile.FilePath
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar);

                    var oldPhysicalPath =
                        Path.Combine(
                            _environment.WebRootPath,
                            oldRelativePath
                        );

                    if (System.IO.File.Exists(
                        oldPhysicalPath))
                    {
                        System.IO.File.Delete(
                            oldPhysicalPath);
                    }
                }

                // =========================
                // UPDATE FILE DATA
                // =========================

                existingFile.FileName =
                    Path.GetFileName(
                        uploadedFile.FileName);

                existingFile.FilePath =
                    $"/uploads/medical-files/{uniqueFileName}";

                existingFile.FileType =
                    extension;
            }

            // =========================
            // SAVE DATABASE CHANGES
            // =========================

            _medicalFileRepository.Update(
                existingFile);

            await _medicalFileRepository.CommitAsync();

            TempData["Success"] =
                "Medical file updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE - GET
        // =========================

        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            var medicalFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id &&
                         f.MedicalRecord != null &&
                         f.MedicalRecord.Appointment != null &&
                         f.MedicalRecord.Appointment.DoctorId == doctor.DoctorId,
                    includes: new Expression<Func<MedicalFile, object>>[]
                    {
                        f => f.MedicalRecord
                    },
                    tracked: false
                );

            if (medicalFile == null)
            {
                return NotFound();
            }

            return View(medicalFile);
        }

        // =========================
        // DELETE - POST
        // =========================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound("Doctor profile not found.");

            var medicalFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id &&
                         f.MedicalRecord != null &&
                         f.MedicalRecord.Appointment != null &&
                         f.MedicalRecord.Appointment.DoctorId == doctor.DoctorId
                );

            if (medicalFile == null)
            {
                return NotFound();
            }

            // =========================
            // DELETE PHYSICAL FILE
            // =========================

            if (!string.IsNullOrEmpty(medicalFile.FilePath))
            {
                var relativePath =
                    medicalFile.FilePath
                        .TrimStart('/')
                        .Replace('/', Path.DirectorySeparatorChar);

                var physicalPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        relativePath
                    );

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            _medicalFileRepository.Delete(medicalFile);
            await _medicalFileRepository.CommitAsync();

            TempData["Success"] = "Medical file deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOAD MEDICAL RECORDS (doctor-scoped)
        // =========================

        private async Task LoadMedicalRecords(
            int doctorId,
            int? selectedMedicalRecordId = null)
        {
            var medicalRecords =
                await _medicalRecordRepository.GetAsync(
                    expression: r =>
                        r.Appointment != null &&
                        r.Appointment.DoctorId == doctorId,
                    includes:
                        new Expression<Func<MedicalRecord, object>>[]
                        {
                            r => r.Appointment
                        },
                    orderBy: q =>
                        q.OrderByDescending(
                            r => r.VisitDate),
                    tracked: false
                );

            ViewBag.MedicalRecords =
                medicalRecords
                    .Select(r => new
                    {
                        Id = r.MedicalRecordId,

                        Display =
                            $"Record #{r.MedicalRecordId} - Appointment #{r.AppointmentId} - {r.VisitDate:dd/MM/yyyy}"
                    })
                    .ToList();

            ViewBag.SelectedMedicalRecordId =
                selectedMedicalRecordId;
        }
    }
}