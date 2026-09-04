 
using clinicManagementSystem.Areas.Admin.Controllers;
using clinicManagementSystem.Areas.Patient.Controllers;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services;
using clinicManagementSystem.Services.IServices;
using clinicManagementSystem.Utilities;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace clinicManagementSystem.Areas.Identity.Controllers
{
    [Area(SD.IDENTITY_AREA)]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAccountService _accountService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;

        
        public AccountController(
            UserManager<ApplicationUser> userManager,
            IAccountService accountService,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IRepository<ApplicationUserOTP> applicationUserOTPRepository)
        {
            _userManager = userManager;
            _accountService = accountService;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _applicationUserOTPRepository = applicationUserOTPRepository;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_accountService.IsLogined(User))
            {
                return RedirectToAction("Index", "Home", new  { area = SD.PATIENT_AREA });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
                return View(registerVM);

            ApplicationUser user = new()
            {
                UserName = registerVM.Email,
                Email = registerVM.Email,
                FullName = registerVM.FullName,
                PhoneNumber = registerVM.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, registerVM.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(registerVM);
            }
            await _userManager.AddToRoleAsync(user, "Patient");

            await _accountService.SendMailAsync(user, Url, Request, EmailType.Register);

            TempData["success_notification"] = "Add Account Successfully, check you email";

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Confirm(string token, string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                TempData["error_notification"] = String.Join(",", result.Errors.Select(e => e.Description));
            }

            TempData["success_notification"] = "Email confirmed successfully, You can now log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {

            if (_accountService.IsLogined(User))
            {
                return RedirectToAction("Index", "Home", new { area = SD.PATIENT_AREA });
            }
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManager.FindByEmailAsync(loginVM.Email) ?? await _userManager.FindByNameAsync(loginVM.Email);

            if (user is null)
            {
                ModelState.AddModelError(nameof(LoginVM.Email), "Invalid Email");
                ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

                return View(loginVM);
            }
            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(nameof(LoginVM.Email), "Confirm Your Email First");

                return View(loginVM);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(LoginVM.Email), "Invalid Email");
                ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

                return View(loginVM);
            }

            TempData["success_notification"] = $"Welcome Back {user.FullName}";
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("SuperAdmin") || roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Dashboard",
                    new { area = SD.ADMIN_AREA });
            }

            if (roles.Contains("Doctor"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = SD.DOCTOR_AREA });
            }

            if (roles.Contains("Patient"))
            {
                return RedirectToAction("Index", "Home", new { area = SD.PATIENT_AREA});
            }

            if (!string.IsNullOrEmpty(loginVM.ReturnUrl) && Url.IsLocalUrl(loginVM.ReturnUrl))
                return Redirect(loginVM.ReturnUrl);

            return RedirectToAction("Index", "Home", new { area = SD.PATIENT_AREA });
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            if (!ModelState.IsValid)
                return View(resendEmailConfirmationVM);

            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.Email) ?? await _userManager.FindByNameAsync(resendEmailConfirmationVM.Email);

            if (user is not null && !user.EmailConfirmed)
                await _accountService.SendMailAsync(user, Url, Request, EmailType.ResendConfirmation);

            TempData["success_notification"] = $"Resend Email Confirmation successfully, please check yoy email";

            return RedirectToAction(nameof(Login));
        }
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(forgetPasswordVM);

            var user = await _userManager.FindByEmailAsync(forgetPasswordVM.Email);

            if (user is not null)
            {
                await _accountService.SendOtpMailAsync(user);
            }

            TempData["success_notification"] = "If this email exists, an OTP has been sent, please check your email";
             
            return RedirectToAction(nameof(ValidateOTP), new { email = forgetPasswordVM.Email });
        }

        [HttpGet]
        public IActionResult ValidateOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return NotFound();

            return View(new ValidateOTPVM { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM validateOTPVM)
        {
            if (!ModelState.IsValid)
                return View(validateOTPVM);

            var user = await _userManager.FindByEmailAsync(validateOTPVM.Email);

            if (user is null)
            {
                ModelState.AddModelError(nameof(ValidateOTPVM.Otp), "Invalid or expired OTP");
                return View(validateOTPVM);
            }

            var otp = await _applicationUserOTPRepository.GetOneAsync(e => e.ApplicationUserId == user.Id
                         && e.OTP == validateOTPVM.Otp
                         && !e.IsUsed
                         && e.ValidTo >= DateTime.Now);

            if (otp is null)
            {
                ModelState.AddModelError(nameof(ValidateOTPVM.Otp), "Invalid or expired OTP");
                return View(validateOTPVM);
            }

            otp.IsUsed = true;
            await _applicationUserOTPRepository.CommitAsync();

             
            return RedirectToAction(nameof(ResetPassword), new { email = validateOTPVM.Email });
        }
        [HttpPost]
        public async Task<IActionResult> ResendOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return NotFound();

            var user = await _userManager.FindByEmailAsync(email);

            if (user is not null)
            {
                var totalOtp = (await _applicationUserOTPRepository.GetAsync(
                    e => e.ApplicationUserId == user.Id && e.CreateAt >= DateTime.Now.AddHours(-24))).Count();

                if (totalOtp > 3)
                {
                    TempData["error_notification"] = "You have exceeded the maximum number of OTP attempts. Please try again later.";
                    return RedirectToAction(nameof(ValidateOTP), new { email });
                }

                await _accountService.SendOtpMailAsync(user);
            }

            TempData["success_notification"] = "OTP number sent successfully. Please check your email.";
            return RedirectToAction(nameof(ValidateOTP), new { email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return NotFound();

            return View(new NewPasswordVM { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(NewPasswordVM newPasswordVM)
        {
            if (!ModelState.IsValid)
                return View(newPasswordVM);

            var user = await _userManager.FindByEmailAsync(newPasswordVM.Email);

            if (user is null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPasswordVM.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(newPasswordVM);
            }

            TempData["success_notification"] = "Password changed successfully, you can now log in";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM changePasswordVM)
        {
            if (!ModelState.IsValid)
                return View(changePasswordVM);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(changePasswordVM);
            }

            TempData["success_notification"] = "Password changed successfully";

            return RedirectToAction(nameof(ProfileController.Index), "Profile" );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
                [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new { returnUrl });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(
    string returnUrl = null,
    string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Error from external provider: {remoteError}");

                return RedirectToAction(nameof(Login));
            }

            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var signInResult =
                await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl ?? "/");
            }

            var email =
                info.Principal.FindFirstValue(ClaimTypes.Email);

            var username =
                info.Principal.FindFirstValue(ClaimTypes.Name);

            if (email == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email was not provided by Google.");

                return RedirectToAction(nameof(Login));
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var random = new Random();

                var randomNumber =
                    random.Next(1000, 9999);

                user = new ApplicationUser
                {
                    UserName =
                        (username ?? email.Split('@')[0])
                        .Replace(" ", "") + randomNumber,

                    Email = email,

                    EmailConfirmed = true
                };

                var createUserResult =
                    await _userManager.CreateAsync(user);

                if (!createUserResult.Succeeded)
                {
                    foreach (var error in createUserResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return RedirectToAction(nameof(Login));
                }
            }

            var existingLogins =
                await _userManager.GetLoginsAsync(user);

            var hasGoogleLogin =
                existingLogins.Any(
                    l => l.LoginProvider == info.LoginProvider);

            if (!hasGoogleLogin)
            {
                var addLoginResult =
                    await _userManager.AddLoginAsync(
                        user,
                        info);

                if (!addLoginResult.Succeeded)
                {
                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return RedirectToAction(nameof(Login));
                }
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
