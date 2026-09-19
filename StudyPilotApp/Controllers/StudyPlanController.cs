using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class StudyPlanController : Controller
{
    private static readonly int[] AllowedSessionMinutes = [30, 45, 60, 90];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ISmartStudyPlanService _studyPlanService;
    private readonly IAITextProvider _aiProvider;

    public StudyPlanController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ISmartStudyPlanService studyPlanService,
        IAITextProvider aiProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _studyPlanService = studyPlanService;
        _aiProvider = aiProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var plans = await _studyPlanService.GetPlansAsync(user.Id);
        var periods = await _studyPlanService.GetAvailablePeriodsAsync(user.Id);
        var model = new SmartStudyPlanIndexViewModel
        {
            AvailablePeriodCount = periods.Count,
            Plans = plans.Select(plan => new SmartStudyPlanSummaryViewModel
            {
                Id = plan.Id,
                Title = plan.Title,
                PeriodLabel = $"Semester {plan.Semester} · {plan.AcademicTerm} {plan.AcademicYear}",
                CareerGoal = plan.CareerGoal,
                WeeklyStudyHours = plan.WeeklyStudyHours,
                CourseCount = plan.Courses.Count,
                SessionCount = plan.Courses.Sum(course => course.Sessions.Count),
                IsActive = plan.IsActive,
                UsedAiAnalysis = plan.UsedAiAnalysis,
                AnalysisProvider = plan.AnalysisProvider,
                CreatedAt = plan.CreatedAt
            }).ToList()
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? periodKey = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var model = new SmartStudyPlanCreateViewModel();
        await PopulateCreateOptionsAsync(model, user.Id);
        model.PeriodKey = model.Periods.Any(item => item.Key == periodKey)
            ? periodKey!
            : model.Periods.FirstOrDefault()?.Key ?? string.Empty;

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("academic-ai")]
    public async Task<IActionResult> Create(
        SmartStudyPlanCreateViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        model.StudyDays = model.StudyDays
            .Where(day => Enum.IsDefined(typeof(DayOfWeek), day))
            .Distinct()
            .ToList();
        if (model.StudyDays.Count == 0)
            ModelState.AddModelError(nameof(model.StudyDays), "Select at least one study day.");
        if (!AllowedSessionMinutes.Contains(model.SessionMinutes))
            ModelState.AddModelError(nameof(model.SessionMinutes), "Select a valid session length.");
        if (model.PreferredEndTime <= model.PreferredStartTime)
            ModelState.AddModelError(nameof(model.PreferredEndTime), "End time must be later than start time.");

        if (ModelState.IsValid)
        {
            try
            {
                var plan = await _studyPlanService.GenerateAsync(
                    user.Id,
                    new StudyPlanGenerationRequest(
                        model.PeriodKey,
                        model.CareerGoal,
                        model.StudyDays,
                        model.PreferredStartTime,
                        model.PreferredEndTime,
                        model.WeeklyStudyHours,
                        model.SessionMinutes,
                        model.BreakMinutes),
                    cancellationToken);
                TempData["StudyPlanSuccess"] = plan.UsedAiAnalysis
                    ? "Your AI-assisted weekly study plan is ready."
                    : "Your weekly plan is ready. AI analysis was unavailable, so safe course estimates were used.";
                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }
            catch (StudyPlanValidationException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        await PopulateCreateOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var plan = await _studyPlanService.GetOwnedPlanAsync(user.Id, id);
        if (plan is null) return NotFound();

        var model = ToDetailsViewModel(plan);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await _studyPlanService.DeleteAsync(user.Id, id)) return NotFound();
        TempData["StudyPlanSuccess"] = "The study plan was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateOptionsAsync(SmartStudyPlanCreateViewModel model, string userId)
    {
        model.Periods = await _studyPlanService.GetAvailablePeriodsAsync(userId);
        model.IsAiConfigured = _aiProvider.IsConfigured;
    }

    private static SmartStudyPlanDetailsViewModel ToDetailsViewModel(SmartStudyPlan plan)
    {
        var sessions = plan.Courses
            .SelectMany(course => course.Sessions.Select(session => new
            {
                Course = course,
                Session = session
            }))
            .ToList();

        return new SmartStudyPlanDetailsViewModel
        {
            Id = plan.Id,
            Title = plan.Title,
            PeriodLabel = $"Semester {plan.Semester} · {plan.AcademicTerm} {plan.AcademicYear}",
            CareerGoal = plan.CareerGoal,
            StudyDays = string.Join(", ", plan.StudyDays.Split(',', StringSplitOptions.RemoveEmptyEntries)),
            PreferredWindow = $"{plan.PreferredStartTime:h:mm tt} – {plan.PreferredEndTime:h:mm tt}",
            WeeklyStudyHours = plan.WeeklyStudyHours,
            SessionMinutes = plan.SessionMinutes,
            BreakMinutes = plan.BreakMinutes,
            UsedAiAnalysis = plan.UsedAiAnalysis,
            AnalysisProvider = plan.AnalysisProvider,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt,
            Schedule = sessions
                .GroupBy(item => item.Session.Day)
                .OrderBy(group => group.Key == DayOfWeek.Sunday ? 0 : (int)group.Key + 1)
                .Select(group => new SmartStudyDayViewModel
                {
                    Day = group.Key,
                    Sessions = group.OrderBy(item => item.Session.StartTime)
                        .Select(item => new SmartStudySessionViewModel
                        {
                            CourseCode = item.Course.CourseCode,
                            CourseName = item.Course.CourseName,
                            StartTime = item.Session.StartTime,
                            EndTime = item.Session.EndTime
                        }).ToList()
                }).ToList(),
            Courses = plan.Courses.OrderByDescending(course => course.CareerRelevance)
                .ThenByDescending(course => course.Difficulty)
                .ThenBy(course => course.CourseCode)
                .Select(course => new SmartStudyCourseViewModel
                {
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Difficulty = course.Difficulty,
                    CareerRelevance = course.CareerRelevance,
                    AnalysisReason = course.AnalysisReason,
                    WeeklyMinutes = course.WeeklyMinutes,
                    SessionCount = course.Sessions.Count
                }).ToList()
        };
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id)
            .Select(item => new
            {
                item.StudentId,
                item.Department,
                item.Semester,
                HasProfileImage = item.ProfileImageData != null,
                item.CreatedAt,
                item.UpdatedAt
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
