using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IProgressService
{
    Task<ProgressAnalyticsData> GetAnalyticsAsync(string userId, DateOnly? snapshotFromDate);
    Task<AcademicProgressSnapshot> CaptureTodayAsync(string userId);
    Task<AcademicProgressSnapshot?> GetOwnedSnapshotAsync(string userId, int snapshotId, bool trackChanges = false);
    void RemoveSnapshot(AcademicProgressSnapshot snapshot);
    Task SaveChangesAsync();
}

public sealed record ProgressAnalyticsData(
    ProgressOverview Overview,
    IReadOnlyList<CourseProgressMetric> Courses,
    IReadOnlyList<SemesterGpaMetric> GpaTrend,
    IReadOnlyList<AssessmentTypeMetric> AssessmentTypes,
    IReadOnlyList<AcademicProgressSnapshot> Snapshots,
    IReadOnlyList<AcademicInsight> Insights,
    decimal? SnapshotProgressChange,
    decimal? SnapshotCompletionChange);

public sealed record ProgressOverview(
    int ActiveCourses,
    decimal AverageCourseProgress,
    int TotalAssessments,
    int CompletedAssessments,
    int PendingAssessments,
    int OverdueAssessments,
    decimal AssessmentCompletionRate,
    decimal? AverageAssessmentScore,
    decimal? CurrentCgpa,
    decimal CompletedCredits,
    int AttentionCourses);

public sealed record CourseProgressMetric(
    int CourseId,
    string CourseCode,
    string CourseName,
    int ProgressPercentage,
    decimal? AssessmentAverage,
    decimal AssessmentCompletionRate,
    int PendingAssessments,
    int OverdueAssessments,
    string? ActualGrade,
    decimal? GradePoint,
    bool IsPinnedPriority,
    PriorityLevel? ManualPriorityLevel,
    bool NeedsAttention);

public sealed record SemesterGpaMetric(
    int SemesterId,
    string Label,
    decimal Gpa,
    decimal Credits);

public sealed record AssessmentTypeMetric(
    string Type,
    int Total,
    int Completed,
    decimal? AverageScore);

public sealed record AcademicInsight(
    string Tone,
    string Title,
    string Message);
