using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

public class AccountController : Controller
{
    private static readonly HashSet<string> AllowedRegistrationRoles =
        new(StringComparer.Ordinal) { "Student", "Faculty" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
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
            var member = await _dbContext.UniversityMembers.SingleOrDefaultAsync(x =>
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
                FullName = model.FullName.Trim(),
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
                    ApplicationUserId = user.Id
                });
            }
            else
            {
                _dbContext.FacultyProfiles.Add(new FacultyProfile
                {
                    FacultyId = universityId,
                    ApplicationUserId = user.Id
                });
            }

            member.IsClaimed = true;
            member.ApplicationUserId = user.Id;

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
    public async Task<IActionResult> Login(string? returnUrl = null)
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
