using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class StudyResource
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ResourceKind Kind { get; set; }
    public ResourceCategory Category { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    [StringLength(2048)]
    public string? ExternalUrl { get; set; }

    [StringLength(260)]
    public string? OriginalFileName { get; set; }

    [StringLength(120)]
    public string? StoredFileName { get; set; }

    [StringLength(150)]
    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
