using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class AcademicProgressSnapshot
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public DateOnly SnapshotDate { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal AverageCourseProgress { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal AssessmentCompletionRate { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? AverageAssessmentScore { get; set; }

    [Range(typeof(decimal), "0", "10")]
    public decimal? CurrentCgpa { get; set; }

    [Range(typeof(decimal), "0", "5000")]
    public decimal CompletedCredits { get; set; }

    public int ActiveCourses { get; set; }
    public int CompletedAssessments { get; set; }
    public int PendingAssessments { get; set; }
    public int OverdueAssessments { get; set; }
    public int AttentionCourses { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
