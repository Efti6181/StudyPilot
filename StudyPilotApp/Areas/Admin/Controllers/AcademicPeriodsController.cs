using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AcademicPeriodsController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAcademicPeriodService _periodService;

    public AcademicPeriodsController(
        UserManager<ApplicationUser> userManager,
        IAcademicPeriodService periodService)
    {
        _userManager = userManager;
        _periodService = periodService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = CleanSearch(search);
        status = NormalizeStatus(status);
        var data = await _periodService.SearchAsync(search, status, page, PageSize, cancellationToken);

        var model = new AcademicPeriodIndexViewModel
        {
            Search = search,
            Status = status,
            TotalCount = data.TotalCount,
            CurrentCount = data.CurrentCount,
            UpcomingCount = data.UpcomingCount,
            PastCount = data.PastCount,
            InactiveCount = data.InactiveCount,
            FilteredCount = data.FilteredCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            Periods = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _periodService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var (status, statusClass) = GetStatus(item);
        var model = new AcademicPeriodDetailsViewModel
        {
            Id = item.Id,
            Label = item.Label,
            Term = item.Term,
            AcademicYear = item.AcademicYear,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            RegistrationStartDate = item.RegistrationStartDate,
            RegistrationEndDate = item.RegistrationEndDate,
            Notes = item.Notes,
            IsCurrent = item.IsCurrent,
            IsActive = item.IsActive,
            DisplayStatus = status,
            StatusClass = statusClass,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var term = today.Month <= 4 ? AcademicTerm.Spring : today.Month <= 8 ? AcademicTerm.Summer : AcademicTerm.Fall;
        var model = new AcademicPeriodFormViewModel
        {
            Term = term,
            AcademicYear = today.Year,
            StartDate = today,
            EndDate = today.AddMonths(4).AddDays(-1),
            IsActive = true
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AcademicPeriodFormViewModel model,
        CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _periodService.CreateAsync(ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Term} {model.AcademicYear} was created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The academic term could not be created.");
        }
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _periodService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new AcademicPeriodFormViewModel
        {
            Id = item.Id,
            Term = item.Term,
            AcademicYear = item.AcademicYear,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            RegistrationStartDate = item.RegistrationStartDate,
            RegistrationEndDate = item.RegistrationEndDate,
            Notes = item.Notes,
            IsCurrent = item.IsCurrent,
            IsActive = item.IsActive
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AcademicPeriodFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _periodService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Term} {model.AcademicYear} was updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The academic term could not be updated.");
        }
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrent(int id, CancellationToken cancellationToken)
    {
        var result = await _periodService.SetCurrentAsync(id, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Succeeded
            ? "The current academic term was updated successfully."
            : result.Error ?? "The current academic term could not be changed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _periodService.SetActiveAsync(id, isActive, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Succeeded
            ? isActive ? "Academic term activated successfully." : "Academic term archived successfully."
            : result.Error ?? "The academic term status could not be changed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static AcademicPeriodInput ToInput(AcademicPeriodFormViewModel model) => new(
        model.Term!.Value,
        model.AcademicYear!.Value,
        model.StartDate!.Value,
        model.EndDate!.Value,
        model.RegistrationStartDate,
        model.RegistrationEndDate,
        model.Notes,
        model.IsCurrent,
        model.IsActive);

    private static AcademicPeriodRowViewModel ToRow(AcademicPeriodData item)
    {
        var (status, statusClass) = GetStatus(item);
        return new AcademicPeriodRowViewModel
        {
            Id = item.Id,
            Label = item.Label,
            Term = item.Term,
            AcademicYear = item.AcademicYear,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            RegistrationStartDate = item.RegistrationStartDate,
            RegistrationEndDate = item.RegistrationEndDate,
            IsCurrent = item.IsCurrent,
            IsActive = item.IsActive,
            DisplayStatus = status,
            StatusClass = statusClass
        };
    }

    private static (string Status, string CssClass) GetStatus(AcademicPeriodData item)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!item.IsActive) return ("Inactive", "inactive");
        if (item.IsCurrent) return ("Current", "current");
        if (item.StartDate > today) return ("Upcoming", "upcoming");
        if (item.EndDate < today) return ("Past", "past");
        return ("In progress", "active");
    }

    private void ValidateForm(AcademicPeriodFormViewModel model)
    {
        if (model.Term.HasValue && !Enum.IsDefined(typeof(AcademicTerm), model.Term.Value))
            ModelState.AddModelError(nameof(model.Term), "Select a valid academic term.");
        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate.Value <= model.StartDate.Value)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after the start date.");
        if (model.RegistrationStartDate.HasValue != model.RegistrationEndDate.HasValue)
            ModelState.AddModelError(nameof(model.RegistrationEndDate), "Provide both registration dates or leave both empty.");
        if (model.RegistrationStartDate.HasValue && model.RegistrationEndDate.HasValue &&
            model.RegistrationEndDate.Value < model.RegistrationStartDate.Value)
            ModelState.AddModelError(nameof(model.RegistrationEndDate), "Registration end date must be on or after its start date.");
        if (model.IsCurrent && !model.IsActive)
            ModelState.AddModelError(nameof(model.IsActive), "A current academic term must remain active.");
    }

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "current" => "current",
        "upcoming" => "upcoming",
        "past" => "past",
        "inactive" => "inactive",
        "active" => "active",
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
