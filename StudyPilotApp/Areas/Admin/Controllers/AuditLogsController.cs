using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AuditLogsController : Controller
{
    private const int PageSize = 15;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminAuditService _auditService;

    public AuditLogsController(UserManager<ApplicationUser> userManager, IAdminAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? controllerFilter,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = Clean(search);
        controllerFilter = Clean(controllerFilter);
        status = NormalizeStatus(status);
        var data = await _auditService.SearchAsync(search, controllerFilter, status, page, PageSize, cancellationToken);
        var model = new AdminAuditIndexViewModel
        {
            Search = search,
            ControllerFilter = controllerFilter,
            Status = status,
            Page = data.Page,
            TotalPages = data.TotalPages,
            FilteredCount = data.FilteredCount,
            TotalCount = data.TotalCount,
            TodayCount = data.TodayCount,
            FailedCount = data.FailedCount,
            UniqueAdminCount = data.UniqueAdminCount,
            Controllers = data.Controllers,
            Items = data.Items.Select(item => new AdminAuditRowViewModel
            {
                Id = item.Id, AdminName = item.AdminName, Action = item.Action,
                Controller = item.Controller, HttpMethod = item.HttpMethod,
                RequestPath = item.RequestPath, EntityId = item.EntityId,
                IpAddress = item.IpAddress, Succeeded = item.Succeeded,
                StatusCode = item.StatusCode, Details = item.Details, CreatedAt = item.CreatedAt
            }).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? search,
        string? controllerFilter,
        string status = "all",
        CancellationToken cancellationToken = default)
    {
        var export = await _auditService.ExportAsync(Clean(search), Clean(controllerFilter), NormalizeStatus(status), cancellationToken);
        return File(export.Content, "text/csv; charset=utf-8", export.FileName);
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "success" => "success", "failed" => "failed", _ => "all"
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
