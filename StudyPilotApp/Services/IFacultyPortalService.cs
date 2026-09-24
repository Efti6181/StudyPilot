using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IFacultyPortalService
{
    Task<FacultyDashboardData?> GetDashboardAsync(string userId, CancellationToken cancellationToken = default);
    Task<FacultyCourseListData> SearchCoursesAsync(string userId, string? search, int? academicPeriodId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<FacultyCourseData?> GetCourseAsync(string userId, int assignmentId, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<bool> UpdateCourseOverviewAsync(string userId, int assignmentId, string? overview, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FacultyPeriodOptionData>> GetPeriodsAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed record FacultyDashboardData(
    int AssignedCourses,
    int ActiveCourses,
    decimal AssignedCredits,
    int UpcomingEvents,
    int ActiveAnnouncements,
    int RecentCommunityPosts,
    int UpcomingAssessments,
    int RecentlyPublishedAssessments,
    int PublishedResources,
    int PublishedCourseAnnouncements,
    IReadOnlyList<FacultyCourseData> Courses,
    IReadOnlyList<FacultyDashboardEventData> Events,
    IReadOnlyList<FacultyDashboardAnnouncementData> Announcements,
    IReadOnlyList<FacultyDashboardCommunityData> CommunityItems);

public sealed record FacultyCourseData(
    int AssignmentId,
    int CatalogCourseId,
    string Code,
    string Name,
    string DepartmentCode,
    string DepartmentName,
    string? ProgramName,
    decimal CreditHours,
    CourseType CourseType,
    CourseDifficultyLevel Difficulty,
    CareerRelevanceLevel CareerRelevance,
    string? CatalogDescription,
    string? Prerequisites,
    int AcademicPeriodId,
    AcademicTerm Term,
    int AcademicYear,
    string Section,
    string? CourseOverview,
    bool IsActive,
    DateTimeOffset AssignedAt);

public sealed record FacultyCourseListData(IReadOnlyList<FacultyCourseData> Items, int TotalCount, int ActiveCount);
public sealed record FacultyPeriodOptionData(int Id, string Label, bool IsCurrent);
public sealed record FacultyDashboardEventData(int Id, string Title, CampusEventType Type, DateTimeOffset StartAt, string Location);
public sealed record FacultyDashboardAnnouncementData(int Id, string Title, AnnouncementPriority Priority, DateTimeOffset PublishedAt);
public sealed record FacultyDashboardCommunityData(int Id, string Title, CommunityCategory Category, string AuthorName, DateTimeOffset CreatedAt);
