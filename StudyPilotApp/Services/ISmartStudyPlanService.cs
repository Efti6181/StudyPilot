using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface ISmartStudyPlanService
{
    Task<IReadOnlyList<StudyPlanPeriod>> GetAvailablePeriodsAsync(string userId);
    Task<IReadOnlyList<SmartStudyPlan>> GetPlansAsync(string userId);
    Task<SmartStudyPlan?> GetOwnedPlanAsync(string userId, int planId, bool trackChanges = false);
    Task<SmartStudyPlan> GenerateAsync(
        string userId,
        StudyPlanGenerationRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, int planId);
}

public sealed record StudyPlanPeriod(
    string Key,
    int Semester,
    AcademicTerm AcademicTerm,
    int AcademicYear,
    int CourseCount,
    string Label);

public sealed record StudyPlanGenerationRequest(
    string PeriodKey,
    string CareerGoal,
    IReadOnlyList<DayOfWeek> StudyDays,
    TimeOnly PreferredStartTime,
    TimeOnly PreferredEndTime,
    decimal WeeklyStudyHours,
    int SessionMinutes,
    int BreakMinutes);

public sealed class StudyPlanValidationException : Exception
{
    public StudyPlanValidationException(string message) : base(message)
    {
    }
}
