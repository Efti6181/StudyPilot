using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum FacultyAssessmentStatus
{
    Draft = 0,
    Published = 1,
    Closed = 2
}

public sealed class FacultyAssessment
{
    public int Id { get; set; }
    public int FacultyCourseAssignmentId { get; set; }
    public FacultyCourseAssignment FacultyCourseAssignment { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;
    public AssessmentType Type { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [StringLength(4000)] public string? Instructions { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    [Range(typeof(decimal), "0", "100000")] public decimal? TotalMarks { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal? WeightPercentage { get; set; }
    public AssessmentDifficulty Difficulty { get; set; } = AssessmentDifficulty.Medium;
    public FacultyAssessmentStatus Status { get; set; } = FacultyAssessmentStatus.Draft;

    [StringLength(255)] public string? AttachmentFileName { get; set; }
    [StringLength(100)] public string? AttachmentContentType { get; set; }
    [MaxLength(5 * 1024 * 1024)] public byte[]? AttachmentData { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public ICollection<Assessment> StudentAssessments { get; set; } = [];
}
