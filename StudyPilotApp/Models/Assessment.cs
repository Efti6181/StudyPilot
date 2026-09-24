using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyPilotApp.Models;

public class Assessment
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? FacultyAssessmentId { get; set; }
    public FacultyAssessment? FacultyAssessment { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    public AssessmentType Type { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(4000)]
    public string? Instructions { get; set; }

    public DateOnly AssignedDate { get; set; }
    public DateTime DueDate { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? TotalMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal? ObtainedMarks { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? WeightPercentage { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.NotStarted;
    public AssessmentDifficulty Difficulty { get; set; } = AssessmentDifficulty.Medium;

    [Range(typeof(decimal), "0", "1000")]
    public decimal? EstimatedStudyHours { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    [NotMapped]
    public bool IsOverdue => Status != AssessmentStatus.Completed && DueDate < DateTime.Now;
}
