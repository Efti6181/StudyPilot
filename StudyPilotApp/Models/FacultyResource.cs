using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum FacultyResourceStatus { Draft = 0, Published = 1, Archived = 2 }

public sealed class FacultyResource
{
    public int Id { get; set; }
    public int FacultyCourseAssignmentId { get; set; }
    public FacultyCourseAssignment FacultyCourseAssignment { get; set; } = null!;
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [StringLength(3000)] public string? Description { get; set; }
    public ResourceKind Kind { get; set; }
    public ResourceCategory Category { get; set; }
    [StringLength(500)] public string? Tags { get; set; }
    [StringLength(2048)] public string? ExternalUrl { get; set; }
    [StringLength(260)] public string? OriginalFileName { get; set; }
    [StringLength(120)] public string? StoredFileName { get; set; }
    [StringLength(150)] public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public FacultyResourceStatus Status { get; set; } = FacultyResourceStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public ICollection<StudyResource> StudentResources { get; set; } = [];
}
