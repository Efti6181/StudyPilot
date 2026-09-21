namespace StudyPilotApp.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardData(
    int TotalStudents,
    int TotalFaculty,
    int ActiveStudents,
    int ActiveFaculty,
    int PendingRegistrations,
    int StudentCourseRecords,
    int UpcomingEvents,
    int CommunityPosts,
    int Resources,
    int UnpublishedEvents,
    IReadOnlyList<AdminRegistrationData> RecentRegistrations,
    IReadOnlyList<AdminEventData> UpcomingEventItems,
    IReadOnlyList<AdminActivityData> RecentActivity,
    IReadOnlyList<AdminGrowthData> UserGrowth);

public sealed record AdminRegistrationData(
    string UserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset RegisteredAt);

public sealed record AdminEventData(
    int Id,
    string Title,
    string Type,
    DateTimeOffset StartAt,
    string Location,
    int RegistrationCount,
    int? Capacity);

public sealed record AdminActivityData(
    string Kind,
    string Title,
    string Description,
    DateTimeOffset OccurredAt,
    string Icon);

public sealed record AdminGrowthData(
    string Label,
    int Students,
    int Faculty);
