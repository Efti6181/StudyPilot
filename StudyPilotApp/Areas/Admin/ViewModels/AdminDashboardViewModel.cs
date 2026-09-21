namespace StudyPilotApp.Areas.Admin.ViewModels;

public class AdminShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Initials { get; set; } = "AD";
}

public sealed class AdminDashboardViewModel : AdminShellViewModel
{
    public int TotalStudents { get; set; }
    public int TotalFaculty { get; set; }
    public int ActiveStudents { get; set; }
    public int ActiveFaculty { get; set; }
    public int PendingRegistrations { get; set; }
    public int StudentCourseRecords { get; set; }
    public int UpcomingEvents { get; set; }
    public int CommunityPosts { get; set; }
    public int Resources { get; set; }
    public int UnpublishedEvents { get; set; }
    public IReadOnlyList<AdminRegistrationViewModel> RecentRegistrations { get; set; } = [];
    public IReadOnlyList<AdminEventViewModel> UpcomingEventItems { get; set; } = [];
    public IReadOnlyList<AdminActivityViewModel> RecentActivity { get; set; } = [];
    public IReadOnlyList<AdminGrowthViewModel> UserGrowth { get; set; } = [];
}

public sealed class AdminRegistrationViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public string Initials { get; set; } = string.Empty;
}

public sealed class AdminEventViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public int RegistrationCount { get; set; }
    public int? Capacity { get; set; }
}

public sealed class AdminActivityViewModel
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Icon { get; set; } = string.Empty;
}

public sealed class AdminGrowthViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Students { get; set; }
    public int Faculty { get; set; }
}
