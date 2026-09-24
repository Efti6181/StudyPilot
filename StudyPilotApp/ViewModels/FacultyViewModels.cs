using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public class FacultyShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FacultyId { get; set; } = string.Empty;
    public string Initials { get; set; } = "FA";
    public string DepartmentLabel { get; set; } = "Department not assigned";
    public string DesignationLabel { get; set; } = "Faculty member";
    public bool HasProfileImage { get; set; }
    public long ProfileImageVersion { get; set; }
}

public sealed class FacultyDashboardViewModel : FacultyShellViewModel
{
    public string Greeting { get; set; } = string.Empty;
    public int AssignedCourses { get; set; }
    public int ActiveCourses { get; set; }
    public decimal AssignedCredits { get; set; }
    public int UpcomingEvents { get; set; }
    public int ActiveAnnouncements { get; set; }
    public int RecentCommunityPosts { get; set; }
    public int UpcomingAssessments { get; set; }
    public int RecentlyPublishedAssessments { get; set; }
    public int PublishedResources { get; set; }
    public int PublishedCourseAnnouncements { get; set; }
    public IReadOnlyList<FacultyCourseData> Courses { get; set; } = [];
    public IReadOnlyList<FacultyDashboardEventData> Events { get; set; } = [];
    public IReadOnlyList<FacultyDashboardAnnouncementData> Announcements { get; set; } = [];
    public IReadOnlyList<FacultyDashboardCommunityData> CommunityItems { get; set; } = [];
}

public sealed class FacultyProfileViewModel : FacultyShellViewModel
{
    [StringLength(100)]
    public string? Designation { get; set; }

    [Phone, StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    [Display(Name = "Office location")]
    public string? OfficeLocation { get; set; }

    [StringLength(250)]
    [Display(Name = "Office hours")]
    public string? OfficeHours { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(1000)]
    [Display(Name = "Research and teaching interests")]
    public string? TeachingInterests { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfileImage { get; set; }

    [Display(Name = "Remove current picture")]
    public bool RemoveProfileImage { get; set; }

    public int CompletionPercentage { get; set; }
}

public sealed class FacultyCoursesViewModel : FacultyShellViewModel
{
    public string? Search { get; set; }
    public int? AcademicPeriodId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public IReadOnlyList<FacultyPeriodOptionData> Periods { get; set; } = [];
    public IReadOnlyList<FacultyCourseData> Courses { get; set; } = [];
}

public sealed class FacultyCourseDetailsViewModel : FacultyShellViewModel
{
    public FacultyCourseData Course { get; set; } = null!;
}

public sealed class FacultyCourseOverviewViewModel : FacultyShellViewModel
{
    public int AssignmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [StringLength(1500)]
    [Display(Name = "Teaching overview")]
    public string? CourseOverview { get; set; }
}
