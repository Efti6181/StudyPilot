using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class NotificationIndexViewModel : StudentShellViewModel
{
    public IReadOnlyList<NotificationItemViewModel> Notifications { get; set; } = [];
    public string? Search { get; set; }
    public NotificationType? Type { get; set; }
    public string Status { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed class NotificationItemViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string Icon => Type switch
    {
        NotificationType.Academic => "bi-mortarboard",
        NotificationType.Assessment => "bi-clipboard2-check",
        NotificationType.Event => "bi-calendar-event",
        NotificationType.Community => "bi-people",
        NotificationType.System => "bi-gear",
        _ => "bi-bell"
    };
}

public sealed class NotificationBellViewModel
{
    public int UnreadCount { get; set; }
    public string BadgeLabel => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
}
