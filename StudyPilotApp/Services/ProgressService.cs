using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _dbContext;

    public ProgressService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProgressAnalyticsData> GetAnalyticsAsync(string userId, DateOnly? snapshotFromDate)
    {
        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Include(course => course.Assessments)
            .Include(course => course.CourseGrades)
            .Include(course => course.PriorityPreference)
            .Where(course => course.ApplicationUserId == userId && course.Status == CourseStatus.Active)
            .OrderBy(course => course.CourseCode)
            .ToListAsync();

        var scale = await _dbContext.GradingScaleEntries
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync();
        var gradePoints = scale.ToDictionary(
            item => item.LetterGrade,
            item => item.GradePoint,
            StringComparer.OrdinalIgnoreCase);

        var semesters = await _dbContext.SemesterResults
            .AsNoTracking()
            .Include(item => item.CourseGrades)
                .ThenInclude(item => item.Course)
            .Where(item => item.ApplicationUserId == userId)
            .OrderBy(item => item.AcademicYear)
            .ThenBy(item => item.AcademicTerm)
            .ThenBy(item => item.SemesterNumber)
            .ToListAsync();

        var snapshotQuery = _dbContext.AcademicProgressSnapshots
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId);
        if (snapshotFromDate.HasValue)
            snapshotQuery = snapshotQuery.Where(item => item.SnapshotDate >= snapshotFromDate.Value);
        var snapshots = await snapshotQuery
            .OrderBy(item => item.SnapshotDate)
            .ToListAsync();

        return BuildAnalytics(courses, semesters, gradePoints, snapshots);
    }

    public async Task<AcademicProgressSnapshot> CaptureTodayAsync(string userId)
    {
        var analytics = await GetAnalyticsAsync(userId, null);
        var overview = analytics.Overview;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var snapshot = await _dbContext.AcademicProgressSnapshots
            .SingleOrDefaultAsync(item =>
                item.ApplicationUserId == userId &&
                item.SnapshotDate == today);

        if (snapshot is null)
        {
            snapshot = new AcademicProgressSnapshot
            {
                ApplicationUserId = userId,
                SnapshotDate = today
            };
            await _dbContext.AcademicProgressSnapshots.AddAsync(snapshot);
        }

        snapshot.AverageCourseProgress = overview.AverageCourseProgress;
        snapshot.AssessmentCompletionRate = overview.AssessmentCompletionRate;
        snapshot.AverageAssessmentScore = overview.AverageAssessmentScore;
        snapshot.CurrentCgpa = overview.CurrentCgpa;
        snapshot.CompletedCredits = overview.CompletedCredits;
        snapshot.ActiveCourses = overview.ActiveCourses;
        snapshot.CompletedAssessments = overview.CompletedAssessments;
        snapshot.PendingAssessments = overview.PendingAssessments;
        snapshot.OverdueAssessments = overview.OverdueAssessments;
        snapshot.AttentionCourses = overview.AttentionCourses;
        snapshot.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return snapshot;
    }

    public Task<AcademicProgressSnapshot?> GetOwnedSnapshotAsync(
        string userId,
        int snapshotId,
        bool trackChanges = false)
    {
        var query = _dbContext.AcademicProgressSnapshots
            .Where(item => item.Id == snapshotId && item.ApplicationUserId == userId);
        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public void RemoveSnapshot(AcademicProgressSnapshot snapshot) =>
        _dbContext.AcademicProgressSnapshots.Remove(snapshot);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    private static ProgressAnalyticsData BuildAnalytics(
        IReadOnlyList<Course> courses,
        IReadOnlyList<SemesterResult> semesters,
        IReadOnlyDictionary<string, decimal> gradePoints,
        IReadOnlyList<AcademicProgressSnapshot> snapshots)
    {
        var now = DateTime.Now;
        var allAssessments = courses.SelectMany(course => course.Assessments).ToList();
        var completed = allAssessments.Count(item => item.Status == AssessmentStatus.Completed);
        var pending = allAssessments.Count - completed;
        var overdue = allAssessments.Count(item =>
            item.Status != AssessmentStatus.Completed && item.DueDate < now);
        var completionRate = Percentage(completed, allAssessments.Count);
        var averageScore = AverageAssessmentScore(allAssessments);
        var averageProgress = courses.Count == 0
            ? 0m
            : Math.Round((decimal)courses.Average(item => item.ProgressPercentage), 2);

        var actualGradeInputs = semesters
            .SelectMany(item => item.CourseGrades)
            .Where(item => !string.IsNullOrWhiteSpace(item.ActualLetterGrade) &&
                           gradePoints.ContainsKey(item.ActualLetterGrade!))
            .Select(item => (item.Course.CreditHours, gradePoints[item.ActualLetterGrade!]));
        var cumulative = GpaCalculator.Calculate(actualGradeInputs);

        var courseMetrics = courses.Select(course => BuildCourseMetric(course, gradePoints, now)).ToList();
        var attention = courseMetrics.Count(item => item.NeedsAttention);
        var overview = new ProgressOverview(
            courses.Count,
            averageProgress,
            allAssessments.Count,
            completed,
            pending,
            overdue,
            completionRate,
            averageScore,
            cumulative.Credits > 0 ? cumulative.Gpa : null,
            cumulative.Credits,
            attention);

        var gpaTrend = semesters.Select(semester =>
        {
            var result = GpaCalculator.Calculate(semester.CourseGrades
                .Where(item => !string.IsNullOrWhiteSpace(item.ActualLetterGrade) &&
                               gradePoints.ContainsKey(item.ActualLetterGrade!))
                .Select(item => (item.Course.CreditHours, gradePoints[item.ActualLetterGrade!])));
            return new SemesterGpaMetric(
                semester.Id,
                $"S{semester.SemesterNumber} {semester.AcademicTerm.ToString()[..3]} {semester.AcademicYear}",
                result.Gpa,
                result.Credits);
        }).Where(item => item.Credits > 0).ToList();

        var typeMetrics = allAssessments
            .GroupBy(item => item.Type)
            .OrderBy(group => group.Key)
            .Select(group => new AssessmentTypeMetric(
                TypeLabel(group.Key),
                group.Count(),
                group.Count(item => item.Status == AssessmentStatus.Completed),
                AverageAssessmentScore(group)))
            .ToList();

        var insights = BuildInsights(overview, courseMetrics, snapshots);
        decimal? progressChange = null;
        decimal? completionChange = null;
        if (snapshots.Count >= 2)
        {
            progressChange = snapshots[^1].AverageCourseProgress - snapshots[0].AverageCourseProgress;
            completionChange = snapshots[^1].AssessmentCompletionRate - snapshots[0].AssessmentCompletionRate;
        }

        return new ProgressAnalyticsData(
            overview,
            courseMetrics,
            gpaTrend,
            typeMetrics,
            snapshots,
            insights,
            progressChange,
            completionChange);
    }

    private static CourseProgressMetric BuildCourseMetric(
        Course course,
        IReadOnlyDictionary<string, decimal> gradePoints,
        DateTime now)
    {
        var assessments = course.Assessments.ToList();
        var completed = assessments.Count(item => item.Status == AssessmentStatus.Completed);
        var pending = assessments.Count - completed;
        var overdue = assessments.Count(item => item.Status != AssessmentStatus.Completed && item.DueDate < now);
        var average = AverageAssessmentScore(assessments);
        var actualGrade = course.CourseGrades
            .Where(item => !string.IsNullOrWhiteSpace(item.ActualLetterGrade))
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Select(item => item.ActualLetterGrade)
            .FirstOrDefault();
        decimal? point = actualGrade is not null && gradePoints.TryGetValue(actualGrade, out var value)
            ? value
            : null;
        var manualLevel = course.PriorityPreference?.ManualPriorityLevel;
        var needsAttention = overdue > 0 ||
                             (average.HasValue && average.Value < 60m) ||
                             course.ProgressPercentage < 50 ||
                             manualLevel is PriorityLevel.High or PriorityLevel.Critical;

        return new CourseProgressMetric(
            course.Id,
            course.CourseCode,
            course.CourseName,
            course.ProgressPercentage,
            average,
            Percentage(completed, assessments.Count),
            pending,
            overdue,
            actualGrade,
            point,
            course.PriorityPreference?.IsPinned ?? false,
            manualLevel,
            needsAttention);
    }

    private static IReadOnlyList<AcademicInsight> BuildInsights(
        ProgressOverview overview,
        IReadOnlyList<CourseProgressMetric> courses,
        IReadOnlyList<AcademicProgressSnapshot> snapshots)
    {
        var insights = new List<AcademicInsight>();
        var strongest = courses
            .Where(item => item.AssessmentAverage.HasValue)
            .OrderByDescending(item => item.AssessmentAverage)
            .FirstOrDefault();
        if (strongest is not null)
            insights.Add(new AcademicInsight("success", "Current strength", $"{strongest.CourseCode} has your highest marked assessment average at {strongest.AssessmentAverage!.Value:0.#}%."));

        var mostUrgent = courses
            .OrderByDescending(item => item.OverdueAssessments)
            .ThenBy(item => item.AssessmentAverage ?? 101m)
            .FirstOrDefault(item => item.NeedsAttention);
        if (mostUrgent is not null)
            insights.Add(new AcademicInsight("danger", "Needs attention", $"{mostUrgent.CourseCode} has {mostUrgent.OverdueAssessments} overdue item(s), {mostUrgent.PendingAssessments} pending item(s), and {mostUrgent.ProgressPercentage}% progress."));

        if (overview.TotalAssessments > 0)
            insights.Add(new AcademicInsight(
                overview.AssessmentCompletionRate >= 70m ? "success" : "warning",
                "Completion pattern",
                $"You have completed {overview.AssessmentCompletionRate:0.#}% of assessments across active courses."));

        if (snapshots.Count >= 2)
        {
            var change = snapshots[^1].AverageCourseProgress - snapshots[0].AverageCourseProgress;
            insights.Add(new AcademicInsight(
                change >= 0 ? "success" : "warning",
                "Recorded momentum",
                $"Average course progress changed by {change:+0.#;-0.#;0} percentage points during the selected snapshot period."));
        }

        if (insights.Count == 0)
            insights.Add(new AcademicInsight("info", "Build your evidence", "Add courses, assessments and marks to unlock more detailed academic insights."));
        return insights.Take(4).ToList();
    }

    private static decimal? AverageAssessmentScore(IEnumerable<Assessment> assessments)
    {
        var marked = assessments
            .Where(item => item.TotalMarks.HasValue && item.TotalMarks.Value > 0 && item.ObtainedMarks.HasValue)
            .ToList();
        if (marked.Count == 0) return null;
        return Math.Round(Math.Clamp(
            marked.Average(item => item.ObtainedMarks!.Value / item.TotalMarks!.Value * 100m),
            0m,
            100m), 2);
    }

    private static decimal Percentage(int part, int total) =>
        total == 0 ? 0m : Math.Round(part * 100m / total, 2);

    private static string TypeLabel(AssessmentType type) => type switch
    {
        AssessmentType.FinalExam => "Final Exam",
        AssessmentType.ClassTest => "Class Test",
        _ => type.ToString()
    };
}
