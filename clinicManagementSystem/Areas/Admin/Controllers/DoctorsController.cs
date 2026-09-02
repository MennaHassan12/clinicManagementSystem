using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using DoctorModel = clinicManagementSystem.Models.Doctor;
using Microsoft.AspNetCore.Authorization;
using clinicManagementSystem.Utilities;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class DoctorsController : Controller
    {
        private readonly IRepository<DoctorModel> _doctorRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DoctorsController(
            IRepository<DoctorModel> doctorRepo,
            IRepository<Department> departmentRepo,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IWebHostEnvironment webHostEnvironment)
        {
            _doctorRepo = doctorRepo;
            _departmentRepo = departmentRepo;
            _userManager = userManager;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? searchName)
        {
            var doctors = await _doctorRepo.GetAsync(
                includes: [d => d.Department, d => d.ApplicationUser]
            );

            if (!string.IsNullOrEmpty(searchName))
            {
                doctors = doctors.Where(d => d.ApplicationUser != null &&
                    (d.ApplicationUser.FullName.Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
                     (d.Department != null && d.Department.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            return View(doctors);
        }

        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.DoctorId == id,
                includes: [
                    d => d.Department,
                    d => d.ApplicationUser,
                    d => d.DoctorSchedules
                ]
            );

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        public async Task<IActionResult> Create()
        {
            var departments = await _departmentRepo.GetAsync();

            var viewModel = new DoctorFormVM
            {
                Departments = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.Name
                })
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorFormVM model)
        {
            ModelState.Remove("DepartmentId");
            ModelState.Remove("Departments");
            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = model.Name,
                        PhoneNumber = model.Phone,
                        EmailConfirmed = true
                    };

                    string temporaryPassword = GenerateRandomPassword();
                    var result = await _userManager.CreateAsync(user, temporaryPassword);
                    if (result.Succeeded)
                    {
                        string? uniqueFileName = null;
                        if (model.Photo != null)
                        {
                            uniqueFileName = UploadFile(model.Photo);
                        }

                        var doctor = new DoctorModel
                        {
                            ApplicationUserId = user.Id,
                            LicenseNumber = model.LicenseNumber,
                            ConsultationFee = model.ConsultationFee,
                            YearsOfExperience = model.YearsOfExperience,
                            Bio = model.Bio,
                            DepartmentId = model.DepartmentId ?? 1,
                            Image = uniqueFileName
                        };

                        await _doctorRepo.CreateAsync(doctor);
                        await _doctorRepo.CommitAsync();

                        string emailSubject = "Clinic System - Your Account Details";
                        string emailBody = $@"
                            Welcome Dr. {model.Name},<br/><br/>
                            Your doctor account has been created by administration.<br/>
                            <b>Login Email:</b> {model.Email}<br/>
                            <b>Temporary Password:</b> {temporaryPassword}<br/><br/>
                            Please log in and update your password.";

                        try
                        {
                            await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);
                        }
                        catch
                        {
                        }

                        TempData["success_notification"] = "Doctor created successfully and credentials have been sent to the email.";
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                    TempData["error_notification"] = "Failed to create doctor. Check inputs!";
                }
            }
            else
            {
                TempData["error_notification"] = "Please fill all required fields correctly.";
            }

            await RepopulateDepartments(model);
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.DoctorId == id,
                includes: [d => d.Department, d => d.ApplicationUser]
            );

            if (doctor == null) return NotFound();

            var departments = await _departmentRepo.GetAsync();

            var viewModel = new DoctorFormVM
            {
                DoctorId = doctor.DoctorId,
                Name = doctor.ApplicationUser?.FullName ?? string.Empty,
                Email = doctor.ApplicationUser?.Email ?? string.Empty,
                Phone = doctor.ApplicationUser?.PhoneNumber ?? string.Empty,
                ConsultationFee = doctor.ConsultationFee,
                YearsOfExperience = doctor.YearsOfExperience,
                Bio = doctor.Bio,
                LicenseNumber = doctor.LicenseNumber,
                DepartmentId = doctor.DepartmentId,
                ExistingImagePath = doctor.Image,
                Departments = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.Name
                })
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DoctorFormVM model)
        {
            ModelState.Remove("DepartmentId");
            ModelState.Remove("Departments");
            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                try
                {
                    var doctor = await _doctorRepo.GetOneAsync(
                        expression: d => d.DoctorId == model.DoctorId,
                        includes: [d => d.ApplicationUser]
                    );

                    if (doctor == null) return NotFound();

                    bool isEmailChanged = false;
                    string newPassword = string.Empty;

                    if (doctor.ApplicationUser != null)
                    {
                        doctor.ApplicationUser.FullName = model.Name;
                        doctor.ApplicationUser.PhoneNumber = model.Phone;

                        if (!string.Equals(doctor.ApplicationUser.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            isEmailChanged = true;
                            doctor.ApplicationUser.Email = model.Email;
                            doctor.ApplicationUser.UserName = model.Email;

                            newPassword = GenerateRandomPassword();
                            var token = await _userManager.GeneratePasswordResetTokenAsync(doctor.ApplicationUser);
                            var resetResult = await _userManager.ResetPasswordAsync(doctor.ApplicationUser, token, newPassword);

                            if (!resetResult.Succeeded)
                            {
                                foreach (var error in resetResult.Errors)
                                {
                                    ModelState.AddModelError("", error.Description);
                                }
                                await RepopulateDepartments(model);
                                return View(model);
                            }
                        }

                        var userUpdateResult = await _userManager.UpdateAsync(doctor.ApplicationUser);
                        if (!userUpdateResult.Succeeded)
                        {
                            foreach (var error in userUpdateResult.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            await RepopulateDepartments(model);
                            return View(model);
                        }
                    }

                    if (model.Photo != null)
                    {
                        if (!string.IsNullOrEmpty(doctor.Image))
                        {
                            DeleteFile(doctor.Image);
                        }
                        doctor.Image = UploadFile(model.Photo);
                    }

                    doctor.ConsultationFee = model.ConsultationFee;
                    doctor.YearsOfExperience = model.YearsOfExperience;
                    doctor.Bio = model.Bio;
                    doctor.LicenseNumber = model.LicenseNumber;
                    doctor.DepartmentId = model.DepartmentId ?? 1;

                    _doctorRepo.Update(doctor);
                    await _doctorRepo.CommitAsync();

                    if (isEmailChanged)
                    {
                        string emailSubject = "Clinic System - Updated Account Details";
                        string emailBody = $@"
                            Welcome Dr. {model.Name},<br/><br/>
Your email address has been updated by administration.<br/>
                            <b>New Login Email:</b> {model.Email}<br/>
                            <b>New Temporary Password:</b> {newPassword}<br/><br/>
                            Please log in with your new credentials and update your password.";

                        try
                        {
                            await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);
                            TempData["success_notification"] = "Doctor updated and new credentials sent to the new email successfully!";
                        }
                        catch
                        {
                            TempData["success_notification"] = "Doctor details updated, but failed to send email notification.";
                        }
                    }
                    else
                    {
                        TempData["success_notification"] = "Doctor details updated successfully!";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                    TempData["error_notification"] = "Error occurred while updating doctor!";
                }
            }
            else
            {
                TempData["error_notification"] = "Validation failed. Please check input values.";
            }

            await RepopulateDepartments(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var doctor = await _doctorRepo.GetOneAsync(
                    expression: d => d.DoctorId == id,
                    includes: [d => d.ApplicationUser]
                );

                if (doctor != null)
                {
                    if (!string.IsNullOrEmpty(doctor.Image))
                    {
                        DeleteFile(doctor.Image);
                    }

                    var linkedUser = doctor.ApplicationUser;

                    _doctorRepo.Delete(doctor);
                    await _doctorRepo.CommitAsync();

                    if (linkedUser != null)
                    {
                        await _userManager.DeleteAsync(linkedUser);
                    }

                    TempData["success_notification"] = "Doctor and associated user deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Doctor not found!";
                }
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["error_notification"] = "Cannot delete doctor as they have linked records (Appointments/Schedules). Details: " + innerMessage;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RepopulateDepartments(DoctorFormVM model)
        {
            var departments = await _departmentRepo.GetAsync();
            model.Departments = departments.Select(d => new SelectListItem
            {
                Value = d.DepartmentId.ToString(),
                Text = d.Name
            });
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray()) + "aA1!";
        }
        private string UploadFile(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "doctors");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteFile(string fileName)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "doctors", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}