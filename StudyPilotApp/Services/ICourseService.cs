using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetOwnedCoursesAsync(
        string userId,
        string? search,
        CourseStatus? status,
        CourseType? type,
        string sort);

    Task<CourseSummary> GetSummaryAsync(string userId);
    Task<Course?> GetOwnedCourseAsync(string userId, int courseId, bool trackChanges = false);
    Task<int> GetAssessmentCountAsync(string userId, int courseId);

    Task<bool> DuplicateExistsAsync(
        string userId,
        string courseCode,
        int semester,
        AcademicTerm academicTerm,
        int academicYear,
        int? excludedCourseId = null);

    Task AddAsync(Course course);
    Task SaveChangesAsync();
    void Remove(Course course);
}

public sealed record CourseSummary(
    int ActiveCourses,
    decimal ActiveCredits,
    double AverageProgress,
    int NeedsAttention);
