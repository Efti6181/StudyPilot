using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class PriorityWeightSettings
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Range(typeof(decimal), "0", "100")]
    public decimal TargetGradeGapWeight { get; set; } = 30m;

    [Range(typeof(decimal), "0", "100")]
    public decimal AssessmentUrgencyWeight { get; set; } = 20m;

    [Range(typeof(decimal), "0", "100")]
    public decimal CourseCreditWeight { get; set; } = 15m;

    [Range(typeof(decimal), "0", "100")]
    public decimal WeaknessWeight { get; set; } = 15m;

    [Range(typeof(decimal), "0", "100")]
    public decimal IncompleteTopicsWeight { get; set; } = 10m;

    [Range(typeof(decimal), "0", "100")]
    public decimal WorkloadRiskWeight { get; set; } = 10m;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
