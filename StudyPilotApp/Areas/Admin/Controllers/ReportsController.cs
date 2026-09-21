using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminReportsService _reportsService;

    public ReportsController(UserManager<ApplicationUser> userManager, IAdminReportsService reportsService)
    {
        _userManager = userManager;
        _reportsService = reportsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var data = await _reportsService.GetAsync(cancellationToken);
        var model = new AdminReportsViewModel
        {
            TotalUsers = data.TotalUsers,
            ActiveUsers = data.ActiveUsers,
            TotalCourses = data.TotalCourses,
            CompletedAssessments = data.CompletedAssessments,
            TotalAssessments = data.TotalAssessments,
            UpcomingEvents = data.UpcomingEvents,
            PublishedAnnouncements = data.PublishedAnnouncements,
            CommunityInteractions = data.CommunityInteractions,
            AverageCourseProgress = data.AverageCourseProgress,
            GrowthLabels = data.GrowthLabels,
            StudentGrowth = data.StudentGrowth,
            FacultyGrowth = data.FacultyGrowth,
            RoleLabels = data.RoleLabels,
            RoleCounts = data.RoleCounts,
            ContentLabels = data.ContentLabels,
            ContentCounts = data.ContentCounts,
            SummaryRows = data.SummaryRows.Select(item => new ReportSummaryRowViewModel
            {
                Area = item.Area, Metric = item.Metric, Value = item.Value, Context = item.Context
            }).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string report, CancellationToken cancellationToken)
    {
        var export = await _reportsService.ExportAsync(report ?? string.Empty, cancellationToken);
        return export is null
            ? BadRequest("Select a valid report export.")
            : File(export.Content, "text/csv; charset=utf-8", export.FileName);
    }

    private static void PopulateShell(AdminShellViewModel model, ApplicationUser user)
    {
        model.FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Administrator" : user.FullName;
        model.Email = user.Email ?? string.Empty;
        model.Initials = Initials(user.FullName);
    }

    private static string Initials(string? value)
    {
        var parts = (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "AD";
        return parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant() : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
