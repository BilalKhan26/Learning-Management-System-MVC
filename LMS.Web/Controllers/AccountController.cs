using LMS.Models;
using LMS.Service;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtEmailService _jwtEmailService;
        private readonly EmailSender _emailSender;
        private readonly IConfiguration _config; // Add this line

        public AccountController(SignInManager<ApplicationUser> signInManager,
                                 UserManager<ApplicationUser> userManager,
                                 JwtEmailService jwtEmailService,
                                 EmailSender emailSender,
                                 IConfiguration config) // Update the constructor
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtEmailService = jwtEmailService;
            _emailSender = emailSender;
            _config = config; // Add this line
        }

        // ================= Login =================
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email and Password are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            // Block login if email not confirmed
            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Please confirm your email before logging in.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "AdminDashboard");
                else if (roles.Contains("Instructor"))
                    return RedirectToAction("Index", "InstructorDashboard");
                else if (roles.Contains("Student"))
                    return RedirectToAction("Index", "StudentDashboard");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ================= Register =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string displayName, string email, string password, string role)
        {
            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View();
            }

            var user = new ApplicationUser
            {
                DisplayName = displayName,
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (!await _userManager.IsInRoleAsync(user, role))
                    await _userManager.AddToRoleAsync(user, role);

                // Generate JWT confirmation token
                var token = _jwtEmailService.GenerateEmailConfirmationToken(user.Id, user.Email);
                Console.WriteLine($"Generated Token: {token}");
                var confirmationLink = Url.Action("ConfirmEmailJwt", "Account", new { token }, Request.Scheme);

                // TODO: Replace with actual email sending service
                Console.WriteLine($"Email Confirmation Link: {confirmationLink}");
                await _emailSender.SendEmailAsync(email, "Confirm Your Email",
    $"<p>Please confirm your account by clicking <a href='{confirmationLink}'>here</a>.</p>");

                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View();
        }

        // ================= Confirm Email with JWT =================
        [HttpGet]
        public async Task<IActionResult> ConfirmEmailJwt(string token)
        {
            try
            {
                Console.WriteLine($"Received Token: {token}");
                var principal = _jwtEmailService.ValidateEmailConfirmationToken(token);

                foreach (var claim in principal.Claims)
                {
                    Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
                }

                var userId = principal.FindFirst("sub")?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null) return BadRequest("Invalid token");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound("User not found");

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                return View("ConfirmEmail");
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid or expired token: " + ex.Message);
            }
        }
        // ================= Resend Confirmation Email =================\
        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View("Login");
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View("Login");
            }
            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email is already confirmed.");
                return View("Login");
            }
            // Generate JWT confirmation token
            var token = _jwtEmailService.GenerateEmailConfirmationToken(user.Id, user.Email);
            var confirmationLink = Url.Action("ConfirmEmailJwt", "Account", new { token }, Request.Scheme);

            await _emailSender.SendEmailAsync(email, "Confirm Your Email",
                $"<p>Please confirm your account by clicking <a href='{confirmationLink}'>here</a>.</p>");
            return View("Login");
        }
        // ================= Forgot Password =================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View();
            }
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email, token = resetToken }, Request.Scheme);

            await _emailSender.SendEmailAsync(email, "Reset Your Password",
                $"<p>Reset your password by clicking <a href='{resetLink}'>here</a>.</p>");
            return View("ForgotPasswordSuccess");
        }

        // ================= Reset Password =================
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            ViewData["Email"] = email;
            ViewData["Token"] = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View();
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View();
            }
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                return View("ForgotPasswordSuccess");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }
        // ================= Reset Password =================
        //[HttpGet]
        //public IActionResult ForgotPassword(string email, string token)
        //{
        //    ViewData["Email"] = email;
        //    ViewData["Token"] = token;
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(string email, string token, string newPassword)
        //{
        //    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
        //    {
        //        ModelState.AddModelError(string.Empty, "All fields are required.");
        //        return View();
        //    }
        //    var user = await _userManager.FindByEmailAsync(email);
        //    if (user == null)
        //    {
        //        ModelState.AddModelError(string.Empty, "User not found.");
        //        return View();
        //    }
        //    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        //    if (result.Succeeded)
        //    {
        //        return View("ForgotPasswordSuccess");
        //    }
        //    foreach (var error in result.Errors)
        //        ModelState.AddModelError(string.Empty, error.Description);
        //    return View();
        //}
    }
}
