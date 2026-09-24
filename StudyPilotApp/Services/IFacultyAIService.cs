using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IFacultyAIService
{
    bool IsProviderConfigured { get; }

    Task<AcademicAIResult> GenerateAsync(
        string userId,
        FacultyAIMode mode,
        string prompt,
        int? assignmentId,
        IReadOnlyList<AcademicAIHistoryMessage> history,
        CancellationToken cancellationToken = default);
}

public sealed record FacultyAIContextSnapshot(
    IReadOnlyList<FacultyAICourseContext> Courses,
    FacultyAICourseContext? SelectedCourse,
    int TotalActiveCourses,
    int TotalEnrolledStudents,
    int UpcomingPublishedAssessments,
    int PublishedResources,
    int ActiveAnnouncements);

public sealed record FacultyAICourseContext(
    int AssignmentId,
    string Code,
    string Name,
    string Section,
    string Period,
    decimal Credits,
    string Type,
    string Difficulty,
    string CareerRelevance,
    string? Description,
    string? Prerequisites,
    string? TeachingOverview,
    int EnrolledStudents,
    int DraftAssessments,
    int PublishedAssessments,
    int UpcomingAssessments,
    int PublishedResources,
    int ActiveAnnouncements,
    IReadOnlyList<FacultyAIAssessmentContext> Assessments);

public sealed record FacultyAIAssessmentContext(
    string Title,
    string Type,
    DateTime DueDate,
    string Difficulty,
    decimal? TotalMarks,
    decimal? WeightPercentage,
    string Status);
