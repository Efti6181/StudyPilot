using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class CoursesController : Controller
{
    private static readonly HashSet<string> AllowedGrades =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "D", "F"
        };

    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "code", "name", "credits", "progress", "newest"
        };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICourseService _courseService;

    public CoursesController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ICourseService courseService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CourseStatus? status,
        CourseType? type,
        string sort = "code")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        search = NormalizeSearch(search);
        status = IsDefined(status) ? status : null;
        type = IsDefined(type) ? type : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "code";

        var courses = await _courseService.GetOwnedCoursesAsync(
            user.Id,
            search,
            status,
            type,
            sort);
        var summary = await _courseService.GetSummaryAsync(user.Id);

        var model = new CourseListViewModel
        {
            Courses = courses.Select(ToCard).ToList(),
            Search = search,
            Status = status,
            Type = type,
            Sort = sort,
            ActiveCourses = summary.ActiveCourses,
            ActiveCredits = summary.ActiveCredits,
            AverageProgress = summary.AverageProgress,
            NeedsAttention = summary.NeedsAttention
        };

        if (!await PopulateShellAsync(model, user))
        {
            return MissingStudentProfile();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var course = await _courseService.GetOwnedCourseAsync(user.Id, id);
        if (course is null)
        {
            return NotFound();
        }

        var model = new CourseDetailsViewModel
        {
            Course = ToCard(course),
            AssessmentCount = await _courseService.GetAssessmentCountAsync(user.Id, id)
        };
        if (!await PopulateShellAsync(model, user))
        {
            return MissingStudentProfile();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new CourseFormViewModel
        {
            AcademicYear = DateTime.UtcNow.Year,
            AcademicTerm = GetCurrentAcademicTerm(),
            CourseType = StudyPilotApp.Models.CourseType.Theory,
            Status = CourseStatus.Active,
            Color = CourseColor.Blue,
            ProgressPercentage = 0
        };

        if (!await PopulateShellAsync(model, user, useProfileSemester: true))
        {
            return MissingStudentProfile();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ValidateCourseOptions(model);

        if (ModelState.IsValid && await IsDuplicateAsync(user.Id, model))
        {
            ModelState.AddModelError(
                nameof(model.CourseCode),
                "This course already exists for the selected semester and academic term.");
        }

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user))
            {
                return MissingStudentProfile();
            }

            return View(model);
        }

        var course = new Course { ApplicationUserId = user.Id };
        ApplyForm(course, model);
        await _courseService.AddAsync(course);

        try
        {
            await _courseService.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The course could not be saved. Check that it is not a duplicate and try again.");
            await PopulateShellAsync(model, user);
            return View(model);
        }

        TempData["CourseSuccess"] = $"{course.CourseCode} was added successfully.";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var course = await _courseService.GetOwnedCourseAsync(user.Id, id);
        if (course is null)
        {
            return NotFound();
        }

        var model = ToForm(course);
        if (!await PopulateShellAsync(model, user))
        {
            return MissingStudentProfile();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var course = await _courseService.GetOwnedCourseAsync(user.Id, id, trackChanges: true);
        if (course is null)
        {
            return NotFound();
        }

        ValidateCourseOptions(model);

        if (ModelState.IsValid && await IsDuplicateAsync(user.Id, model, id))
        {
            ModelState.AddModelError(
                nameof(model.CourseCode),
                "This course already exists for the selected semester and academic term.");
        }

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user))
            {
                return MissingStudentProfile();
            }

            return View(model);
        }

        ApplyForm(course, model);
        course.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _courseService.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The course could not be updated. Check that it is not a duplicate and try again.");
            await PopulateShellAsync(model, user);
            return View(model);
        }

        TempData["CourseSuccess"] = $"{course.CourseCode} was updated successfully.";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var course = await _courseService.GetOwnedCourseAsync(user.Id, id);
        if (course is null)
        {
            return NotFound();
        }

        var model = new CourseDeleteViewModel
        {
            Course = ToCard(course),
            RelatedAssessmentCount = await _courseService.GetAssessmentCountAsync(user.Id, id)
        };
        if (!await PopulateShellAsync(model, user))
        {
            return MissingStudentProfile();
        }

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var course = await _courseService.GetOwnedCourseAsync(user.Id, id, trackChanges: true);
        if (course is null)
        {
            return NotFound();
        }

        var relatedAssessmentCount = await _courseService.GetAssessmentCountAsync(user.Id, id);
        if (relatedAssessmentCount > 0)
        {
            TempData["CourseError"] =
                $"This course has {relatedAssessmentCount} assessment(s). Delete or move them before deleting the course.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        _courseService.Remove(course);
        await _courseService.SaveChangesAsync();

        TempData["CourseSuccess"] = $"{course.CourseCode} was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PopulateShellAsync(
        StudentShellViewModel model,
        ApplicationUser user,
        bool useProfileSemester = false)
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

        if (profile is null)
        {
            return false;
        }

        var fullName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.Email ?? "Student"
            : user.FullName;

        model.FullName = fullName;
        model.Email = user.Email ?? string.Empty;
        model.StudentId = profile.StudentId;
        model.Initials = CreateInitials(fullName);
        model.DepartmentLabel = profile.Department ?? "Department not set";
        model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set";
        model.HasProfileImage = profile.HasProfileImage;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();

        if (useProfileSemester && model is CourseFormViewModel form && !form.Semester.HasValue)
        {
            form.Semester = profile.Semester;
        }

        return true;
    }

    private void ValidateCourseOptions(CourseFormViewModel model)
    {
        if (model.AcademicTerm.HasValue && !Enum.IsDefined(typeof(AcademicTerm), model.AcademicTerm.Value))
        {
            ModelState.AddModelError(nameof(model.AcademicTerm), "Please select a valid academic term.");
        }

        if (model.CourseType.HasValue && !Enum.IsDefined(typeof(CourseType), model.CourseType.Value))
        {
            ModelState.AddModelError(nameof(model.CourseType), "Please select a valid course type.");
        }

        if (model.Status.HasValue && !Enum.IsDefined(typeof(CourseStatus), model.Status.Value))
        {
            ModelState.AddModelError(nameof(model.Status), "Please select a valid status.");
        }

        if (model.Color.HasValue && !Enum.IsDefined(typeof(CourseColor), model.Color.Value))
        {
            ModelState.AddModelError(nameof(model.Color), "Please select a valid color.");
        }

        model.TargetGrade = NormalizeOptional(model.TargetGrade)?.ToUpperInvariant();
        if (model.TargetGrade is not null && !AllowedGrades.Contains(model.TargetGrade))
        {
            ModelState.AddModelError(nameof(model.TargetGrade), "Please select a valid target grade.");
        }
    }

    private Task<bool> IsDuplicateAsync(string userId, CourseFormViewModel model, int? excludedId = null) =>
        _courseService.DuplicateExistsAsync(
            userId,
            model.CourseCode,
            model.Semester!.Value,
            model.AcademicTerm!.Value,
            model.AcademicYear!.Value,
            excludedId);

    private static void ApplyForm(Course course, CourseFormViewModel model)
    {
        course.CourseCode = model.CourseCode.Trim().ToUpperInvariant();
        course.CourseName = model.CourseName.Trim();
        course.CreditHours = model.CreditHours!.Value;
        course.Instructor = NormalizeOptional(model.Instructor);
        course.Semester = model.Semester!.Value;
        course.AcademicTerm = model.AcademicTerm!.Value;
        course.AcademicYear = model.AcademicYear!.Value;
        course.CourseType = model.CourseType!.Value;
        course.Description = NormalizeOptional(model.Description);
        course.TargetGrade = NormalizeOptional(model.TargetGrade)?.ToUpperInvariant();
        course.Status = model.Status!.Value;
        course.Color = model.Color!.Value;
        course.ProgressPercentage = model.ProgressPercentage!.Value;
    }

    private static CourseFormViewModel ToForm(Course course) => new()
    {
        Id = course.Id,
        CourseCode = course.CourseCode,
        CourseName = course.CourseName,
        CreditHours = course.CreditHours,
        Instructor = course.Instructor,
        Semester = course.Semester,
        AcademicTerm = course.AcademicTerm,
        AcademicYear = course.AcademicYear,
        CourseType = course.CourseType,
        Description = course.Description,
        TargetGrade = course.TargetGrade,
        Status = course.Status,
        Color = course.Color,
        ProgressPercentage = course.ProgressPercentage
    };

    private static CourseCardViewModel ToCard(Course course) => new()
    {
        Id = course.Id,
        CourseCode = course.CourseCode,
        CourseName = course.CourseName,
        CreditHours = course.CreditHours,
        Instructor = course.Instructor ?? "Instructor not set",
        InstructorInitials = CreateInitials(course.Instructor ?? "Instructor"),
        Semester = course.Semester,
        AcademicTerm = course.AcademicTerm,
        AcademicYear = course.AcademicYear,
        CourseType = course.CourseType,
        Status = course.Status,
        Color = course.Color,
        ProgressPercentage = course.ProgressPercentage,
        TargetGrade = course.TargetGrade,
        Description = course.Description
    };

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var normalized = search.Trim();
        return normalized.Length <= 100 ? normalized : normalized[..100];
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsDefined<TEnum>(TEnum? value)
        where TEnum : struct, Enum =>
        !value.HasValue || Enum.IsDefined(typeof(TEnum), value.Value);

    private static AcademicTerm GetCurrentAcademicTerm()
    {
        var month = DateTime.UtcNow.Month;
        return month <= 4 ? AcademicTerm.Spring : month <= 8 ? AcademicTerm.Summer : AcademicTerm.Fall;
    }

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "IN";
        }

        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
