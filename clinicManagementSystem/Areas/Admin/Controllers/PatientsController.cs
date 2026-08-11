using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using PatientModel = clinicManagementSystem.Models.Patient;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PatientsController : Controller
    {
        private readonly IRepository<PatientModel> _patientRepo;
        private readonly IRepository<ApplicationUser> _userRepo;

        public PatientsController(
            IRepository<PatientModel> patientRepo,
            IRepository<ApplicationUser> userRepo)
        {
            _patientRepo = patientRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index(string? searchName)
        {
            var patients = await _patientRepo.GetAsync(includes: [p => p.ApplicationUser]);

            if (!string.IsNullOrEmpty(searchName))
            {
                patients = patients.Where(p => p.ApplicationUser != null &&
                    p.ApplicationUser.FullName.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(patients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.PatientId == id,
                includes: [p => p.ApplicationUser]
            );

            if (patient == null) return NotFound();

            return View(patient);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientFormVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Phone ?? Guid.NewGuid().ToString(),
                        FullName = model.Name,
                        PhoneNumber = model.Phone
                    };

                    await _userRepo.CreateAsync(user);
                    await _userRepo.CommitAsync();

                    var patient = new PatientModel
                    {
                        ApplicationUserId = user.Id,
                        BirthDate = model.BirthDate,
                        Gender = model.Gender,
                        BloodType = model.BloodType ?? "N/A",
                        Address = model.Address ?? "N/A",
                        EmergencyContactName = model.EmergencyContactName,
                        EmergencyContactPhone = model.EmergencyContactPhone,
                        EmergencyContactRelation = model.EmergencyContactRelation
                    };

                    await _patientRepo.CreateAsync(patient);
                    await _patientRepo.CommitAsync();

                    TempData["success_notification"] = "Patient created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                    TempData["error_notification"] = "Failed to create patient. Check inputs!";
                }
            }
            else
            {
                TempData["error_notification"] = "Please fill all required fields correctly.";
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.PatientId == id,
                includes: [p => p.ApplicationUser]
            );
            if (patient == null) return NotFound();

            var viewModel = new PatientFormVM
            {
                PatientId = patient.PatientId,
                Name = patient.ApplicationUser?.FullName,
                Phone = patient.ApplicationUser?.PhoneNumber,
                BirthDate = patient.BirthDate,
                Gender = patient.Gender,
                BloodType = patient.BloodType,
                Address = patient.Address,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelation = patient.EmergencyContactRelation
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PatientFormVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _patientRepo.GetOneAsync(
                        expression: p => p.PatientId == model.PatientId,
                        includes: [p => p.ApplicationUser]
                    );

                    if (patient == null) return NotFound();

                    if (patient.ApplicationUser != null)
                    {
                        patient.ApplicationUser.FullName = model.Name;
                        patient.ApplicationUser.PhoneNumber = model.Phone;
                        _userRepo.Update(patient.ApplicationUser);
                        await _userRepo.CommitAsync();
                    }

                    patient.BirthDate = model.BirthDate;
                    patient.Gender = model.Gender;
                    patient.BloodType = model.BloodType ?? "N/A";
                    patient.Address = model.Address ?? "N/A";
                    patient.EmergencyContactName = model.EmergencyContactName;
                    patient.EmergencyContactPhone = model.EmergencyContactPhone;
                    patient.EmergencyContactRelation = model.EmergencyContactRelation;

                    _patientRepo.Update(patient);
                    await _patientRepo.CommitAsync();

                    TempData["success_notification"] = "Patient updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                    TempData["error_notification"] = "Error occurred while updating patient!";
                }
            }
            else
            {
                TempData["error_notification"] = "Validation failed. Please check inputs.";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var patient = await _patientRepo.GetOneAsync(expression: p => p.PatientId == id);
                if (patient != null)
                {
                    _patientRepo.Delete(patient);
                    await _patientRepo.CommitAsync();

                    TempData["success_notification"] = "Patient deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Patient not found!";
                }
            }
            catch (Exception)
            {
                TempData["error_notification"] = "Cannot delete patient as they have associated records.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
