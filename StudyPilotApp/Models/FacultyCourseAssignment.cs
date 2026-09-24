using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class FacultyCourseAssignment
{
    public int Id { get; set; }

    public int FacultyProfileId { get; set; }
    public FacultyProfile FacultyProfile { get; set; } = null!;

    public int CatalogCourseId { get; set; }
    public CatalogCourse CatalogCourse { get; set; } = null!;

    public int AcademicPeriodId { get; set; }
    public AcademicPeriod AcademicPeriod { get; set; } = null!;

    [Required, StringLength(30)]
    public string Section { get; set; } = "A";

    [StringLength(1500)]
    public string? CourseOverview { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<FacultyAssessment> Assessments { get; set; } = [];
    public ICollection<FacultyResource> Resources { get; set; } = [];
    public ICollection<FacultyAnnouncement> Announcements { get; set; } = [];
    public ICollection<Course> StudentCourses { get; set; } = [];
}
