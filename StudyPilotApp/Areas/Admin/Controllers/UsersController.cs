using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminUserService _adminUserService;
    private readonly IRegistrationAuthorizationService _authorizationService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IAdminUserService adminUserService,
        IRegistrationAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _adminUserService = adminUserService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string role = "all",
        string status = "all",
        int? departmentId = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        search = CleanSearch(search);
        role = NormalizeRole(role);
        status = NormalizeStatus(status);
        var departments = await _authorizationService.GetDepartmentsAsync(
            false, cancellationToken: cancellationToken);
        if (departmentId.HasValue && departments.All(item => item.Id != departmentId.Value))
            departmentId = null;

        bool? isActive = status switch
        {
            "active" => true,
            "disabled" => false,
            _ => null
        };
        var data = await _adminUserService.SearchAsync(
            search,
            role == "all" ? null : role,
            isActive,
            departmentId,
            page,
            PageSize,
            cancellationToken);

        var model = new AdminUserIndexViewModel
        {
            Search = search,
            Role = role,
            Status = status,
            DepartmentId = departmentId,
            TotalCount = data.TotalCount,
            StudentCount = data.StudentCount,
            FacultyCount = data.FacultyCount,
            AdminCount = data.AdminCount,
            ActiveCount = data.ActiveCount,
            DisabledCount = data.DisabledCount,
            FilteredCount = data.FilteredCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            DepartmentOptions = departments.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}", item.Id.ToString(), item.Id == departmentId)).ToList(),
            Users = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _adminUserService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();

        var model = new AdminUserDetailsViewModel
        {
            Id = item.Id,
            UserFullName = item.FullName,
            UserEmail = item.Email,
            Role = item.Role,
            IsActive = item.IsActive,
            IsCurrentAdmin = item.Id == admin.Id,
            CreatedAt = item.CreatedAt,
            DeactivatedAt = item.DeactivatedAt,
            AccountStatusChangedAt = item.AccountStatusChangedAt,
            EmailConfirmed = item.EmailConfirmed,
            PhoneNumber = item.PhoneNumber,
            AccessFailedCount = item.AccessFailedCount,
            LockoutEnd = item.LockoutEnd,
            UniversityId = item.UniversityId,
            DepartmentCode = item.DepartmentCode,
            DepartmentName = item.DepartmentName,
            ProgramCode = item.ProgramCode,
            ProgramName = item.ProgramName,
            Batch = item.Batch,
            CurrentSemester = item.CurrentSemester,
            UserInitials = CreateInitials(item.FullName)
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(
        string id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var result = await _adminUserService.SetActiveAsync(
            id, isActive, admin.Id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = result.Error ?? "The account status could not be changed.";
        }
        else
        {
            TempData["AdminSuccess"] = isActive
                ? "The user account was reactivated successfully."
                : "The user account was disabled and its active sessions were revoked.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static AdminUserRowViewModel ToRow(AdminUserData item) => new()
    {
        Id = item.Id,
        FullName = item.FullName,
        Email = item.Email,
        Role = item.Role,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt,
        UniversityId = item.UniversityId,
        DepartmentCode = item.DepartmentCode,
        DepartmentName = item.DepartmentName,
        Initials = CreateInitials(item.FullName)
    };

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeRole(string? role) => role?.ToLowerInvariant() switch
    {
        "student" => "Student",
        "faculty" => "Faculty",
        "admin" => "Admin",
        _ => "all"
    };

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" => "active",
        "disabled" => "disabled",
        _ => "all"
    };

    private static void PopulateShell(AdminShellViewModel model, ApplicationUser user)
    {
        model.FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Administrator" : user.FullName;
        model.Email = user.Email ?? string.Empty;
        model.Initials = CreateInitials(user.FullName);
    }

    private static string CreateInitials(string? value)
    {
        var parts = (value ?? string.Empty).Split(
            ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "U";
        return string.Concat(parts.Take(2).Select(item => char.ToUpperInvariant(item[0])));
    }
}
