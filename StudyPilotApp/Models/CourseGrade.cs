using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class CourseGrade
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public int SemesterResultId { get; set; }
    public SemesterResult SemesterResult { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [Required, StringLength(10)]
    public string ExpectedLetterGrade { get; set; } = string.Empty;

    [StringLength(10)]
    public string? ActualLetterGrade { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
