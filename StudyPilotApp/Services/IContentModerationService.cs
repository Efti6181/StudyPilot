using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IContentModerationService
{
    Task<ModerationQueueData> SearchAsync(
        string? search,
        string scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ModerationContentData?> GetContentAsync(
        ModeratedContentType contentType,
        int sourceId,
        CancellationToken cancellationToken = default);
    Task<ModerationResult> RemoveAsync(
        ModeratedContentType contentType,
        int sourceId,
        string reason,
        string moderatorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ModerationContentData(
    ModeratedContentType ContentType,
    int SourceId,
    string Title,
    string Content,
    string OwnerUserId,
    string OwnerName,
    string OwnerEmail,
    DateTimeOffset CreatedAt,
    string Detail,
    int InteractionCount);

public sealed record ModerationHistoryData(
    long Id,
    ModeratedContentType ContentType,
    int SourceId,
    string ContentTitle,
    string ContentExcerpt,
    string OwnerName,
    string OwnerEmail,
    string Reason,
    string ModeratorName,
    DateTimeOffset ModeratedAt);

public sealed record ModerationQueueData(
    IReadOnlyList<ModerationContentData> Items,
    IReadOnlyList<ModerationHistoryData> History,
    int PostCount,
    int CommentCount,
    int ResourceCount,
    int RemovedCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record ModerationResult(bool Succeeded, string? Error)
{
    public static ModerationResult Success() => new(true, null);
    public static ModerationResult Failure(string error) => new(false, error);
}
