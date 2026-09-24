using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CourseCatalogController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICourseCatalogService _catalogService;
    private readonly ApplicationDbContext _dbContext;

    public CourseCatalogController(
        UserManager<ApplicationUser> userManager,
        ICourseCatalogService catalogService,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _catalogService = catalogService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? departmentId,
        int? programId,
        CourseType? courseType,
        CourseDifficultyLevel? difficulty,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = CleanSearch(search);
        status = NormalizeStatus(status);
        if (courseType.HasValue && !Enum.IsDefined(typeof(CourseType), courseType.Value)) courseType = null;
        if (difficulty.HasValue && !Enum.IsDefined(typeof(CourseDifficultyLevel), difficulty.Value)) difficulty = null;

        var departments = await _catalogService.GetDepartmentsAsync(false, cancellationToken: cancellationToken);
        if (departmentId.HasValue && departments.All(item => item.Id != departmentId.Value)) departmentId = null;
        var programs = await _catalogService.GetProgramsAsync(departmentId, false, cancellationToken: cancellationToken);
        if (programId.HasValue && programs.All(item => item.Id != programId.Value)) programId = null;
        bool? isActive = status switch { "active" => true, "inactive" => false, _ => null };

        var data = await _catalogService.SearchAsync(
            search, departmentId, programId, courseType, difficulty, isActive,
            page, PageSize, cancellationToken);
        var model = new CourseCatalogIndexViewModel
        {
            Search = search,
            DepartmentId = departmentId,
            ProgramId = programId,
            CourseType = courseType,
            Difficulty = difficulty,
            Status = status,
            TotalCount = data.TotalCount,
            ActiveCount = data.ActiveCount,
            InactiveCount = data.InactiveCount,
            CoreCount = data.CoreCount,
            HardCount = data.HardCount,
            FilteredCount = data.FilteredCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            Courses = data.Items.Select(ToRow).ToList(),
            DepartmentOptions = departments.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}", item.Id.ToString(), item.Id == departmentId)).ToList(),
            ProgramOptions = programs.Select(item => new SelectListItem(
                $"{item.Code} — {item.Name}", item.Id.ToString(), item.Id == programId)).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _catalogService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = ToDetails(item);
        await PopulateFacultyAssignmentsAsync(model, cancellationToken);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? departmentId, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var model = new CourseCatalogFormViewModel
        {
            DepartmentId = departmentId ?? 0,
            CourseType = CourseType.Theory,
            Difficulty = CourseDifficultyLevel.Moderate,
            CareerRelevance = CareerRelevanceLevel.Medium
        };
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseCatalogFormViewModel model, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateEnums(model);
        if (ModelState.IsValid)
        {
            var result = await _catalogService.CreateAsync(ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Code.Trim().ToUpperInvariant()} was added to the catalog.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The catalog course could not be created.");
        }
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _catalogService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new CourseCatalogFormViewModel
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            DepartmentId = item.DepartmentId,
            ProgramId = item.ProgramId,
            CreditHours = item.CreditHours,
            CourseType = item.CourseType,
            RecommendedSemester = item.RecommendedSemester,
            Difficulty = item.Difficulty,
            CareerRelevance = item.CareerRelevance,
            Description = item.Description,
            Prerequisites = item.Prerequisites,
            IsActive = item.IsActive
        };
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseCatalogFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateEnums(model);
        if (ModelState.IsValid)
        {
            var result = await _catalogService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = $"{model.Code.Trim().ToUpperInvariant()} was updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The catalog course could not be updated.");
        }
        await PopulateOptionsAsync(model, cancellationToken);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _catalogService.SetActiveAsync(id, isActive, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Succeeded
            ? isActive ? "Catalog course activated successfully." : "Catalog course archived successfully."
            : result.Error ?? "The catalog course status could not be changed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFaculty(
        int id, int facultyProfileId, int academicPeriodId, string section,
        CancellationToken cancellationToken)
    {
        section = (section ?? string.Empty).Trim().ToUpperInvariant();
        if (section.Length is < 1 or > 30 || section.Any(character =>
                !char.IsLetterOrDigit(character) && character != '-' && character != ' '))
        {
            TempData["AdminError"] = "Enter a valid section using letters, numbers, spaces, or hyphens.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var courseExists = await _dbContext.CatalogCourses.AnyAsync(item => item.Id == id && item.IsActive, cancellationToken);
        var facultyExists = await _dbContext.FacultyProfiles.AnyAsync(item => item.Id == facultyProfileId && item.ApplicationUser.IsActive, cancellationToken);
        var periodExists = await _dbContext.AcademicPeriods.AnyAsync(item => item.Id == academicPeriodId && item.IsActive, cancellationToken);
        if (!courseExists || !facultyExists || !periodExists)
        {
            TempData["AdminError"] = "Select a valid active course, faculty member, and academic term.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var existing = await _dbContext.FacultyCourseAssignments.SingleOrDefaultAsync(item =>
            item.FacultyProfileId == facultyProfileId && item.CatalogCourseId == id &&
            item.AcademicPeriodId == academicPeriodId && item.Section == section, cancellationToken);
        if (existing is not null)
        {
            if (existing.IsActive)
            {
                TempData["AdminError"] = "This active faculty assignment already exists.";
                return RedirectToAction(nameof(Details), new { id });
            }
            existing.IsActive = true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["AdminSuccess"] = "The existing faculty assignment was reactivated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _dbContext.FacultyCourseAssignments.Add(new FacultyCourseAssignment
        {
            FacultyProfileId = facultyProfileId, CatalogCourseId = id,
            AcademicPeriodId = academicPeriodId, Section = section
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = "Faculty assigned to the course successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFacultyAssignmentStatus(int id, int assignmentId, bool isActive, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.FacultyCourseAssignments
            .SingleOrDefaultAsync(item => item.Id == assignmentId && item.CatalogCourseId == id, cancellationToken);
        if (assignment is null) return NotFound();
        assignment.IsActive = isActive;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["AdminSuccess"] = isActive ? "Faculty assignment activated." : "Faculty assignment deactivated. Existing Student records are preserved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Programs(int departmentId, CancellationToken cancellationToken)
    {
        if (departmentId <= 0) return Json(Array.Empty<object>());
        var items = await _catalogService.GetProgramsAsync(departmentId, true, cancellationToken: cancellationToken);
        return Json(items.Select(item => new { item.Id, label = $"{item.Code} — {item.Name}" }));
    }

    private async Task PopulateOptionsAsync(CourseCatalogFormViewModel model, CancellationToken cancellationToken)
    {
        var departments = await _catalogService.GetDepartmentsAsync(
            true, model.DepartmentId > 0 ? model.DepartmentId : null, cancellationToken);
        var programs = await _catalogService.GetProgramsAsync(
            model.DepartmentId > 0 ? model.DepartmentId : null,
            true, model.ProgramId, cancellationToken);
        model.DepartmentOptions = departments.Select(item => new SelectListItem(
            $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
            item.Id.ToString(), item.Id == model.DepartmentId)).ToList();
        model.ProgramOptions = programs.Select(item => new SelectListItem(
            $"{item.Code} — {item.Name}{(item.IsActive ? string.Empty : " (Inactive)")}",
            item.Id.ToString(), item.Id == model.ProgramId)).ToList();
    }

    private async Task PopulateFacultyAssignmentsAsync(CourseCatalogDetailsViewModel model, CancellationToken cancellationToken)
    {
        var assignmentRows = await _dbContext.FacultyCourseAssignments.AsNoTracking()
            .Where(item => item.CatalogCourseId == model.Id)
            .OrderByDescending(item => item.AcademicPeriod.IsCurrent)
            .ThenBy(item => item.Section)
            .Select(item => new
            {
                item.Id,
                FacultyName = item.FacultyProfile.ApplicationUser.FullName,
                item.FacultyProfile.FacultyId,
                Department = item.FacultyProfile.Department == null ? "Not assigned" : item.FacultyProfile.Department.Code,
                item.AcademicPeriod.Term,
                item.AcademicPeriod.AcademicYear,
                item.Section,
                item.IsActive
            }).ToListAsync(cancellationToken);
        model.FacultyAssignments = assignmentRows.Select(item => new FacultyCourseAssignmentAdminViewModel
        {
            Id = item.Id,
            FacultyName = item.FacultyName,
            FacultyId = item.FacultyId,
            Department = item.Department,
            Period = $"{item.Term} {item.AcademicYear}",
            Section = item.Section,
            IsActive = item.IsActive
        }).ToList();
        var facultyOptions = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(item => item.ApplicationUser.IsActive)
            .OrderBy(item => item.ApplicationUser.FullName)
            .Select(item => new { item.Id, item.FacultyId, item.ApplicationUser.FullName })
            .ToListAsync(cancellationToken);
        model.FacultyOptions = facultyOptions.Select(item => new SelectListItem(
            $"{item.FacultyId} — {item.FullName}", item.Id.ToString())).ToList();

        var periodOptions = await _dbContext.AcademicPeriods.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.AcademicYear)
            .Select(item => new { item.Id, item.Term, item.AcademicYear, item.IsCurrent })
            .ToListAsync(cancellationToken);
        model.AcademicPeriodOptions = periodOptions.Select(item => new SelectListItem(
            $"{item.Term} {item.AcademicYear}{(item.IsCurrent ? " (Current)" : string.Empty)}",
            item.Id.ToString())).ToList();
    }

    private void ValidateEnums(CourseCatalogFormViewModel model)
    {
        if (model.CourseType.HasValue && !Enum.IsDefined(typeof(CourseType), model.CourseType.Value))
            ModelState.AddModelError(nameof(model.CourseType), "Select a valid course type.");
        if (model.Difficulty.HasValue && !Enum.IsDefined(typeof(CourseDifficultyLevel), model.Difficulty.Value))
            ModelState.AddModelError(nameof(model.Difficulty), "Select a valid difficulty.");
        if (model.CareerRelevance.HasValue && !Enum.IsDefined(typeof(CareerRelevanceLevel), model.CareerRelevance.Value))
            ModelState.AddModelError(nameof(model.CareerRelevance), "Select valid career relevance.");
    }

    private static CatalogCourseInput ToInput(CourseCatalogFormViewModel model) => new(
        model.Code, model.Name, model.DepartmentId, model.ProgramId, model.CreditHours,
        model.CourseType!.Value, model.RecommendedSemester, model.Difficulty!.Value,
        model.CareerRelevance!.Value, model.Description, model.Prerequisites, model.IsActive);

    private static CourseCatalogRowViewModel ToRow(CatalogCourseData item) => new()
    {
        Id = item.Id, Code = item.Code, Name = item.Name,
        DepartmentCode = item.DepartmentCode, DepartmentName = item.DepartmentName,
        ProgramCode = item.ProgramCode, ProgramName = item.ProgramName,
        CreditHours = item.CreditHours, CourseType = item.CourseType,
        RecommendedSemester = item.RecommendedSemester, Difficulty = item.Difficulty,
        CareerRelevance = item.CareerRelevance, IsActive = item.IsActive
    };

    private static CourseCatalogDetailsViewModel ToDetails(CatalogCourseData item) => new()
    {
        Id = item.Id, Code = item.Code, Name = item.Name,
        DepartmentId = item.DepartmentId, DepartmentCode = item.DepartmentCode,
        DepartmentName = item.DepartmentName, ProgramId = item.ProgramId,
        ProgramCode = item.ProgramCode, ProgramName = item.ProgramName,
        CreditHours = item.CreditHours, CourseType = item.CourseType,
        RecommendedSemester = item.RecommendedSemester, Difficulty = item.Difficulty,
        CareerRelevance = item.CareerRelevance, Description = item.Description,
        Prerequisites = item.Prerequisites, IsActive = item.IsActive,
        CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt
    };

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" => "active", "inactive" => "inactive", _ => "all"
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
        return parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant() : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
