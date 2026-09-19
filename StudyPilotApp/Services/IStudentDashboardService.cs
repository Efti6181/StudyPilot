using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IStudentDashboardService
{
    Task<StudentDashboardData> GetAsync(string userId);
    Task<AcademicProgressSnapshot> CaptureTodayAsync(string userId);
}

public sealed record StudentDashboardData(
    ProgressOverview Overview,
    IReadOnlyList<SemesterGpaMetric> GpaTrend,
    IReadOnlyList<CourseProgressMetric> Courses,
    IReadOnlyList<Assessment> UpcomingAssessments);
