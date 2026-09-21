using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminDashboardService _dashboardService;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        IAdminDashboardService dashboardService)
    {
        _userManager = userManager;
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var data = await _dashboardService.GetDashboardAsync(cancellationToken);
        var model = new AdminDashboardViewModel
        {
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Administrator" : user.FullName,
            Email = user.Email ?? string.Empty,
            Initials = CreateInitials(user.FullName),
            TotalStudents = data.TotalStudents,
            TotalFaculty = data.TotalFaculty,
            ActiveStudents = data.ActiveStudents,
            ActiveFaculty = data.ActiveFaculty,
            PendingRegistrations = data.PendingRegistrations,
            StudentCourseRecords = data.StudentCourseRecords,
            UpcomingEvents = data.UpcomingEvents,
            CommunityPosts = data.CommunityPosts,
            Resources = data.Resources,
            UnpublishedEvents = data.UnpublishedEvents,
            RecentRegistrations = data.RecentRegistrations.Select(item => new AdminRegistrationViewModel
            {
                FullName = item.FullName,
                Email = item.Email,
                Role = item.Role,
                IsActive = item.IsActive,
                RegisteredAt = item.RegisteredAt,
                Initials = CreateInitials(item.FullName)
            }).ToList(),
            UpcomingEventItems = data.UpcomingEventItems.Select(item => new AdminEventViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type,
                StartAt = item.StartAt,
                Location = item.Location,
                RegistrationCount = item.RegistrationCount,
                Capacity = item.Capacity
            }).ToList(),
            RecentActivity = data.RecentActivity.Select(item => new AdminActivityViewModel
            {
                Kind = item.Kind,
                Title = item.Title,
                Description = item.Description,
                OccurredAt = item.OccurredAt,
                Icon = item.Icon
            }).ToList(),
            UserGrowth = data.UserGrowth.Select(item => new AdminGrowthViewModel
            {
                Label = item.Label,
                Students = item.Students,
                Faculty = item.Faculty
            }).ToList()
        };

        return View(model);
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
