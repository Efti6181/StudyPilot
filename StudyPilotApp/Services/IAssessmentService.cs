using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAssessmentService
{
    Task<IReadOnlyList<Assessment>> GetOwnedAssessmentsAsync(
        string userId,
        string? search,
        int? courseId,
        AssessmentStatus? status,
        AssessmentType? type,
        bool overdueOnly,
        string sort);

    Task<AssessmentSummary> GetSummaryAsync(string userId);
    Task<Assessment?> GetOwnedAssessmentAsync(string userId, int assessmentId, bool trackChanges = false);
    Task<bool> OwnsCourseAsync(string userId, int courseId);
    Task<IReadOnlyList<Course>> GetCourseOptionsAsync(string userId);
    Task AddAsync(Assessment assessment);
    Task SaveChangesAsync();
    void Remove(Assessment assessment);
}

public sealed record AssessmentSummary(
    int Total,
    int Completed,
    int Upcoming,
    int Overdue,
    int DueThisWeek);
