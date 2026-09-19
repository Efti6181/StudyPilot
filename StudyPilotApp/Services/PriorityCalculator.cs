using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public static class PriorityCalculator
{
    public static PriorityCalculation Calculate(
        PriorityFactorValues factors,
        PriorityWeightValues weights,
        PriorityLevel? manualOverride,
        decimal availableHoursPerWeek)
    {
        var totalWeight = weights.TargetGradeGap + weights.AssessmentUrgency +
                          weights.CourseCredit + weights.Weakness +
                          weights.IncompleteTopics + weights.WorkloadRisk;

        if (totalWeight <= 0)
        {
            throw new ArgumentException("Priority weights must total more than zero.", nameof(weights));
        }

        var score =
            (Clamp(factors.TargetGradeGap) * weights.TargetGradeGap +
             Clamp(factors.AssessmentUrgency) * weights.AssessmentUrgency +
             Clamp(factors.CourseCredit) * weights.CourseCredit +
             Clamp(factors.Weakness) * weights.Weakness +
             Clamp(factors.IncompleteTopics) * weights.IncompleteTopics +
             Clamp(factors.WorkloadRisk) * weights.WorkloadRisk) / totalWeight;

        score = Math.Round(score, 1, MidpointRounding.AwayFromZero);
        var calculatedLevel = score switch
        {
            >= 80m => PriorityLevel.Critical,
            >= 60m => PriorityLevel.High,
            >= 35m => PriorityLevel.Medium,
            _ => PriorityLevel.Low
        };

        var requestedMinutes = score switch
        {
            >= 80m => 120,
            >= 60m => 90,
            >= 35m => 60,
            _ => 30
        };
        var weeklyMinutes = Math.Max(0, (int)Math.Round(availableHoursPerWeek * 60m));
        var suggestedMinutes = weeklyMinutes == 0
            ? requestedMinutes
            : Math.Min(requestedMinutes, Math.Max(30, weeklyMinutes));

        return new PriorityCalculation(
            score,
            calculatedLevel,
            manualOverride ?? calculatedLevel,
            manualOverride.HasValue,
            suggestedMinutes);
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 100m);
}

public sealed record PriorityFactorValues(
    decimal TargetGradeGap,
    decimal AssessmentUrgency,
    decimal CourseCredit,
    decimal Weakness,
    decimal IncompleteTopics,
    decimal WorkloadRisk);

public sealed record PriorityWeightValues(
    decimal TargetGradeGap,
    decimal AssessmentUrgency,
    decimal CourseCredit,
    decimal Weakness,
    decimal IncompleteTopics,
    decimal WorkloadRisk);

public sealed record PriorityCalculation(
    decimal Score,
    PriorityLevel CalculatedLevel,
    PriorityLevel DisplayLevel,
    bool IsManualOverride,
    int SuggestedStudyMinutes);
