namespace StudyPilotApp.ViewModels;

public class StudentDashboardViewModel
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string StudentId { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public string Greeting { get; init; } = string.Empty;
    public string Program { get; init; } = "CSE";
    public string Semester { get; init; } = "Semester 6";
}
