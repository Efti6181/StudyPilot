using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class CoursePriorityPreference
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [Range(1, 5)]
    public int ConfidenceRating { get; set; } = 3;

    [Range(0, 100)]
    public int TopicCompletionPercentage { get; set; } = 50;

    [Range(1, 5)]
    public int WorkloadRisk { get; set; } = 3;

    [Range(typeof(decimal), "0", "168")]
    public decimal AvailableStudyHoursPerWeek { get; set; } = 5m;

    public PriorityLevel? ManualPriorityLevel { get; set; }
    public bool IsPinned { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
