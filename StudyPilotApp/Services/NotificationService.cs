using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationPage> GetNotificationsAsync(
        string userId,
        string? search,
        NotificationType? type,
        string status,
        int page,
        int pageSize)
    {
        var userNotifications = _dbContext.AppNotifications.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId);

        var unreadCount = await userNotifications.CountAsync(item => !item.IsRead);
        var query = userNotifications;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.Message, pattern));
        }

        if (type.HasValue)
            query = query.Where(item => item.Type == type.Value);

        query = status switch
        {
            "unread" => query.Where(item => !item.IsRead),
            "read" => query.Where(item => item.IsRead),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(item => item.IsRead)
            .ThenByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NotificationPage(items, totalCount, unreadCount);
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _dbContext.AppNotifications.AsNoTracking()
            .CountAsync(item => item.ApplicationUserId == userId && !item.IsRead);

    public async Task CreateAsync(
        string userId,
        string title,
        string message,
        NotificationType type = NotificationType.General,
        string? relatedUrl = null)
    {
        title = title.Trim();
        message = message.Trim();

        if (string.IsNullOrWhiteSpace(userId) ||
            title.Length is < 1 or > 160 ||
            message.Length is < 1 or > 1000)
        {
            throw new ArgumentException("Notification data is invalid.");
        }

        await _dbContext.AppNotifications.AddAsync(new AppNotification
        {
            ApplicationUserId = userId,
            Title = title,
            Message = message,
            Type = Enum.IsDefined(type) ? type : NotificationType.General,
            RelatedUrl = NormalizeLocalUrl(relatedUrl)
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> MarkReadAsync(long notificationId, string userId)
    {
        var notification = await OwnedQuery(notificationId, userId).SingleOrDefaultAsync();
        if (notification is null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> MarkUnreadAsync(long notificationId, string userId)
    {
        var notification = await OwnedQuery(notificationId, userId).SingleOrDefaultAsync();
        if (notification is null) return false;

        if (notification.IsRead)
        {
            notification.IsRead = false;
            notification.ReadAt = null;
            await _dbContext.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbContext.AppNotifications
            .Where(item => item.ApplicationUserId == userId && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAt, now));
    }

    public async Task<bool> DeleteAsync(long notificationId, string userId)
    {
        var notification = await OwnedQuery(notificationId, userId).SingleOrDefaultAsync();
        if (notification is null) return false;

        _dbContext.AppNotifications.Remove(notification);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public Task<int> ClearReadAsync(string userId) =>
        _dbContext.AppNotifications
            .Where(item => item.ApplicationUserId == userId && item.IsRead)
            .ExecuteDeleteAsync();

    private IQueryable<AppNotification> OwnedQuery(long notificationId, string userId) =>
        _dbContext.AppNotifications.Where(item =>
            item.Id == notificationId && item.ApplicationUserId == userId);

    private static string? NormalizeLocalUrl(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 500) return null;
        if (!value.StartsWith('/') || value.StartsWith("//", StringComparison.Ordinal)) return null;
        return value;
    }
}
