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
public sealed class PrioritiesController : Controller
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "priority", "deadline", "course", "credits" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IPriorityService _priorityService;
    private readonly IGpaService _gpaService;

    public PrioritiesController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IPriorityService priorityService,
        IGpaService gpaService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _priorityService = priorityService;
        _gpaService = gpaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        PriorityLevel? level,
        string sort = "priority")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await EnsureDependenciesAsync(user.Id);
        search = NormalizeSearch(search);
        level = IsDefined(level) ? level : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "priority";

        var settings = await _priorityService.GetSettingsAsync(user.Id);
        var scale = await _priorityService.GetGradingScaleAsync(user.Id);
        var courses = await _priorityService.GetOwnedCoursesAsync(user.Id);
        var all = courses.Select(course => BuildPriority(course, settings, scale)).ToList();

        IEnumerable<PriorityCourseViewModel> filtered = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(item =>
                item.CourseCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.CourseName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        if (level.HasValue)
            filtered = filtered.Where(item => item.DisplayLevel == level.Value);

        filtered = sort switch
        {
            "deadline" => filtered.OrderBy(item => item.NearestDeadline ?? DateTime.MaxValue),
            "course" => filtered.OrderBy(item => item.CourseCode),
            "credits" => filtered.OrderByDescending(item => item.CreditHours).ThenByDescending(item => item.Score),
            _ => filtered.OrderByDescending(item => item.IsPinned)
                .ThenByDescending(item => item.DisplayLevel)
                .ThenByDescending(item => item.Score)
                .ThenBy(item => item.NearestDeadline ?? DateTime.MaxValue)
        };

        var model = new PriorityListViewModel
        {
            Courses = filtered.ToList(),
            TodayFocus = all.OrderByDescending(item => item.IsPinned)
                .ThenByDescending(item => item.DisplayLevel)
                .ThenByDescending(item => item.Score)
                .ThenBy(item => item.NearestDeadline ?? DateTime.MaxValue)
                .Take(3)
                .ToList(),
            Search = search,
            Level = level,
            Sort = sort,
            CriticalCount = all.Count(item => item.DisplayLevel == PriorityLevel.Critical),
            HighCount = all.Count(item => item.DisplayLevel == PriorityLevel.High),
            DueThisWeekCount = all.Count(item => item.DaysUntilDeadline is >= 0 and <= 7),
            PendingAssessmentCount = all.Sum(item => item.PendingAssessments)
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await EnsureDependenciesAsync(user.Id);

        var course = await _priorityService.GetOwnedCourseAsync(user.Id, id);
        if (course is null) return NotFound();
        var settings = await _priorityService.GetSettingsAsync(user.Id);
        var scale = await _priorityService.GetGradingScaleAsync(user.Id);
        var model = new PriorityDetailsViewModel
        {
            Priority = BuildPriority(course, settings, scale),
            Weights = ToSettingsViewModel(settings)
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Configure(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var course = await _priorityService.GetOwnedCourseAsync(user.Id, id);
        if (course is null) return NotFound();
        var preference = course.PriorityPreference;
        var pending = PendingAssessments(course);

        var model = new PriorityConfigureViewModel
        {
            CourseId = course.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            CourseProgress = course.ProgressPercentage,
            PendingAssessments = pending.Count,
            NearestDeadline = pending.OrderBy(item => item.DueDate).FirstOrDefault()?.DueDate,
            AssessmentAverage = AssessmentAverage(course),
            ConfidenceRating = preference?.ConfidenceRating ?? 3,
            TopicCompletionPercentage = preference?.TopicCompletionPercentage ?? course.ProgressPercentage,
            WorkloadRisk = preference?.WorkloadRisk ?? 3,
            AvailableStudyHoursPerWeek = preference?.AvailableStudyHoursPerWeek ?? 5m,
            ManualPriorityLevel = preference?.ManualPriorityLevel,
            IsPinned = preference?.IsPinned ?? false,
            Notes = preference?.Notes
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(int id, PriorityConfigureViewModel model)
    {
        if (model.CourseId != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var course = await _priorityService.GetOwnedCourseAsync(user.Id, id);
        if (course is null) return NotFound();

        if (model.ManualPriorityLevel.HasValue && !Enum.IsDefined(typeof(PriorityLevel), model.ManualPriorityLevel.Value))
            ModelState.AddModelError(nameof(model.ManualPriorityLevel), "Select a valid priority level.");

        if (!ModelState.IsValid)
        {
            PopulateConfigureEvidence(model, course);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var preference = await _priorityService.GetPreferenceAsync(user.Id, id, trackChanges: true);
        if (preference is null)
        {
            preference = new CoursePriorityPreference
            {
                ApplicationUserId = user.Id,
                CourseId = id
            };
            await _priorityService.AddPreferenceAsync(preference);
        }

        preference.ConfidenceRating = model.ConfidenceRating;
        preference.TopicCompletionPercentage = model.TopicCompletionPercentage;
        preference.WorkloadRisk = model.WorkloadRisk;
        preference.AvailableStudyHoursPerWeek = model.AvailableStudyHoursPerWeek;
        preference.ManualPriorityLevel = model.ManualPriorityLevel;
        preference.IsPinned = model.IsPinned;
        preference.Notes = NormalizeOptional(model.Notes);
        preference.UpdatedAt = DateTimeOffset.UtcNow;
        await _priorityService.SaveChangesAsync();

        TempData["PrioritySuccess"] = $"Priority inputs for {course.CourseCode} were updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePin(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var course = await _priorityService.GetOwnedCourseAsync(user.Id, id);
        if (course is null) return NotFound();

        var preference = await _priorityService.GetPreferenceAsync(user.Id, id, trackChanges: true);
        if (preference is null)
        {
            preference = DefaultPreference(user.Id, course);
            preference.IsPinned = true;
            await _priorityService.AddPreferenceAsync(preference);
        }
        else
        {
            preference.IsPinned = !preference.IsPinned;
            preference.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await _priorityService.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearOverride(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (await _priorityService.GetOwnedCourseAsync(user.Id, id) is null) return NotFound();
        var preference = await _priorityService.GetPreferenceAsync(user.Id, id, trackChanges: true);
        if (preference is not null)
        {
            preference.ManualPriorityLevel = null;
            preference.UpdatedAt = DateTimeOffset.UtcNow;
            await _priorityService.SaveChangesAsync();
        }
        TempData["PrioritySuccess"] = "Manual override cleared. The calculated level is active again.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (await _priorityService.GetOwnedCourseAsync(user.Id, id) is null) return NotFound();
        var preference = await _priorityService.GetPreferenceAsync(user.Id, id, trackChanges: true);
        if (preference is not null)
        {
            _priorityService.RemovePreference(preference);
            await _priorityService.SaveChangesAsync();
        }
        TempData["PrioritySuccess"] = "Course priority inputs were reset to automatic defaults.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _priorityService.EnsureSettingsAsync(user.Id);
        var model = ToSettingsViewModel(await _priorityService.GetSettingsAsync(user.Id));
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(PriorityWeightSettingsViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _priorityService.EnsureSettingsAsync(user.Id);
        if (model.TotalWeight != 100m)
            ModelState.AddModelError(string.Empty, $"Priority weights must total exactly 100%. Current total: {model.TotalWeight:0.##}%.");

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var settings = await _priorityService.GetSettingsAsync(user.Id, trackChanges: true);
        settings.TargetGradeGapWeight = model.TargetGradeGapWeight;
        settings.AssessmentUrgencyWeight = model.AssessmentUrgencyWeight;
        settings.CourseCreditWeight = model.CourseCreditWeight;
        settings.WeaknessWeight = model.WeaknessWeight;
        settings.IncompleteTopicsWeight = model.IncompleteTopicsWeight;
        settings.WorkloadRiskWeight = model.WorkloadRiskWeight;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _priorityService.SaveChangesAsync();
        TempData["PrioritySuccess"] = "Priority engine weights were updated.";
        return RedirectToAction(nameof(Settings));
    }

    private static PriorityCourseViewModel BuildPriority(
        Course course,
        PriorityWeightSettings settings,
        IReadOnlyList<GradingScaleEntry> scale)
    {
        var preference = course.PriorityPreference;
        var confidence = preference?.ConfidenceRating ?? 3;
        var topicCompletion = preference?.TopicCompletionPercentage ?? course.ProgressPercentage;
        var workloadRisk = preference?.WorkloadRisk ?? 3;
        var availableHours = preference?.AvailableStudyHoursPerWeek ?? 5m;
        var pending = PendingAssessments(course);
        var nearest = pending.OrderBy(item => item.DueDate).FirstOrDefault()?.DueDate;
        var average = AssessmentAverage(course);

        var factors = new PriorityFactorValues(
            TargetGradeGapFactor(course, scale, average),
            UrgencyFactor(nearest),
            Math.Clamp(course.CreditHours / 6m * 100m, 0m, 100m),
            WeaknessFactor(confidence, average, course.ProgressPercentage),
            100m - topicCompletion,
            (workloadRisk - 1m) / 4m * 100m);

        var weights = new PriorityWeightValues(
            settings.TargetGradeGapWeight,
            settings.AssessmentUrgencyWeight,
            settings.CourseCreditWeight,
            settings.WeaknessWeight,
            settings.IncompleteTopicsWeight,
            settings.WorkloadRiskWeight);
        var calculation = PriorityCalculator.Calculate(
            factors,
            weights,
            preference?.ManualPriorityLevel,
            availableHours);

        return new PriorityCourseViewModel
        {
            CourseId = course.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            CreditHours = course.CreditHours,
            CourseProgress = course.ProgressPercentage,
            TargetGrade = course.TargetGrade,
            Score = calculation.Score,
            CalculatedLevel = calculation.CalculatedLevel,
            DisplayLevel = calculation.DisplayLevel,
            IsManualOverride = calculation.IsManualOverride,
            IsPinned = preference?.IsPinned ?? false,
            SuggestedStudyMinutes = calculation.SuggestedStudyMinutes,
            PendingAssessments = pending.Count,
            NearestDeadline = nearest,
            AssessmentAverage = average,
            ConfidenceRating = confidence,
            TopicCompletionPercentage = topicCompletion,
            WorkloadRisk = workloadRisk,
            AvailableStudyHoursPerWeek = availableHours,
            Notes = preference?.Notes,
            Factors = new PriorityFactorViewModel
            {
                TargetGradeGap = Math.Round(factors.TargetGradeGap, 1),
                AssessmentUrgency = Math.Round(factors.AssessmentUrgency, 1),
                CourseCredit = Math.Round(factors.CourseCredit, 1),
                Weakness = Math.Round(factors.Weakness, 1),
                IncompleteTopics = Math.Round(factors.IncompleteTopics, 1),
                WorkloadRisk = Math.Round(factors.WorkloadRisk, 1)
            },
            Reasons = BuildReasons(course, factors, nearest, average)
        };
    }

    private static List<Assessment> PendingAssessments(Course course) =>
        course.Assessments
            .Where(item => item.Status != AssessmentStatus.Completed)
            .ToList();

    private static decimal? AssessmentAverage(Course course)
    {
        var marked = course.Assessments
            .Where(item => item.TotalMarks.HasValue && item.TotalMarks.Value > 0 && item.ObtainedMarks.HasValue)
            .ToList();
        if (marked.Count == 0) return null;

        var weighted = marked.Where(item => item.WeightPercentage.HasValue && item.WeightPercentage.Value > 0).ToList();
        decimal value;
        if (weighted.Count > 0)
        {
            var weightTotal = weighted.Sum(item => item.WeightPercentage!.Value);
            value = weighted.Sum(item =>
                item.ObtainedMarks!.Value / item.TotalMarks!.Value * item.WeightPercentage!.Value) /
                weightTotal * 100m;
        }
        else
        {
            value = marked.Average(item => item.ObtainedMarks!.Value / item.TotalMarks!.Value * 100m);
        }
        return Math.Round(Math.Clamp(value, 0m, 100m), 1);
    }

    private static decimal TargetGradeGapFactor(
        Course course,
        IReadOnlyList<GradingScaleEntry> scale,
        decimal? assessmentAverage)
    {
        var target = scale.FirstOrDefault(item =>
            string.Equals(item.LetterGrade, course.TargetGrade, StringComparison.OrdinalIgnoreCase));
        if (target is null) return 100m - course.ProgressPercentage;

        var actualLetter = course.CourseGrades
            .Where(item => !string.IsNullOrWhiteSpace(item.ActualLetterGrade))
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.ActualLetterGrade)
            .FirstOrDefault();
        var actual = scale.FirstOrDefault(item =>
            string.Equals(item.LetterGrade, actualLetter, StringComparison.OrdinalIgnoreCase));
        var maximumPoint = scale.Count == 0 ? 4m : Math.Max(0.01m, scale.Max(item => item.GradePoint));
        if (actual is not null)
            return Math.Clamp((target.GradePoint - actual.GradePoint) / maximumPoint * 100m, 0m, 100m);
        if (assessmentAverage.HasValue)
            return Math.Clamp(target.MinimumPercentage - assessmentAverage.Value, 0m, 100m);
        return 100m - course.ProgressPercentage;
    }

    private static decimal UrgencyFactor(DateTime? deadline)
    {
        if (!deadline.HasValue) return 5m;
        var days = (deadline.Value - DateTime.Now).TotalDays;
        return days switch
        {
            < 0 => 100m,
            <= 1 => 95m,
            <= 3 => 80m,
            <= 7 => 65m,
            <= 14 => 40m,
            <= 30 => 20m,
            _ => 5m
        };
    }

    private static decimal WeaknessFactor(int confidence, decimal? average, int courseProgress)
    {
        var confidenceWeakness = (5m - confidence) / 4m * 100m;
        var performanceWeakness = 100m - (average ?? courseProgress);
        return Math.Clamp((confidenceWeakness + performanceWeakness) / 2m, 0m, 100m);
    }

    private static IReadOnlyList<string> BuildReasons(
        Course course,
        PriorityFactorValues factors,
        DateTime? deadline,
        decimal? average)
    {
        var reasons = new List<(decimal Value, string Message)>
        {
            (factors.TargetGradeGap, course.TargetGrade is null ? "Course progress leaves room for improvement." : $"Performance is being compared with target grade {course.TargetGrade}."),
            (factors.AssessmentUrgency, deadline.HasValue ? (deadline.Value < DateTime.Now ? "An unfinished assessment is overdue." : $"Nearest assessment is due {deadline.Value:dd MMM}.") : "No immediate assessment deadline."),
            (factors.CourseCredit, $"This is a {course.CreditHours:0.#}-credit course."),
            (factors.Weakness, average.HasValue ? $"Marked assessment average is {average:0.#}%." : "Confidence and course progress are used because marks are limited."),
            (factors.IncompleteTopics, "Incomplete topics increase the study need."),
            (factors.WorkloadRisk, "Workload risk affects available preparation capacity.")
        };
        return reasons.OrderByDescending(item => item.Value).Take(3).Select(item => item.Message).ToList();
    }

    private async Task EnsureDependenciesAsync(string userId)
    {
        await _priorityService.EnsureSettingsAsync(userId);
        await _gpaService.EnsureDefaultScaleAsync(userId);
    }

    private static CoursePriorityPreference DefaultPreference(string userId, Course course) => new()
    {
        ApplicationUserId = userId,
        CourseId = course.Id,
        ConfidenceRating = 3,
        TopicCompletionPercentage = course.ProgressPercentage,
        WorkloadRisk = 3,
        AvailableStudyHoursPerWeek = 5m
    };

    private static PriorityWeightSettingsViewModel ToSettingsViewModel(PriorityWeightSettings item) => new()
    {
        TargetGradeGapWeight = item.TargetGradeGapWeight,
        AssessmentUrgencyWeight = item.AssessmentUrgencyWeight,
        CourseCreditWeight = item.CourseCreditWeight,
        WeaknessWeight = item.WeaknessWeight,
        IncompleteTopicsWeight = item.IncompleteTopicsWeight,
        WorkloadRiskWeight = item.WorkloadRiskWeight
    };

    private static void PopulateConfigureEvidence(PriorityConfigureViewModel model, Course course)
    {
        var pending = PendingAssessments(course);
        model.CourseCode = course.CourseCode;
        model.CourseName = course.CourseName;
        model.CourseProgress = course.ProgressPercentage;
        model.PendingAssessments = pending.Count;
        model.NearestDeadline = pending.OrderBy(item => item.DueDate).FirstOrDefault()?.DueDate;
        model.AssessmentAverage = AssessmentAverage(course);
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id)
            .Select(item => new
            {
                item.StudentId, item.Department, item.Semester,
                HasProfileImage = item.ProfileImageData != null,
                item.CreatedAt, item.UpdatedAt
            }).SingleOrDefaultAsync();
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
