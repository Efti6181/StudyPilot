using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IEventService
{
    Task<EventPage> GetEventsAsync(
        string userId,
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string scope,
        string sort,
        int page,
        int pageSize);

    Task<EventSummary> GetSummaryAsync(string userId);
    Task<CampusEvent?> GetEventAsync(int eventId);
    Task<IReadOnlyList<CampusEvent>> GetUpcomingForDashboardAsync(string userId, int count);
    Task<EventRegistrationResult> RegisterAsync(int eventId, string userId);
    Task<bool> CancelRegistrationAsync(int eventId, string userId);
    Task<bool?> ToggleSavedAsync(int eventId, string userId);
}

public sealed record EventPage(IReadOnlyList<CampusEvent> Items, int TotalCount);
public sealed record EventSummary(int Registered, int Saved);

public enum EventRegistrationResult
{
    Registered,
    AlreadyRegistered,
    NotFound,
    RegistrationClosed,
    EventEnded,
    Full
}
