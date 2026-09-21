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
public sealed class ProgramsController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAcademicProgramService _programService;

    public ProgramsController(
        UserManager<ApplicationUser> userManager,
        IAcademicProgramService programService)
    {
        _userManager = userManager;
        _programService = programService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? departmentId,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = CleanSearch(search);
        status = NormalizeStatus(status);
        var departments = await _programService.GetDepartmentsAsync(false, cancellationToken: cancellationToken);
        if (departmentId.HasValue && departments.All(item => item.Id != departmentId.Value))
            departmentId = null;

        bool? isActive = status switch
        {
            "active" => true,
            "inactive" => false,
            _ => null
        };
        var data = await _programService.SearchAsync(
            search, departmentId, isActive, page, PageSize, cancellationToken);

        var model = new AcademicProgramIndexViewModel
        {
            Search = search,
            DepartmentId = departmentId,
            Status = status,
            TotalCount = data.TotalCount,
            FilteredCount = data.FilteredCount,
            ActiveCount = data.ActiveCount,
            InactiveCount = data.InactiveCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            DepartmentOptions = departments.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
                item.Id.ToString())).ToList(),
            Programs = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var item = await _programService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();

        var model = new AcademicProgramDetailsViewModel
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            DepartmentId = item.DepartmentId,
            DepartmentCode = item.DepartmentCode,
            DepartmentName = item.DepartmentName,
            TotalCredits = item.TotalCredits,
            DurationYears = item.DurationYears,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? departmentId, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = new AcademicProgramFormViewModel { DepartmentId = departmentId ?? 0 };
        await PopulateDepartmentsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AcademicProgramFormViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (ModelState.IsValid)
        {
            var result = await _programService.CreateAsync(ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Name.Trim()} was added successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The program could not be created.");
        }

        await PopulateDepartmentsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var item = await _programService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();

        var model = new AcademicProgramFormViewModel
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            DepartmentId = item.DepartmentId,
            TotalCredits = item.TotalCredits,
            DurationYears = item.DurationYears,
            IsActive = item.IsActive
        };
        await PopulateDepartmentsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AcademicProgramFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (ModelState.IsValid)
        {
            var result = await _programService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Name.Trim()} was updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The program could not be updated.");
        }

        await PopulateDepartmentsAsync(model, cancellationToken);
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _programService.SetActiveAsync(id, isActive, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = result.Error ?? "The program status could not be changed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["AdminSuccess"] = isActive
            ? "Program activated successfully."
            : "Program deactivated. Existing academic references remain protected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateDepartmentsAsync(
        AcademicProgramFormViewModel model,
        CancellationToken cancellationToken)
    {
        var departments = await _programService.GetDepartmentsAsync(
            true, model.DepartmentId > 0 ? model.DepartmentId : null, cancellationToken);
        model.DepartmentOptions = departments.Select(item => new SelectListItem(
            $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
            item.Id.ToString())).ToList();
    }

    private static AcademicProgramInput ToInput(AcademicProgramFormViewModel model) => new(
        model.Code,
        model.Name,
        model.Description,
        model.DepartmentId,
        model.TotalCredits,
        model.DurationYears,
        model.IsActive);

    private static AcademicProgramRowViewModel ToRow(AcademicProgramData item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        DepartmentCode = item.DepartmentCode,
        DepartmentName = item.DepartmentName,
        TotalCredits = item.TotalCredits,
        DurationYears = item.DurationYears,
        IsActive = item.IsActive
    };

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" => "active",
        "inactive" => "inactive",
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
        if (parts.Length == 0) return "AD";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
