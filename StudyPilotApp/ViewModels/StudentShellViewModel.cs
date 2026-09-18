namespace StudyPilotApp.ViewModels;

public class StudentShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string DepartmentLabel { get; set; } = "Department not set";
    public string SemesterLabel { get; set; } = "Semester not set";
    public bool HasProfileImage { get; set; }
    public long ProfileImageVersion { get; set; }
}
