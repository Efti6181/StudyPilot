using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public sealed class FacultyAnnouncementIndexViewModel : FacultyShellViewModel
{
    public IReadOnlyList<FacultyAnnouncementData> Items { get; set; } = [];
    public IReadOnlyList<FacultyAssessmentCourseOption> Courses { get; set; } = [];
    public string? Search { get; set; }
    public int? AssignmentId { get; set; }
    public AnnouncementPriority? Priority { get; set; }
    public FacultyAnnouncementStatus? Status { get; set; }
}

public sealed class FacultyAnnouncementFormViewModel : FacultyShellViewModel
{
    public int? Id { get; set; }
    [Required, Display(Name = "Course and section")]
    public int? AssignmentId { get; set; }
    public IReadOnlyList<FacultyAssessmentCourseOption> Courses { get; set; } = [];
    [Required, StringLength(180, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(320, MinimumLength = 5)] public string Summary { get; set; } = string.Empty;
    [Required, StringLength(5000, MinimumLength = 10)] public string Content { get; set; } = string.Empty;
    [Required] public AnnouncementPriority? Priority { get; set; }
    [Display(Name = "Expires at")] public DateTime? ExpiresAt { get; set; }
    public FacultyAnnouncementStatus CurrentStatus { get; set; }
}

public sealed class FacultyAnnouncementDetailsViewModel : FacultyShellViewModel
{
    public FacultyAnnouncement Announcement { get; set; } = null!;
    public int RecipientCount { get; set; }
    public int ReadCount { get; set; }
}

public sealed class CourseAnnouncementIndexViewModel : StudentShellViewModel
{
    public IReadOnlyList<StudentCourseAnnouncementData> Items { get; set; } = [];
    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];
    public string? Search { get; set; }
    public int? CourseId { get; set; }
    public AnnouncementPriority? Priority { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int UnreadCount { get; set; }
}

public sealed class CourseAnnouncementDetailsViewModel : StudentShellViewModel
{
    public StudentCourseAnnouncementData Announcement { get; set; } = null!;
}
