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
public sealed class RegistrationAuthorizationsController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRegistrationAuthorizationService _authorizationService;

    public RegistrationAuthorizationsController(
        UserManager<ApplicationUser> userManager,
        IRegistrationAuthorizationService authorizationService)
    {
        _userManager = userManager;
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
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        search = CleanSearch(search);
        role = NormalizeRoleFilter(role);
        status = NormalizeStatus(status);

        var departments = await _authorizationService.GetDepartmentsAsync(false, cancellationToken: cancellationToken);
        if (departmentId.HasValue && departments.All(item => item.Id != departmentId.Value)) departmentId = null;
        var data = await _authorizationService.SearchAsync(
            search, role == "all" ? null : role, status, departmentId, page, PageSize, cancellationToken);

        var model = new RegistrationAuthorizationIndexViewModel
        {
            Search = search,
            Role = role,
            Status = status,
            DepartmentId = departmentId,
            TotalCount = data.TotalCount,
            StudentCount = data.StudentCount,
            FacultyCount = data.FacultyCount,
            RegisteredCount = data.RegisteredCount,
            UnregisteredCount = data.UnregisteredCount,
            DisabledCount = data.DisabledCount,
            FilteredCount = data.FilteredCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            DepartmentOptions = departments.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}", item.Id.ToString())).ToList(),
            Records = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var item = await _authorizationService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = ToDetails(item);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string role = "Student",
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = new RegistrationAuthorizationFormViewModel
        {
            Role = role == "Faculty" ? "Faculty" : "Student"
        };
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RegistrationAuthorizationFormViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        ValidateConditionalFields(model);

        if (ModelState.IsValid)
        {
            var result = await _authorizationService.CreateAsync(ToInput(model), user.Id, cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"Registration authorization created for {model.FullName.Trim()}.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The authorization could not be created.");
        }

        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var item = await _authorizationService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new RegistrationAuthorizationFormViewModel
        {
            Id = item.Id,
            IsClaimed = item.IsClaimed,
            UniversityId = item.UniversityId,
            Email = item.Email,
            FullName = item.FullName ?? item.RegisteredUserName ?? string.Empty,
            Role = item.Role,
            DepartmentId = item.DepartmentId ?? 0,
            ProgramId = item.ProgramId,
            Batch = item.Batch,
            CurrentSemester = item.CurrentSemester,
            IsActive = item.IsActive
        };
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        RegistrationAuthorizationFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var existing = await _authorizationService.GetAsync(id, cancellationToken);
        if (existing is null) return NotFound();
        model.IsClaimed = existing.IsClaimed;
        ValidateConditionalFields(model);

        if (ModelState.IsValid)
        {
            var result = await _authorizationService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = "Registration authorization updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The authorization could not be updated.");
        }

        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.SetActiveAsync(id, isActive, cancellationToken);
        if (!result.Succeeded)
            TempData["AdminError"] = result.Error ?? "The authorization status could not be changed.";
        else
            TempData["AdminSuccess"] = isActive
                ? "Registration authorization activated."
                : "Registration authorization deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ProgramOptions(int departmentId, CancellationToken cancellationToken)
    {
        if (departmentId <= 0) return Json(Array.Empty<object>());
        var programs = await _authorizationService.GetProgramsAsync(
            departmentId, true, cancellationToken: cancellationToken);
        return Json(programs.Select(item => new { item.Id, Text = $"{item.Code} — {item.Name}" }));
    }

    private async Task PopulateOptionsAsync(
        RegistrationAuthorizationFormViewModel model,
        CancellationToken cancellationToken)
    {
        var departments = await _authorizationService.GetDepartmentsAsync(
            true, model.DepartmentId > 0 ? model.DepartmentId : null, cancellationToken);
        model.DepartmentOptions = departments.Select(item => new SelectListItem(
            $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
            item.Id.ToString())).ToList();

        if (model.DepartmentId > 0)
        {
            var programs = await _authorizationService.GetProgramsAsync(
                model.DepartmentId, true, model.ProgramId, cancellationToken);
            model.ProgramOptions = programs.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
                item.Id.ToString())).ToList();
        }
    }

    private void ValidateConditionalFields(RegistrationAuthorizationFormViewModel model)
    {
        if (model.Role == "Student")
        {
            if (!model.ProgramId.HasValue)
                ModelState.AddModelError(nameof(model.ProgramId), "Select a program for the Student.");
            if (!model.CurrentSemester.HasValue)
                ModelState.AddModelError(nameof(model.CurrentSemester), "Enter the current semester.");
        }
        else if (model.Role == "Faculty")
        {
            model.ProgramId = null;
            model.Batch = null;
            model.CurrentSemester = null;
            ModelState.Remove(nameof(model.ProgramId));
            ModelState.Remove(nameof(model.CurrentSemester));
        }
    }

    private static RegistrationAuthorizationInput ToInput(RegistrationAuthorizationFormViewModel model) => new(
        model.UniversityId, model.Email, model.Role, model.FullName, model.DepartmentId,
        model.ProgramId, model.Batch, model.CurrentSemester, model.IsActive);

    private static RegistrationAuthorizationRowViewModel ToRow(RegistrationAuthorizationData item) => new()
    {
        Id = item.Id,
        UniversityId = item.UniversityId,
        Email = item.Email,
        Role = item.Role,
        FullName = item.FullName ?? item.RegisteredUserName ?? "Authorized user",
        DepartmentCode = item.DepartmentCode,
        ProgramCode = item.ProgramCode,
        IsActive = item.IsActive,
        IsClaimed = item.IsClaimed,
        CreatedAt = item.CreatedAt
    };

    private static RegistrationAuthorizationDetailsViewModel ToDetails(RegistrationAuthorizationData item) => new()
    {
        Id = item.Id,
        UniversityId = item.UniversityId,
        Email = item.Email,
        Role = item.Role,
        FullName = item.FullName ?? item.RegisteredUserName ?? "Authorized user",
        DepartmentId = item.DepartmentId,
        DepartmentCode = item.DepartmentCode,
        DepartmentName = item.DepartmentName,
        ProgramId = item.ProgramId,
        ProgramCode = item.ProgramCode,
        ProgramName = item.ProgramName,
        Batch = item.Batch,
        CurrentSemester = item.CurrentSemester,
        IsActive = item.IsActive,
        IsClaimed = item.IsClaimed,
        ApplicationUserId = item.ApplicationUserId,
        RegisteredUserName = item.RegisteredUserName,
        CreatedAt = item.CreatedAt,
        RegisteredAt = item.RegisteredAt,
        UpdatedAt = item.UpdatedAt
    };

    private static string? CleanSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, 100)];
    }

    private static string NormalizeRoleFilter(string? value) => value switch
    {
        "Student" => "Student",
        "Faculty" => "Faculty",
        _ => "all"
    };

    private static string NormalizeStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "registered" => "registered",
        "unregistered" => "unregistered",
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
        var parts = (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "AD";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
