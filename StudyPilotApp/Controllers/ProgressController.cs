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
public sealed class ProgressController : Controller
{
    private static readonly HashSet<int> AllowedPeriods = [0, 30, 90, 180, 365];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IProgressService _progressService;
    private readonly IGpaService _gpaService;

    public ProgressController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IProgressService progressService,
        IGpaService gpaService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _progressService = progressService;
        _gpaService = gpaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int period = 90)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        period = AllowedPeriods.Contains(period) ? period : 90;
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        DateOnly? fromDate = period == 0
            ? null
            : DateOnly.FromDateTime(DateTime.Now.AddDays(-period));
        var data = await _progressService.GetAnalyticsAsync(user.Id, fromDate);
        var model = new ProgressDashboardViewModel
        {
            PeriodDays = period,
            Overview = data.Overview,
            Courses = data.Courses,
            GpaTrend = data.GpaTrend,
            AssessmentTypes = data.AssessmentTypes,
            Snapshots = data.Snapshots,
            Insights = data.Insights,
            SnapshotProgressChange = data.SnapshotProgressChange,
            SnapshotCompletionChange = data.SnapshotCompletionChange
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSnapshot(int period = 90)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        period = AllowedPeriods.Contains(period) ? period : 90;
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        await _progressService.CaptureTodayAsync(user.Id);
        TempData["ProgressSuccess"] = "Today's academic progress snapshot was saved. Saving again today will update it.";
        return RedirectToAction(nameof(Index), new { period });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSnapshot(int id, int period = 90)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        period = AllowedPeriods.Contains(period) ? period : 90;
        var snapshot = await _progressService.GetOwnedSnapshotAsync(user.Id, id, trackChanges: true);
        if (snapshot is null) return NotFound();
        _progressService.RemoveSnapshot(snapshot);
        await _progressService.SaveChangesAsync();
        TempData["ProgressSuccess"] = "Progress snapshot deleted.";
        return RedirectToAction(nameof(Index), new { period });
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
