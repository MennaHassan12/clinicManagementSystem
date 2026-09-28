 
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
        public async Task<IActionResult> Register()
        {
            if (_accountService.IsLogined(User))
            {
                return await RedirectToHomeByRole();
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
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);

            if (user is null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                TempData["error_notification"] = String.Join(",", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["success_notification"] = "Email confirmed successfully, You can now log in.";
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {

            if (_accountService.IsLogined(User))
            {
                return await RedirectToHomeByRole();
            }
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked due to many failed attempts. Try again later or reset your password.");
                return View(loginVM);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(LoginVM.Email), "Invalid Email");
                ModelState.AddModelError(nameof(LoginVM.Password), "Invalid Password");

                return View(loginVM);
            }

            TempData["success_notification"] = $"Welcome Back {user.FullName}";

            if (!string.IsNullOrEmpty(loginVM.ReturnUrl) && Url.IsLocalUrl(loginVM.ReturnUrl))
                return LocalRedirect(loginVM.ReturnUrl);

            return await RedirectToHomeByRole(user);
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            if (!ModelState.IsValid)
                return View(resendEmailConfirmationVM);

            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.Email) ?? await _userManager.FindByNameAsync(resendEmailConfirmationVM.Email);

            if (user is not null && !user.EmailConfirmed)
                await _accountService.SendMailAsync(user, Url, Request, EmailType.ResendConfirmation);

            TempData["success_notification"] = $"Resend Email Confirmation successfully, please check your email";

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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM validateOTPVM)
        {
            if (!ModelState.IsValid) return View(validateOTPVM);

            var user = await _userManager.FindByEmailAsync(validateOTPVM.Email);
            if (user is null)
            {
                ModelState.AddModelError(nameof(ValidateOTPVM.Otp), "Invalid or expired OTP");
                return View(validateOTPVM);
            }

            var otp = await _applicationUserOTPRepository.GetOneAsync(e =>
                e.ApplicationUserId == user.Id
                && !e.IsUsed
                && e.ValidTo >= DateTime.UtcNow
                && e.FailedAttempts < 5);

            if (otp is null || otp.OTP != validateOTPVM.Otp)
            {
                if (otp is not null)
                {
                    otp.FailedAttempts++;
                    await _applicationUserOTPRepository.CommitAsync();
                }
                ModelState.AddModelError(nameof(ValidateOTPVM.Otp), "Invalid or expired OTP");
                return View(validateOTPVM);
            }

            otp.IsUsed = true;
            await _applicationUserOTPRepository.CommitAsync();

            TempData["ResetToken"] = await _userManager.GeneratePasswordResetTokenAsync(user);
            TempData["ResetEmail"] = user.Email;
            return RedirectToAction(nameof(ResetPassword));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return NotFound();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null)
                await _accountService.SendOtpMailAsync(user);

            TempData["success_notification"] = "If this email exists, a new code has been sent (max 3 codes per 24 hours).";
            return RedirectToAction(nameof(ValidateOTP), new { email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            token ??= TempData["ResetToken"] as string;
            email ??= TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction(nameof(ForgetPassword));

            return View(new NewPasswordVM { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(NewPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is null) return BadRequest();

            var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(vm);
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

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
            if (!await _userManager.HasPasswordAsync(user))
            {
                TempData["error_notification"] = "Your account uses Google sign-in. Use 'Forgot password' to create a password.";
                return RedirectToAction(nameof(ProfileController.Index), "Profile", new { area = SD.IDENTITY_AREA });
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(changePasswordVM);
            }

            TempData["success_notification"] = "Password changed successfully";
            await _signInManager.RefreshSignInAsync(user);

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
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = SD.IDENTITY_AREA, returnUrl });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["error_notification"] = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user == null)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    TempData["error_notification"] = "Email was not provided by Google.";
                    return RedirectToAction(nameof(Login));
                }

                user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name.Length > 30 ? name[..30] : name,
                        EmailConfirmed = true
                    };
                    var create = await _userManager.CreateAsync(user);
                    if (!create.Succeeded)
                    {
                        TempData["error_notification"] = string.Join(", ", create.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Login));
                    }
                }
                else if (!user.EmailConfirmed)
                {
                    // حماية من pre-hijacking
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    if (await _userManager.HasPasswordAsync(user))
                        await _userManager.RemovePasswordAsync(user);
                }

                var add = await _userManager.AddLoginAsync(user, info);
                if (!add.Succeeded)
                {
                    TempData["error_notification"] = string.Join(", ", add.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Login));
                }
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                TempData["error_notification"] = "Your account is locked. Try again later.";
                return RedirectToAction(nameof(Login));
            }

            if (!(await _userManager.GetRolesAsync(user)).Any())
                await _userManager.AddToRoleAsync(user, SD.ROLE_PATIENT);

            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return await RedirectToHomeByRole(user);
        }

        private async Task<IActionResult> RedirectToHomeByRole(ApplicationUser appUser = null)
        {
            var user = appUser ?? await _userManager.GetUserAsync(User);

            if (user is not null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains(SD.ROLE_SUPER_ADMIN) || roles.Contains(SD.ROLE_ADMIN))
                    return RedirectToAction("Index", "Dashboard", new { area = SD.ADMIN_AREA });

                if (roles.Contains(SD.ROLE_DOCTOR))
                    return RedirectToAction("Index", "Dashboard", new { area = SD.DOCTOR_AREA });
            }

            return RedirectToAction("Index", "Home", new { area = SD.PATIENT_AREA });
        }
    }
}
