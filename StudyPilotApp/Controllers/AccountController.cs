using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

public class AccountController : Controller
{
    private static readonly HashSet<string> AllowedRegistrationRoles =
        new(StringComparer.Ordinal) { "Student", "Faculty" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPlatformSettingsService _settingsService;
    private readonly IAccountEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        IPlatformSettingsService settingsService,
        IAccountEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _settingsService = settingsService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
            {
                return await RedirectToDashboardAsync(currentUser);
            }
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!AllowedRegistrationRoles.Contains(model.AccountType))
        {
            ModelState.AddModelError(nameof(model.AccountType), "Please select Student or Faculty.");
        }

        if (AllowedRegistrationRoles.Contains(model.AccountType))
        {
            var settings = await _settingsService.GetAsync();
            var registrationEnabled = model.AccountType == "Student"
                ? settings.StudentRegistrationEnabled
                : settings.FacultyRegistrationEnabled;
            if (!registrationEnabled)
                ModelState.AddModelError(nameof(model.AccountType), $"{model.AccountType} registration is temporarily disabled.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var universityId = model.UniversityId.Trim().ToUpperInvariant();
        var normalizedEmail = _userManager.NormalizeEmail(email);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var member = await _dbContext.UniversityMembers
                .Include(x => x.Department)
                .SingleOrDefaultAsync(x =>
                    x.UniversityId == universityId &&
                    x.NormalizedEmail == normalizedEmail &&
                    x.Role == model.AccountType &&
                    x.IsActive &&
                    !x.IsClaimed);

            if (member is null)
            {
                ModelState.AddModelError(
                    nameof(model.UniversityId),
                    "The university ID, email and account type do not match an available university record.");
                await transaction.RollbackAsync();
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                await transaction.RollbackAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = string.IsNullOrWhiteSpace(member.FullName)
                    ? model.FullName.Trim()
                    : member.FullName!,
                Email = email,
                UserName = email,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult, model);
                await transaction.RollbackAsync();
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.AccountType);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Role assignment failed for new user {UserId}: {Errors}",
                    user.Id, FormatErrors(roleResult));
                ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
                await transaction.RollbackAsync();
                return View(model);
            }

            if (model.AccountType == "Student")
            {
                _dbContext.StudentProfiles.Add(new StudentProfile
                {
                    StudentId = universityId,
                    ApplicationUserId = user.Id,
                    Department = member.Department?.Name,
                    Batch = member.Batch,
                    Semester = member.CurrentSemester
                });
            }
            else
            {
                _dbContext.FacultyProfiles.Add(new FacultyProfile
                {
                    FacultyId = universityId,
                    ApplicationUserId = user.Id,
                    DepartmentId = member.DepartmentId
                });
            }

            member.IsClaimed = true;
            member.ApplicationUserId = user.Id;
            member.RegisteredAt = DateTimeOffset.UtcNow;
            member.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectToDashboardAsync(user);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError(exception, "Registration failed for university ID {UniversityId}.", universityId);
            ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("account-recovery")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CheckEmailAvailability(
        string? email,
        CancellationToken cancellationToken)
    {
        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            return Json(new
            {
                isAvailable = false,
                isValid = false,
                message = "Enter a valid email address."
            });
        }

        var normalizedEmail = _userManager.NormalizeEmail(email);
        var exists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);

        return Json(new
        {
            isAvailable = !exists,
            isValid = true,
            message = exists
                ? "An account already uses this email."
                : "Email is available for registration."
        });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-recovery")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, token = encodedToken },
                Request.Scheme);

            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                var delivered = await _emailService.SendPasswordResetAsync(
                    user.Email!,
                    user.FullName,
                    callbackUrl,
                    cancellationToken);

                if (!delivered)
                {
                    _logger.LogWarning(
                        "A password reset email could not be delivered for user {UserId}. SMTP configured: {Configured}.",
                        user.Id,
                        _emailService.IsConfigured);
                }
            }
        }

        // Always show the same response so visitors cannot discover registered emails.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account-recovery")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            if (error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.Password), error.Description);
            else
                ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null, bool disabled = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
            {
                return await RedirectToDashboardAsync(currentUser);
            }
        }

        ViewData["ReturnUrl"] = returnUrl;
        if (disabled) ViewData["AccountDisabled"] = true;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            AddInvalidLoginError();
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been disabled. Contact the StudyPilot administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return await RedirectToDashboardAsync(user);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Please try again later.");
        }
        else
        {
            AddInvalidLoginError();
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private void AddInvalidLoginError() =>
        ModelState.AddModelError(string.Empty, "Invalid email or password.");

    private void AddIdentityErrors(IdentityResult result, RegisterViewModel model)
    {
        foreach (var error in result.Errors)
        {
            if (error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Password), error.Description);
            }
            else if (error.Code.Equals("DuplicateEmail", StringComparison.OrdinalIgnoreCase) ||
                     error.Code.Equals("DuplicateUserName", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

    private async Task<IActionResult> RedirectToDashboardAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (await _userManager.IsInRoleAsync(user, "Faculty"))
        {
            return RedirectToAction("Index", "Faculty");
        }

        if (await _userManager.IsInRoleAsync(user, "Student"))
        {
            return RedirectToAction("Index", "Student");
        }

        await _signInManager.SignOutAsync();
        ModelState.AddModelError(string.Empty, "Your account does not have an assigned role.");
        return View("Login", new LoginViewModel());
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));
}
