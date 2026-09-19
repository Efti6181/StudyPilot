using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface INotificationService
{
    Task<NotificationPage> GetNotificationsAsync(
        string userId,
        string? search,
        NotificationType? type,
        string status,
        int page,
        int pageSize);

    Task<int> GetUnreadCountAsync(string userId);

    Task CreateAsync(
        string userId,
        string title,
        string message,
        NotificationType type = NotificationType.General,
        string? relatedUrl = null);

    Task<bool> MarkReadAsync(long notificationId, string userId);
    Task<bool> MarkUnreadAsync(long notificationId, string userId);
    Task<int> MarkAllReadAsync(string userId);
    Task<bool> DeleteAsync(long notificationId, string userId);
    Task<int> ClearReadAsync(string userId);
}

public sealed record NotificationPage(
    IReadOnlyList<AppNotification> Items,
    int TotalCount,
    int UnreadCount);
