using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class SemesterResult
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Range(1, 12)]
    public int SemesterNumber { get; set; }

    public AcademicTerm AcademicTerm { get; set; }

    [Range(2000, 2100)]
    public int AcademicYear { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<CourseGrade> CourseGrades { get; set; } = [];
}
