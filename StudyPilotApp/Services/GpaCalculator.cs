namespace StudyPilotApp.Services;

public static class GpaCalculator
{
    public static GpaCalculation Calculate(IEnumerable<(decimal Credits, decimal GradePoint)> courses)
    {
        var valid = courses.Where(item => item.Credits > 0).ToList();
        var credits = valid.Sum(item => item.Credits);
        if (credits == 0)
        {
            return new GpaCalculation(0m, 0m, 0m);
        }

        var qualityPoints = valid.Sum(item => item.Credits * item.GradePoint);
        return new GpaCalculation(
            Math.Round(qualityPoints / credits, 2, MidpointRounding.AwayFromZero),
            credits,
            qualityPoints);
    }

    public static GpaGoalResult CalculateRequiredGpa(
        decimal currentCgpa,
        decimal completedCredits,
        decimal targetCgpa,
        decimal futureCredits,
        decimal maximumGradePoint)
    {
        if (futureCredits <= 0)
        {
            return new GpaGoalResult(false, null, "Future credits must be greater than zero.");
        }

        var required = ((targetCgpa * (completedCredits + futureCredits)) -
                        (currentCgpa * completedCredits)) / futureCredits;

        required = Math.Round(required, 2, MidpointRounding.AwayFromZero);

        if (required <= 0)
        {
            return new GpaGoalResult(true, 0m, "Your current record already keeps you at or above this target.");
        }

        if (required > maximumGradePoint)
        {
            return new GpaGoalResult(
                false,
                required,
                $"This target is not possible within {futureCredits:0.##} future credits. It would require a GPA of {required:0.00}, above the maximum {maximumGradePoint:0.00}.");
        }

        return new GpaGoalResult(
            true,
            required,
            $"You need approximately {required:0.00} GPA across the next {futureCredits:0.##} credits.");
    }
}

public sealed record GpaCalculation(decimal Gpa, decimal Credits, decimal QualityPoints);
public sealed record GpaGoalResult(bool IsPossible, decimal? RequiredGpa, string Message);
