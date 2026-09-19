using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public sealed class ProgressDashboardViewModel : StudentShellViewModel
{
    public int PeriodDays { get; set; } = 90;
    public ProgressOverview Overview { get; set; } = new(0, 0, 0, 0, 0, 0, 0, null, null, 0, 0);
    public IReadOnlyList<CourseProgressMetric> Courses { get; set; } = [];
    public IReadOnlyList<SemesterGpaMetric> GpaTrend { get; set; } = [];
    public IReadOnlyList<AssessmentTypeMetric> AssessmentTypes { get; set; } = [];
    public IReadOnlyList<AcademicProgressSnapshot> Snapshots { get; set; } = [];
    public IReadOnlyList<AcademicInsight> Insights { get; set; } = [];
    public decimal? SnapshotProgressChange { get; set; }
    public decimal? SnapshotCompletionChange { get; set; }
}
