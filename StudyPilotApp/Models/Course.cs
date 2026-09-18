using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string CourseName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.5", "9.0")]
    public decimal CreditHours { get; set; }

    [StringLength(150)]
    public string? Instructor { get; set; }

    [Range(1, 12)]
    public int Semester { get; set; }

    public AcademicTerm AcademicTerm { get; set; }

    [Range(2000, 2100)]
    public int AcademicYear { get; set; }

    public CourseType CourseType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(5)]
    public string? TargetGrade { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Active;
    public CourseColor Color { get; set; } = CourseColor.Blue;

    [Range(0, 100)]
    public int ProgressPercentage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Assessment> Assessments { get; set; } = [];
}
