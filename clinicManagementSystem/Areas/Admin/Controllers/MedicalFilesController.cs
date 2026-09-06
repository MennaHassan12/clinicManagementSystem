using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Utilities;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize]
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
        public async Task<IActionResult> Create(MedicalFileVM model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(
                    "File",
                    "Please select a file."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadMedicalRecords(model.MedicalRecordId);
                return View(model);
            }

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(model.File!.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "File",
                    "Only PDF, JPG, JPEG and PNG files are allowed."
                );

                await LoadMedicalRecords(model.MedicalRecordId);
                return View(model);
            }

            const long maxFileSize = 10 * 1024 * 1024;

            if (model.File.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "File",
                    "File size must not exceed 10 MB."
                );

                await LoadMedicalRecords(model.MedicalRecordId);
                return View(model);
            }

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
                    await model.File.CopyToAsync(stream);
                }

                var medicalFile = new MedicalFile
                {
                    MedicalRecordId = model.MedicalRecordId,
                    FileName = Path.GetFileName(model.File.FileName),
                    FilePath = $"/uploads/medical-files/{uniqueFileName}",
                    FileType = extension,
                    UploadDate = model.UploadDate == default
                        ? DateTime.Now
                        : model.UploadDate
                };

                await _medicalFileRepository.CreateAsync(medicalFile);
                await _medicalFileRepository.CommitAsync();

                TempData["Success"] =
                    "Medical file uploaded successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Failed to upload file: {ex.Message}";

                await LoadMedicalRecords(model.MedicalRecordId);

                return View(model);
            }
        }

        // =========================
        // EDIT - GET
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var medicalFile = await _medicalFileRepository.GetOneAsync(
                f => f.MedicalFileId == id,
                tracked: false
            );

            if (medicalFile == null)
            {
                return NotFound();
            }

            await LoadMedicalRecords(medicalFile.MedicalRecordId);

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
            var existingFile = await _medicalFileRepository.GetOneAsync(
                f => f.MedicalFileId == id
            );

            if (existingFile == null)
            {
                return NotFound();
            }

            if (MedicalRecordId <= 0)
            {
                TempData["Error"] = "Please select a medical record.";
                await LoadMedicalRecords(MedicalRecordId);
                return View(existingFile);
            }

            existingFile.MedicalRecordId = MedicalRecordId;
            existingFile.UploadDate = UploadDate == default ? DateTime.Now : UploadDate;

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Only PDF, JPG, JPEG and PNG files are allowed.";
                    await LoadMedicalRecords(MedicalRecordId);
                    return View(existingFile);
                }

                const long maxFileSize = 10 * 1024 * 1024;
                if (uploadedFile.Length > maxFileSize)
                {
                    TempData["Error"] = "File size must not exceed 10 MB.";
                    await LoadMedicalRecords(MedicalRecordId);
                    return View(existingFile);
                }

                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath, "uploads", "medical-files");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var physicalFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                // Delete old physical file
                if (!string.IsNullOrEmpty(existingFile.FilePath))
                {
                    var oldRelativePath = existingFile.FilePath
                        .TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPhysicalPath = Path.Combine(_environment.WebRootPath, oldRelativePath);
                    if (System.IO.File.Exists(oldPhysicalPath))
                        System.IO.File.Delete(oldPhysicalPath);
                }

                existingFile.FileName = Path.GetFileName(uploadedFile.FileName);
                existingFile.FilePath = $"/uploads/medical-files/{uniqueFileName}";
                existingFile.FileType = extension;
            }

            _medicalFileRepository.Update(existingFile);
            await _medicalFileRepository.CommitAsync();

            TempData["Success"] = "Medical file updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE - GET
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var medicalFile = await _medicalFileRepository.GetOneAsync(
                f => f.MedicalFileId == id,
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
            var medicalFile = await _medicalFileRepository.GetOneAsync(
                f => f.MedicalFileId == id
            );

            if (medicalFile == null)
            {
                return NotFound();
            }

            // Delete physical file from disk
            if (!string.IsNullOrEmpty(medicalFile.FilePath))
            {
                var relativePath = medicalFile.FilePath
                    .TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);
            }

            _medicalFileRepository.Delete(medicalFile);
            await _medicalFileRepository.CommitAsync();

            TempData["Success"] = "Medical file deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOAD MEDICAL RECORDS
        // =========================
        private async Task LoadMedicalRecords(int? selectedMedicalRecordId = null)
        {
            var medicalRecords = await _medicalRecordRepository.GetAsync(
                includes: new Expression<Func<MedicalRecord, object>>[]
                {
                    r => r.Appointment
                },
                orderBy: q => q.OrderByDescending(r => r.VisitDate),
                tracked: false
            );

            ViewBag.MedicalRecords = medicalRecords
                .Select(r => new
                {
                    Id = r.MedicalRecordId,
                    Display = $"Record #{r.MedicalRecordId} - Appointment #{r.AppointmentId} - {r.VisitDate:dd/MM/yyyy}"
                })
                .ToList();

            ViewBag.SelectedMedicalRecordId = selectedMedicalRecordId;
        }
    }
}