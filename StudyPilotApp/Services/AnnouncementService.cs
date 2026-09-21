using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Linq.Expressions;

namespace StudyPilotApp.Services;

public sealed class AnnouncementService : IAnnouncementService
{
    private static readonly Expression<Func<Announcement, AnnouncementData>> Projection = item => new(
        item.Id, item.Title, item.Summary, item.Content, item.Audience, item.Priority,
        item.IsPublished, item.PublishedAt, item.ExpiresAt, item.DeliveredAt,
        item.RecipientCount, item.CreatedByUser.FullName, item.CreatedAt, item.UpdatedAt);

    private readonly ApplicationDbContext _dbContext;

    public AnnouncementService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AnnouncementListData> SearchAsync(
        string? search,
        AnnouncementAudience? audience,
        AnnouncementPriority? priority,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var now = DateTimeOffset.UtcNow;
        var all = _dbContext.Announcements.AsNoTracking();
        var totalCount = await all.CountAsync(cancellationToken);
        var publishedCount = await all.CountAsync(item => item.IsPublished, cancellationToken);
        var activeCount = await all.CountAsync(item => item.IsPublished &&
            (!item.ExpiresAt.HasValue || item.ExpiresAt >= now), cancellationToken);
        var urgentCount = await all.CountAsync(item => item.IsPublished &&
            item.Priority == AnnouncementPriority.Urgent &&
            (!item.ExpiresAt.HasValue || item.ExpiresAt >= now), cancellationToken);
        var deliveredRecipientCount = await all.SumAsync(item => item.RecipientCount, cancellationToken);

        IQueryable<Announcement> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.Summary, pattern) ||
                EF.Functions.ILike(item.Content, pattern));
        }
        if (audience.HasValue) query = query.Where(item => item.Audience == audience.Value);
        if (priority.HasValue) query = query.Where(item => item.Priority == priority.Value);
        query = status switch
        {
            "published" => query.Where(item => item.IsPublished),
            "draft" => query.Where(item => !item.IsPublished),
            "active" => query.Where(item => item.IsPublished && (!item.ExpiresAt.HasValue || item.ExpiresAt >= now)),
            "expired" => query.Where(item => item.ExpiresAt.HasValue && item.ExpiresAt < now),
            _ => query
        };

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.PublishedAt ?? item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new AnnouncementListData(
            items, totalCount, publishedCount, totalCount - publishedCount, activeCount,
            urgentCount, deliveredRecipientCount, filteredCount, page, pageSize);
    }

    public Task<AnnouncementData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Announcements.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AnnouncementSaveResult> CreateAsync(
        AnnouncementInput input,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminUserId))
            return AnnouncementSaveResult.Failure("The administrator account could not be verified.");
        var error = Validate(input);
        if (error is not null) return AnnouncementSaveResult.Failure(error);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var item = new Announcement { CreatedByUserId = adminUserId };
        Apply(item, input);
        _dbContext.Announcements.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (input.IsPublished) await DeliverAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AnnouncementSaveResult.Success(item.Id, item.RecipientCount);
    }

    public async Task<AnnouncementSaveResult> UpdateAsync(
        int id,
        AnnouncementInput input,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Announcements.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AnnouncementSaveResult.Failure("Announcement not found.");
        var error = Validate(input);
        if (error is not null) return AnnouncementSaveResult.Failure(error);
        if (item.DeliveredAt.HasValue && item.Audience != input.Audience)
            return AnnouncementSaveResult.Failure("The audience cannot change after notifications have been delivered.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var shouldDeliver = input.IsPublished && !item.DeliveredAt.HasValue;
        Apply(item, input);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        if (shouldDeliver) await DeliverAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AnnouncementSaveResult.Success(id, item.RecipientCount);
    }

    public async Task<AnnouncementSaveResult> SetPublishedAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Announcements.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AnnouncementSaveResult.Failure("Announcement not found.");
        if (item.IsPublished == isPublished) return AnnouncementSaveResult.Success(id, item.RecipientCount);
        if (isPublished)
        {
            var error = Validate(ToInput(item, true));
            if (error is not null) return AnnouncementSaveResult.Failure(error);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        item.IsPublished = isPublished;
        item.PublishedAt = isPublished ? item.PublishedAt ?? DateTimeOffset.UtcNow : item.PublishedAt;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        if (isPublished && !item.DeliveredAt.HasValue) await DeliverAsync(item, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AnnouncementSaveResult.Success(id, item.RecipientCount);
    }

    public async Task<AnnouncementSaveResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Announcements.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AnnouncementSaveResult.Failure("Announcement not found.");
        if (item.DeliveredAt.HasValue)
            return AnnouncementSaveResult.Failure("Delivered announcements are retained for audit history. Unpublish it instead.");
        _dbContext.Announcements.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AnnouncementSaveResult.Success(id);
    }

    private async Task DeliverAsync(Announcement item, CancellationToken cancellationToken)
    {
        var recipients =
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join user in _dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where user.IsActive &&
                  ((item.Audience == AnnouncementAudience.Everyone &&
                    (role.Name == "Student" || role.Name == "Faculty")) ||
                   (item.Audience == AnnouncementAudience.Students && role.Name == "Student") ||
                   (item.Audience == AnnouncementAudience.Faculty && role.Name == "Faculty"))
            select user.Id;

        var recipientIds = await recipients.Distinct().ToListAsync(cancellationToken);
        var title = item.Priority == AnnouncementPriority.Urgent
            ? $"Urgent: {item.Title}"
            : item.Title;
        title = title[..Math.Min(title.Length, 160)];
        var message = item.Priority == AnnouncementPriority.Important
            ? $"Important: {item.Summary}"
            : item.Summary;

        _dbContext.AppNotifications.AddRange(recipientIds.Select(userId => new AppNotification
        {
            ApplicationUserId = userId,
            Title = title,
            Message = message,
            Type = NotificationType.System,
            RelatedUrl = "/Notifications"
        }));
        item.IsPublished = true;
        item.PublishedAt ??= DateTimeOffset.UtcNow;
        item.DeliveredAt = DateTimeOffset.UtcNow;
        item.RecipientCount = recipientIds.Count;
    }

    private static string? Validate(AnnouncementInput input)
    {
        if (!Enum.IsDefined(typeof(AnnouncementAudience), input.Audience)) return "Select a valid audience.";
        if (!Enum.IsDefined(typeof(AnnouncementPriority), input.Priority)) return "Select a valid priority.";
        if (input.IsPublished && input.ExpiresAt.HasValue && input.ExpiresAt <= DateTimeOffset.UtcNow)
            return "A published announcement must expire in the future.";
        return null;
    }

    private static void Apply(Announcement item, AnnouncementInput input)
    {
        item.Title = input.Title.Trim();
        item.Summary = input.Summary.Trim();
        item.Content = input.Content.Trim();
        item.Audience = input.Audience;
        item.Priority = input.Priority;
        item.ExpiresAt = input.ExpiresAt;
        item.IsPublished = input.IsPublished;
        if (input.IsPublished) item.PublishedAt ??= DateTimeOffset.UtcNow;
    }

    private static AnnouncementInput ToInput(Announcement item, bool isPublished) => new(
        item.Title, item.Summary, item.Content, item.Audience, item.Priority, item.ExpiresAt, isPublished);
}
