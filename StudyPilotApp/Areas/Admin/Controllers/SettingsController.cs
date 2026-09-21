using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPlatformSettingsService _settingsService;

    public SettingsController(UserManager<ApplicationUser> userManager, IPlatformSettingsService settingsService)
    {
        _userManager = userManager;
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var data = await _settingsService.GetAsync(cancellationToken);
        var model = ToViewModel(data);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PlatformSettingsViewModel model, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        if (model.MaintenanceNoticeEnabled && string.IsNullOrWhiteSpace(model.MaintenanceNotice))
            ModelState.AddModelError(nameof(model.MaintenanceNotice), "Enter a notice before enabling it.");

        if (!ModelState.IsValid)
        {
            PopulateShell(model, admin);
            return View(model);
        }

        await _settingsService.UpdateAsync(new PlatformSettingsInput(
            model.InstitutionName, model.SupportEmail, model.TimeZoneId,
            model.DateFormat, model.DefaultPageSize, model.AuditRetentionDays,
            model.StudentRegistrationEnabled, model.FacultyRegistrationEnabled,
            model.AcademicAiEnabled, model.CommunityEnabled, model.EventsEnabled,
            model.MaintenanceNoticeEnabled, model.MaintenanceNotice), admin.Id, cancellationToken);
        TempData["AdminSuccess"] = "System settings saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static PlatformSettingsViewModel ToViewModel(PlatformSettingsData item) => new()
    {
        InstitutionName = item.InstitutionName,
        SupportEmail = item.SupportEmail,
        TimeZoneId = item.TimeZoneId,
        DateFormat = item.DateFormat,
        DefaultPageSize = item.DefaultPageSize,
        AuditRetentionDays = item.AuditRetentionDays,
        StudentRegistrationEnabled = item.StudentRegistrationEnabled,
        FacultyRegistrationEnabled = item.FacultyRegistrationEnabled,
        AcademicAiEnabled = item.AcademicAiEnabled,
        CommunityEnabled = item.CommunityEnabled,
        EventsEnabled = item.EventsEnabled,
        MaintenanceNoticeEnabled = item.MaintenanceNoticeEnabled,
        MaintenanceNotice = item.MaintenanceNotice,
        UpdatedAt = item.UpdatedAt,
        UpdatedByName = item.UpdatedByName
    };

    private static void PopulateShell(AdminShellViewModel model, ApplicationUser user)
    {
        model.FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Administrator" : user.FullName;
        model.Email = user.Email ?? string.Empty;
        var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        model.Initials = parts.Length switch
        {
            0 => "AD",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
    }
}
