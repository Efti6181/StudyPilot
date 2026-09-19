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
public sealed class NotificationsController : Controller
{
    private const int PageSize = 12;
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "all", "unread", "read" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public NotificationsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        NotificationType? type,
        string status = "all",
        int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = NormalizeSearch(search);
        type = IsDefined(type) ? type : null;
        status = AllowedStatuses.Contains(status) ? status.ToLowerInvariant() : "all";
        page = Math.Max(1, page);

        var result = await _notificationService.GetNotificationsAsync(
            user.Id, search, type, status, page, PageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
            result = await _notificationService.GetNotificationsAsync(
                user.Id, search, type, status, page, PageSize);
        }

        var model = new NotificationIndexViewModel
        {
            Notifications = result.Items.Select(item => new NotificationItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Message = item.Message,
                Type = item.Type,
                RelatedUrl = item.RelatedUrl,
                IsRead = item.IsRead,
                CreatedAt = item.CreatedAt
            }).ToList(),
            Search = search,
            Type = type,
            Status = status,
            Page = page,
            PageSize = PageSize,
            TotalCount = result.TotalCount,
            UnreadCount = result.UnreadCount
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(long id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var notification = await _dbContext.AppNotifications.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.ApplicationUserId == userId);
        if (notification is null) return NotFound();

        await _notificationService.MarkReadAsync(id, userId);
        if (!string.IsNullOrWhiteSpace(notification.RelatedUrl) && Url.IsLocalUrl(notification.RelatedUrl))
            return LocalRedirect(notification.RelatedUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!await _notificationService.MarkReadAsync(id, userId)) return NotFound();
        return SafeReturn(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUnread(long id, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!await _notificationService.MarkUnreadAsync(id, userId)) return NotFound();
        return SafeReturn(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var changed = await _notificationService.MarkAllReadAsync(userId);
        TempData["NotificationSuccess"] = changed == 0
            ? "You have no unread notifications."
            : $"Marked {changed} notification{(changed == 1 ? "" : "s")} as read.";
        return SafeReturn(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!await _notificationService.DeleteAsync(id, userId)) return NotFound();
        TempData["NotificationSuccess"] = "Notification deleted.";
        return SafeReturn(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearRead()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var deleted = await _notificationService.ClearReadAsync(userId);
        TempData["NotificationSuccess"] = deleted == 0
            ? "There are no read notifications to clear."
            : $"Cleared {deleted} read notification{(deleted == 1 ? "" : "s")}.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult SafeReturn(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));

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

    private static string? NormalizeSearch(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 100)];
    }

    private static bool IsDefined<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value);

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
