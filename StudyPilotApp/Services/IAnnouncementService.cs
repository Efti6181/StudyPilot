using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAnnouncementService
{
    Task<AnnouncementListData> SearchAsync(
        string? search,
        AnnouncementAudience? audience,
        AnnouncementPriority? priority,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AnnouncementData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<AnnouncementSaveResult> CreateAsync(
        AnnouncementInput input,
        string adminUserId,
        CancellationToken cancellationToken = default);
    Task<AnnouncementSaveResult> UpdateAsync(
        int id,
        AnnouncementInput input,
        CancellationToken cancellationToken = default);
    Task<AnnouncementSaveResult> SetPublishedAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken = default);
    Task<AnnouncementSaveResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AnnouncementInput(
    string Title,
    string Summary,
    string Content,
    AnnouncementAudience Audience,
    AnnouncementPriority Priority,
    DateTimeOffset? ExpiresAt,
    bool IsPublished);

public sealed record AnnouncementData(
    int Id,
    string Title,
    string Summary,
    string Content,
    AnnouncementAudience Audience,
    AnnouncementPriority Priority,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? DeliveredAt,
    int RecipientCount,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AnnouncementListData(
    IReadOnlyList<AnnouncementData> Items,
    int TotalCount,
    int PublishedCount,
    int DraftCount,
    int ActiveCount,
    int UrgentCount,
    int DeliveredRecipientCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AnnouncementSaveResult(bool Succeeded, int? Id, int RecipientCount, string? Error)
{
    public static AnnouncementSaveResult Success(int id, int recipientCount = 0) => new(true, id, recipientCount, null);
    public static AnnouncementSaveResult Failure(string error) => new(false, null, 0, error);
}
