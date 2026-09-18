using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class AssessmentsController : Controller
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "deadline", "latest", "course", "status", "created"
        };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IAssessmentService assessmentService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _assessmentService = assessmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? courseId,
        AssessmentStatus? status,
        AssessmentType? type,
        bool overdueOnly = false,
        string sort = "deadline")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = NormalizeSearch(search);
        status = IsDefined(status) ? status : null;
        type = IsDefined(type) ? type : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "deadline";

        if (courseId.HasValue && !await _assessmentService.OwnsCourseAsync(user.Id, courseId.Value))
        {
            courseId = null;
        }

        var assessments = await _assessmentService.GetOwnedAssessmentsAsync(
            user.Id, search, courseId, status, type, overdueOnly, sort);
        var summary = await _assessmentService.GetSummaryAsync(user.Id);

        var model = new AssessmentListViewModel
        {
            Assessments = assessments.Select(ToCard).ToList(),
            CourseOptions = await BuildCourseOptionsAsync(user.Id, courseId),
            Search = search,
            CourseId = courseId,
            Status = status,
            Type = type,
            OverdueOnly = overdueOnly,
            Sort = sort,
            Total = summary.Total,
            Completed = summary.Completed,
            Upcoming = summary.Upcoming,
            Overdue = summary.Overdue,
            DueThisWeek = summary.DueThisWeek
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id);
        if (assessment is null) return NotFound();

        var model = new AssessmentDetailsViewModel { Assessment = ToCard(assessment) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (courseId.HasValue && !await _assessmentService.OwnsCourseAsync(user.Id, courseId.Value))
        {
            courseId = null;
        }

        var model = new AssessmentFormViewModel
        {
            CourseId = courseId,
            AssignedDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateTime.Now.AddDays(7).Date.AddHours(23).AddMinutes(59),
            Type = AssessmentType.Assignment,
            Status = AssessmentStatus.NotStarted,
            Difficulty = AssessmentDifficulty.Medium
        };

        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await ValidateFormAsync(model, user.Id);
        if (!ModelState.IsValid)
        {
            await PopulateCourseOptionsAsync(model, user.Id);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var assessment = new Assessment { ApplicationUserId = user.Id };
        ApplyForm(assessment, model);
        await _assessmentService.AddAsync(assessment);
        await _assessmentService.SaveChangesAsync();

        TempData["AssessmentSuccess"] = $"{assessment.Title} was added successfully.";
        return RedirectToAction(nameof(Details), new { id = assessment.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id);
        if (assessment is null) return NotFound();

        var model = ToForm(assessment);
        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssessmentFormViewModel model)
    {
        if (model.Id != id) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id, trackChanges: true);
        if (assessment is null) return NotFound();

        await ValidateFormAsync(model, user.Id);
        if (!ModelState.IsValid)
        {
            await PopulateCourseOptionsAsync(model, user.Id);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        ApplyForm(assessment, model);
        assessment.UpdatedAt = DateTimeOffset.UtcNow;
        await _assessmentService.SaveChangesAsync();

        TempData["AssessmentSuccess"] = $"{assessment.Title} was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id, trackChanges: true);
        if (assessment is null) return NotFound();

        assessment.Status = AssessmentStatus.Completed;
        assessment.UpdatedAt = DateTimeOffset.UtcNow;
        await _assessmentService.SaveChangesAsync();

        TempData["AssessmentSuccess"] = $"{assessment.Title} was marked completed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id);
        if (assessment is null) return NotFound();

        var model = new AssessmentDeleteViewModel { Assessment = ToCard(assessment) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var assessment = await _assessmentService.GetOwnedAssessmentAsync(user.Id, id, trackChanges: true);
        if (assessment is null) return NotFound();

        var title = assessment.Title;
        _assessmentService.Remove(assessment);
        await _assessmentService.SaveChangesAsync();

        TempData["AssessmentSuccess"] = $"{title} was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(AssessmentFormViewModel model, string userId)
    {
        if (model.CourseId.HasValue && !await _assessmentService.OwnsCourseAsync(userId, model.CourseId.Value))
        {
            ModelState.AddModelError(nameof(model.CourseId), "Please select one of your own courses.");
        }

        ValidateEnum(model.Type, nameof(model.Type), "assessment type");
        ValidateEnum(model.Status, nameof(model.Status), "status");
        ValidateEnum(model.Difficulty, nameof(model.Difficulty), "difficulty");

        if (model.AssignedDate.HasValue && model.DueDate.HasValue &&
            model.DueDate.Value.Date < model.AssignedDate.Value.ToDateTime(TimeOnly.MinValue))
        {
            ModelState.AddModelError(nameof(model.DueDate), "Due date cannot be earlier than the assigned date.");
        }

        if (model.ObtainedMarks.HasValue && !model.TotalMarks.HasValue)
        {
            ModelState.AddModelError(nameof(model.TotalMarks), "Enter total marks before obtained marks.");
        }
        else if (model.ObtainedMarks.HasValue && model.TotalMarks.HasValue &&
                 model.ObtainedMarks.Value > model.TotalMarks.Value)
        {
            ModelState.AddModelError(nameof(model.ObtainedMarks), "Obtained marks cannot exceed total marks.");
        }
    }

    private void ValidateEnum<TEnum>(TEnum? value, string field, string label)
        where TEnum : struct, Enum
    {
        if (value.HasValue && !Enum.IsDefined(typeof(TEnum), value.Value))
        {
            ModelState.AddModelError(field, $"Please select a valid {label}.");
        }
    }

    private async Task PopulateCourseOptionsAsync(AssessmentFormViewModel model, string userId) =>
        model.CourseOptions = await BuildCourseOptionsAsync(userId, model.CourseId);

    private async Task<IReadOnlyList<SelectListItem>> BuildCourseOptionsAsync(string userId, int? selectedId)
    {
        var courses = await _assessmentService.GetCourseOptionsAsync(userId);
        return courses.Select(course => new SelectListItem
        {
            Value = course.Id.ToString(),
            Text = $"{course.CourseCode} — {course.CourseName}",
            Selected = course.Id == selectedId
        }).ToList();
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id)
            .Select(item => new
            {
                item.StudentId,
                item.Department,
                item.Semester,
                HasProfileImage = item.ProfileImageData != null,
                item.CreatedAt,
                item.UpdatedAt
            })
            .SingleOrDefaultAsync();

        if (profile is null) return false;

        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Student" : user.FullName;
        model.FullName = fullName;
        model.Email = user.Email ?? string.Empty;
        model.StudentId = profile.StudentId;
        model.Initials = CreateInitials(fullName);
        model.DepartmentLabel = profile.Department ?? "Department not set";
        model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set";
        model.HasProfileImage = profile.HasProfileImage;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();
        return true;
    }

    private static void ApplyForm(Assessment assessment, AssessmentFormViewModel model)
    {
        assessment.CourseId = model.CourseId!.Value;
        assessment.Title = model.Title.Trim();
        assessment.Type = model.Type!.Value;
        assessment.Description = NormalizeOptional(model.Description);
        assessment.AssignedDate = model.AssignedDate!.Value;
        assessment.DueDate = DateTime.SpecifyKind(model.DueDate!.Value, DateTimeKind.Unspecified);
        assessment.TotalMarks = model.TotalMarks;
        assessment.ObtainedMarks = model.ObtainedMarks;
        assessment.WeightPercentage = model.WeightPercentage;
        assessment.Status = model.Status!.Value;
        assessment.Difficulty = model.Difficulty!.Value;
        assessment.EstimatedStudyHours = model.EstimatedStudyHours;
        assessment.Notes = NormalizeOptional(model.Notes);
    }

    private static AssessmentFormViewModel ToForm(Assessment item) => new()
    {
        Id = item.Id,
        CourseId = item.CourseId,
        Title = item.Title,
        Type = item.Type,
        Description = item.Description,
        AssignedDate = item.AssignedDate,
        DueDate = item.DueDate,
        TotalMarks = item.TotalMarks,
        ObtainedMarks = item.ObtainedMarks,
        WeightPercentage = item.WeightPercentage,
        Status = item.Status,
        Difficulty = item.Difficulty,
        EstimatedStudyHours = item.EstimatedStudyHours,
        Notes = item.Notes
    };

    private static AssessmentCardViewModel ToCard(Assessment item) => new()
    {
        Id = item.Id,
        CourseId = item.CourseId,
        CourseCode = item.Course.CourseCode,
        CourseName = item.Course.CourseName,
        Title = item.Title,
        Type = item.Type,
        Description = item.Description,
        AssignedDate = item.AssignedDate,
        DueDate = item.DueDate,
        TotalMarks = item.TotalMarks,
        ObtainedMarks = item.ObtainedMarks,
        WeightPercentage = item.WeightPercentage,
        Status = item.Status,
        Difficulty = item.Difficulty,
        EstimatedStudyHours = item.EstimatedStudyHours,
        Notes = item.Notes
    };

    private static bool IsDefined<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        !value.HasValue || Enum.IsDefined(typeof(TEnum), value.Value);

    private static string? NormalizeSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= 100 ? normalized : normalized[..100];
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ST";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
