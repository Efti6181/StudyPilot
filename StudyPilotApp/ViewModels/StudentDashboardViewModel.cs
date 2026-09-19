using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public class StudentDashboardViewModel : StudentShellViewModel
{
    public string Greeting { get; set; } = string.Empty;
    public ProgressOverview Overview { get; set; } =
        new(0, 0m, 0, 0, 0, 0, 0m, null, null, 0m, 0);
    public IReadOnlyList<SemesterGpaMetric> GpaTrend { get; set; } = [];
    public IReadOnlyList<CourseProgressMetric> FocusCourses { get; set; } = [];
    public IReadOnlyList<DashboardAssessmentViewModel> UpcomingAssessments { get; set; } = [];
    public IReadOnlyList<DashboardEventViewModel> UpcomingEvents { get; set; } = [];
}

public sealed class DashboardAssessmentViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsOverdue { get; set; }
}
