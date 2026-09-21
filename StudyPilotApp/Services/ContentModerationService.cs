using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class ContentModerationService : IContentModerationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IResourceService _resourceService;

    public ContentModerationService(ApplicationDbContext dbContext, IResourceService resourceService)
    {
        _dbContext = dbContext;
        _resourceService = resourceService;
    }

    public async Task<ModerationQueueData> SearchAsync(
        string? search,
        string scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var postCount = await _dbContext.CommunityPosts.AsNoTracking().CountAsync(cancellationToken);
        var commentCount = await _dbContext.CommunityComments.AsNoTracking().CountAsync(cancellationToken);
        var resourceCount = await _dbContext.StudyResources.AsNoTracking().CountAsync(cancellationToken);
        var removedCount = await _dbContext.ContentModerationRecords.AsNoTracking().CountAsync(cancellationToken);

        if (scope == "history")
        {
            var historyQuery = _dbContext.ContentModerationRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                historyQuery = historyQuery.Where(item =>
                    EF.Functions.ILike(item.ContentTitle, pattern) ||
                    EF.Functions.ILike(item.ContentExcerpt, pattern) ||
                    EF.Functions.ILike(item.OwnerName, pattern) ||
                    EF.Functions.ILike(item.OwnerEmail, pattern) ||
                    EF.Functions.ILike(item.Reason, pattern));
            }
            var filteredCount = await historyQuery.CountAsync(cancellationToken);
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
            page = Math.Min(page, totalPages);
            var history = await historyQuery
                .OrderByDescending(item => item.ModeratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new ModerationHistoryData(
                    item.Id, item.ContentType, item.SourceId, item.ContentTitle,
                    item.ContentExcerpt, item.OwnerName, item.OwnerEmail, item.Reason,
                    item.ModeratedByUser.FullName, item.ModeratedAt))
                .ToListAsync(cancellationToken);
            return new ModerationQueueData(
                [], history, postCount, commentCount, resourceCount, removedCount,
                filteredCount, page, pageSize);
        }

        var items = new List<ModerationContentData>();
        if (scope is "all" or "posts")
            items.AddRange(await GetPostsAsync(search, cancellationToken));
        if (scope is "all" or "comments")
            items.AddRange(await GetCommentsAsync(search, cancellationToken));
        if (scope is "all" or "resources")
            items.AddRange(await GetResourcesAsync(search, cancellationToken));

        items = items.OrderByDescending(item => item.CreatedAt).ToList();
        var totalItems = items.Count;
        var pages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Min(page, pages);
        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new ModerationQueueData(
            pagedItems, [], postCount, commentCount, resourceCount, removedCount,
            totalItems, page, pageSize);
    }

    public Task<ModerationContentData?> GetContentAsync(
        ModeratedContentType contentType,
        int sourceId,
        CancellationToken cancellationToken = default) => contentType switch
        {
            ModeratedContentType.CommunityPost => GetPostAsync(sourceId, cancellationToken),
            ModeratedContentType.CommunityComment => GetCommentAsync(sourceId, cancellationToken),
            ModeratedContentType.StudyResource => GetResourceAsync(sourceId, cancellationToken),
            _ => Task.FromResult<ModerationContentData?>(null)
        };

    public async Task<ModerationResult> RemoveAsync(
        ModeratedContentType contentType,
        int sourceId,
        string reason,
        string moderatorUserId,
        CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 1000)
            return ModerationResult.Failure("Provide a moderation reason between 10 and 1,000 characters.");
        if (string.IsNullOrWhiteSpace(moderatorUserId))
            return ModerationResult.Failure("The moderator account could not be verified.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        ModerationContentData? content;
        string? storedFileName = null;
        switch (contentType)
        {
            case ModeratedContentType.CommunityPost:
                var post = await _dbContext.CommunityPosts
                    .Include(item => item.ApplicationUser)
                    .SingleOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                if (post is null) return ModerationResult.Failure("Community post not found.");
                content = ToContent(post);
                _dbContext.CommunityPosts.Remove(post);
                break;
            case ModeratedContentType.CommunityComment:
                var comment = await _dbContext.CommunityComments
                    .Include(item => item.ApplicationUser)
                    .Include(item => item.CommunityPost)
                    .SingleOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                if (comment is null) return ModerationResult.Failure("Community comment not found.");
                content = ToContent(comment);
                _dbContext.CommunityComments.Remove(comment);
                break;
            case ModeratedContentType.StudyResource:
                var resource = await _dbContext.StudyResources
                    .Include(item => item.ApplicationUser)
                    .Include(item => item.Course)
                    .SingleOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                if (resource is null) return ModerationResult.Failure("Study resource not found.");
                content = ToContent(resource);
                storedFileName = resource.StoredFileName;
                _dbContext.StudyResources.Remove(resource);
                break;
            default:
                return ModerationResult.Failure("Select a valid content type.");
        }

        _dbContext.ContentModerationRecords.Add(new ContentModerationRecord
        {
            ContentType = content.ContentType,
            SourceId = content.SourceId,
            ContentTitle = Truncate(content.Title, 180),
            ContentExcerpt = Truncate(content.Content, 1000),
            OwnerUserId = content.OwnerUserId,
            OwnerName = Truncate(content.OwnerName, 150),
            OwnerEmail = Truncate(content.OwnerEmail, 256),
            Reason = reason,
            ModeratedByUserId = moderatorUserId
        });
        _dbContext.AppNotifications.Add(new AppNotification
        {
            ApplicationUserId = content.OwnerUserId,
            Title = "Content removed by StudyPilot moderation",
            Message = Truncate($"{content.Title} was removed. Reason: {reason}", 1000),
            Type = contentType == ModeratedContentType.StudyResource
                ? NotificationType.System
                : NotificationType.Community,
            RelatedUrl = contentType == ModeratedContentType.StudyResource ? "/Resources" : "/Community"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (storedFileName is not null)
        {
            try
            {
                await _resourceService.DeleteStoredFileAsync(storedFileName);
            }
            catch (IOException)
            {
                // The database moderation action is authoritative. A locked orphan file can be cleaned later.
            }
            catch (UnauthorizedAccessException)
            {
                // Avoid rolling a completed moderation request back after the database transaction committed.
            }
        }
        return ModerationResult.Success();
    }

    private async Task<IReadOnlyList<ModerationContentData>> GetPostsAsync(string? search, CancellationToken token)
    {
        var query = _dbContext.CommunityPosts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.Content, pattern) || EF.Functions.ILike(item.ApplicationUser.FullName, pattern));
        }
        var rows = await query.Select(item => new
        {
            item.Id, item.Title, item.Content, item.ApplicationUserId,
            OwnerName = item.ApplicationUser.FullName,
            OwnerEmail = item.ApplicationUser.Email ?? string.Empty,
            item.CreatedAt, item.Category,
            InteractionCount = item.Comments.Count + item.Likes.Count
        }).ToListAsync(token);
        return rows.Select(item => new ModerationContentData(
            ModeratedContentType.CommunityPost, item.Id, item.Title, item.Content,
            item.ApplicationUserId, item.OwnerName, item.OwnerEmail, item.CreatedAt,
            item.Category.ToString(), item.InteractionCount)).ToList();
    }

    private async Task<IReadOnlyList<ModerationContentData>> GetCommentsAsync(string? search, CancellationToken token)
    {
        var query = _dbContext.CommunityComments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Content, pattern) ||
                EF.Functions.ILike(item.CommunityPost.Title, pattern) ||
                EF.Functions.ILike(item.ApplicationUser.FullName, pattern));
        }
        return await query.Select(item => new ModerationContentData(
            ModeratedContentType.CommunityComment, item.Id, "Comment on: " + item.CommunityPost.Title,
            item.Content, item.ApplicationUserId, item.ApplicationUser.FullName,
            item.ApplicationUser.Email ?? string.Empty, item.CreatedAt,
            "Community comment", item.Likes.Count)).ToListAsync(token);
    }

    private async Task<IReadOnlyList<ModerationContentData>> GetResourcesAsync(string? search, CancellationToken token)
    {
        var query = _dbContext.StudyResources.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Title, pattern) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)) ||
                EF.Functions.ILike(item.ApplicationUser.FullName, pattern));
        }
        var rows = await query.Select(item => new
        {
            item.Id, item.Title, item.Description, item.ApplicationUserId,
            OwnerName = item.ApplicationUser.FullName,
            OwnerEmail = item.ApplicationUser.Email ?? string.Empty,
            item.CreatedAt, item.Kind, item.Category
        }).ToListAsync(token);
        return rows.Select(item => new ModerationContentData(
            ModeratedContentType.StudyResource, item.Id, item.Title,
            item.Description ?? "No description provided.", item.ApplicationUserId,
            item.OwnerName, item.OwnerEmail, item.CreatedAt,
            $"{item.Kind} · {item.Category}", 0)).ToList();
    }

    private async Task<ModerationContentData?> GetPostAsync(int id, CancellationToken token)
    {
        var item = await _dbContext.CommunityPosts.AsNoTracking().Where(row => row.Id == id)
            .Select(row => new
            {
                row.Id, row.Title, row.Content, row.ApplicationUserId,
                OwnerName = row.ApplicationUser.FullName,
                OwnerEmail = row.ApplicationUser.Email ?? string.Empty,
                row.CreatedAt, row.Category,
                InteractionCount = row.Comments.Count + row.Likes.Count
            }).SingleOrDefaultAsync(token);
        return item is null ? null : new ModerationContentData(
            ModeratedContentType.CommunityPost, item.Id, item.Title, item.Content,
            item.ApplicationUserId, item.OwnerName, item.OwnerEmail, item.CreatedAt,
            item.Category.ToString(), item.InteractionCount);
    }

    private Task<ModerationContentData?> GetCommentAsync(int id, CancellationToken token) =>
        _dbContext.CommunityComments.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new ModerationContentData(
                ModeratedContentType.CommunityComment, item.Id, "Comment on: " + item.CommunityPost.Title,
                item.Content, item.ApplicationUserId, item.ApplicationUser.FullName,
                item.ApplicationUser.Email ?? string.Empty, item.CreatedAt, "Community comment", item.Likes.Count))
            .SingleOrDefaultAsync(token);

    private async Task<ModerationContentData?> GetResourceAsync(int id, CancellationToken token)
    {
        var item = await _dbContext.StudyResources.AsNoTracking().Where(row => row.Id == id)
            .Select(row => new
            {
                row.Id, row.Title, row.Description, row.ApplicationUserId,
                OwnerName = row.ApplicationUser.FullName,
                OwnerEmail = row.ApplicationUser.Email ?? string.Empty,
                row.CreatedAt, row.Kind, row.Category
            }).SingleOrDefaultAsync(token);
        return item is null ? null : new ModerationContentData(
            ModeratedContentType.StudyResource, item.Id, item.Title,
            item.Description ?? "No description provided.", item.ApplicationUserId,
            item.OwnerName, item.OwnerEmail, item.CreatedAt,
            $"{item.Kind} · {item.Category}", 0);
    }

    private static ModerationContentData ToContent(CommunityPost item) => new(
        ModeratedContentType.CommunityPost, item.Id, item.Title, item.Content,
        item.ApplicationUserId, item.ApplicationUser.FullName, item.ApplicationUser.Email ?? string.Empty,
        item.CreatedAt, item.Category.ToString(), 0);

    private static ModerationContentData ToContent(CommunityComment item) => new(
        ModeratedContentType.CommunityComment, item.Id, $"Comment on: {item.CommunityPost.Title}", item.Content,
        item.ApplicationUserId, item.ApplicationUser.FullName, item.ApplicationUser.Email ?? string.Empty,
        item.CreatedAt, "Community comment", 0);

    private static ModerationContentData ToContent(StudyResource item) => new(
        ModeratedContentType.StudyResource, item.Id, item.Title, item.Description ?? "No description provided.",
        item.ApplicationUserId, item.ApplicationUser.FullName, item.ApplicationUser.Email ?? string.Empty,
        item.CreatedAt, $"{item.Kind} · {item.Category}", 0);

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum];
}
