using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAcademicAIService
{
    Task<AcademicAIResult> GenerateAsync(
        string userId,
        AcademicAIMode mode,
        string prompt,
        int? courseId,
        int? assessmentId,
        IReadOnlyList<AcademicAIHistoryMessage> history,
        CancellationToken cancellationToken = default);

    bool IsProviderConfigured { get; }
}

public sealed record AcademicAIResult(
    string Text,
    bool IsFallback,
    string Provider);

public sealed record AcademicAIHistoryMessage(
    AcademicAIMessageRole Role,
    string Content);

public interface IAcademicContextService
{
    Task<AcademicContextSnapshot> BuildAsync(
        string userId,
        int? courseId,
        int? assessmentId,
        CancellationToken cancellationToken = default);
}

public sealed record AcademicContextSnapshot(
    IReadOnlyList<AcademicCourseContext> Courses,
    IReadOnlyList<AcademicAssessmentContext> PendingAssessments,
    AcademicCourseContext? SelectedCourse,
    AcademicAssessmentContext? SelectedAssessment,
    decimal? CurrentCgpa,
    decimal? AverageAssessmentScore,
    int CompletedAssessments,
    int PendingAssessmentCount,
    int OverdueAssessmentCount,
    IReadOnlyList<string> ResourceTitles);

public sealed record AcademicCourseContext(
    int Id,
    string Code,
    string Name,
    decimal Credits,
    int Progress,
    string? TargetGrade,
    int? Confidence,
    int? TopicCompletion,
    int? WorkloadRisk,
    decimal? AvailableHours);

public sealed record AcademicAssessmentContext(
    int Id,
    int CourseId,
    string CourseCode,
    string Title,
    string Type,
    DateTime DueDate,
    string Status,
    string Difficulty,
    decimal? EstimatedHours,
    string? Description);

public interface IAITextProvider
{
    bool IsConfigured { get; }
    Task<AIProviderResult> GenerateAsync(
        string systemInstruction,
        string prompt,
        CancellationToken cancellationToken = default);
}

public sealed record AIProviderResult(bool Success, string? Text, string Provider, string? ErrorCode = null);
