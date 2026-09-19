namespace StudyPilotApp.ViewModels;

public class StudentDashboardViewModel : StudentShellViewModel
{
    public string Greeting { get; set; } = string.Empty;
    public IReadOnlyList<DashboardEventViewModel> UpcomingEvents { get; set; } = [];
}
