using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class MedicalFilesController : Controller
    {
        private readonly IRepository<MedicalFile> _medicalFileRepository;
        private readonly IRepository<MedicalRecord> _medicalRecordRepository;
        private readonly IWebHostEnvironment _environment;

        public MedicalFilesController(
            IRepository<MedicalFile> medicalFileRepository,
            IRepository<MedicalRecord> medicalRecordRepository,
            IWebHostEnvironment environment)
        {
            _medicalFileRepository = medicalFileRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _environment = environment;
        }

        // =========================
        // INDEX
        // =========================

        public async Task<IActionResult> Index()
        {
            var medicalFiles = await _medicalFileRepository.GetAsync(
                includes: new Expression<Func<MedicalFile, object>>[]
                {
                    f => f.MedicalRecord
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
            await LoadMedicalRecords();

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

                await LoadMedicalRecords(medicalRecordId);

                return View(new MedicalFileVM
                {
                    MedicalRecordId = medicalRecordId,
                    UploadDate = UploadDate
                });
            }

            // =========================
            // CHECK FILE
            // =========================

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["Error"] =
                    "Please select a file.";

                await LoadMedicalRecords(medicalRecordId);

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

                await LoadMedicalRecords(medicalRecordId);

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

                await LoadMedicalRecords(medicalRecordId);

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

                await LoadMedicalRecords(medicalRecordId);

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
            var medicalFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id,
                    tracked: false
                );

            if (medicalFile == null)
            {
                return NotFound();
            }

            await LoadMedicalRecords(
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
            // =========================
            // GET EXISTING FILE
            // =========================

            var existingFile =
                await _medicalFileRepository.GetOneAsync(
                    f => f.MedicalFileId == id
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

                await LoadMedicalRecords(MedicalRecordId);

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

                    await LoadMedicalRecords(MedicalRecordId);

                    return View(existingFile);
                }

                // Check size
                const long maxFileSize =
                    10 * 1024 * 1024;

                if (uploadedFile.Length > maxFileSize)
                {
                    TempData["Error"] =
                        "File size must not exceed 10 MB.";

                    await LoadMedicalRecords(MedicalRecordId);

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
        // LOAD MEDICAL RECORDS
        // =========================

        private async Task LoadMedicalRecords(
            int? selectedMedicalRecordId = null)
        {
            var medicalRecords =
                await _medicalRecordRepository.GetAsync(
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