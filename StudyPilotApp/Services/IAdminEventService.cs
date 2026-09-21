using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAdminEventService
{
    Task<AdminEventListData> SearchAsync(
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AdminManagedEventData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminEventRegistrantData>> GetRegistrantsAsync(
        int eventId,
        CancellationToken cancellationToken = default);
    Task<AdminEventSaveResult> CreateAsync(
        AdminEventInput input,
        string adminUserId,
        CancellationToken cancellationToken = default);
    Task<AdminEventSaveResult> UpdateAsync(
        int id,
        AdminEventInput input,
        CancellationToken cancellationToken = default);
    Task<AdminEventSaveResult> SetPublishedAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken = default);
    Task<AdminEventSaveResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AdminEventInput(
    string Title,
    string ShortDescription,
    string Description,
    CampusEventType Type,
    EventLocationType LocationType,
    string? Venue,
    string? OnlineUrl,
    string OrganizerName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateTimeOffset RegistrationDeadline,
    int? Capacity,
    bool IsPublished);

public sealed record AdminManagedEventData(
    int Id,
    string Title,
    string ShortDescription,
    string Description,
    CampusEventType Type,
    EventLocationType LocationType,
    string? Venue,
    string? OnlineUrl,
    string OrganizerName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateTimeOffset RegistrationDeadline,
    int? Capacity,
    bool IsPublished,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int RegistrationCount,
    int CancelledCount,
    int SavedCount);

public sealed record AdminEventRegistrantData(
    string UserId,
    string FullName,
    string Email,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? CancelledAt);

public sealed record AdminEventListData(
    IReadOnlyList<AdminManagedEventData> Items,
    int TotalCount,
    int PublishedCount,
    int DraftCount,
    int UpcomingCount,
    int ActiveRegistrationCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AdminEventSaveResult(bool Succeeded, int? Id, string? Error)
{
    public static AdminEventSaveResult Success(int id) => new(true, id, null);
    public static AdminEventSaveResult Failure(string error) => new(false, null, error);
}
