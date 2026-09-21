using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DepartmentsController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(
        UserManager<ApplicationUser> userManager,
        IDepartmentService departmentService)
    {
        _userManager = userManager;
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = CleanSearch(search);
        status = NormalizeStatus(status);
        bool? isActive = status switch
        {
            "active" => true,
            "inactive" => false,
            _ => null
        };

        var data = await _departmentService.SearchAsync(search, isActive, page, PageSize, cancellationToken);
        var model = new DepartmentIndexViewModel
        {
            Search = search,
            Status = status,
            TotalCount = data.TotalCount,
            FilteredCount = data.FilteredCount,
            ActiveCount = data.ActiveCount,
            InactiveCount = data.InactiveCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            Departments = data.Items.Select(item => new DepartmentRowViewModel
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt
            }).ToList()
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var department = await _departmentService.GetAsync(id, cancellationToken);
        if (department is null) return NotFound();

        var model = new DepartmentDetailsViewModel
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = new DepartmentFormViewModel();
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DepartmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (ModelState.IsValid)
        {
            var result = await _departmentService.CreateAsync(ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Name.Trim()} was added successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The department could not be created.");
        }

        PopulateShell(model, user);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var department = await _departmentService.GetAsync(id, cancellationToken);
        if (department is null) return NotFound();

        var model = new DepartmentFormViewModel
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive
        };
        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        DepartmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (ModelState.IsValid)
        {
            var result = await _departmentService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Name.Trim()} was updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The department could not be updated.");
        }

        PopulateShell(model, user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.SetActiveAsync(id, isActive, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = result.Error ?? "The department status could not be changed.";
            return RedirectToAction(nameof(Index));
        }

        TempData["AdminSuccess"] = isActive
            ? "Department activated successfully."
            : "Department deactivated. Existing academic records remain protected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static DepartmentInput ToInput(DepartmentFormViewModel model) =>
        new(model.Code, model.Name, model.Description, model.IsActive);

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
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "AD";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
