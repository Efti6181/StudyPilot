using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.ViewComponents;

public sealed class NotificationBellViewComponent : ViewComponent
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public NotificationBellViewComponent(
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = UserClaimsPrincipal;
        if (principal.Identity?.IsAuthenticated != true || !principal.IsInRole("Student"))
            return Content(string.Empty);

        var userId = _userManager.GetUserId(principal);
        var unreadCount = string.IsNullOrWhiteSpace(userId)
            ? 0
            : await _notificationService.GetUnreadCountAsync(userId);

        return View(new NotificationBellViewModel { UnreadCount = unreadCount });
    }
}
