using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum ModeratedContentType
{
    CommunityPost = 1,
    CommunityComment = 2,
    StudyResource = 3
}

public sealed class ContentModerationRecord
{
    public long Id { get; set; }
    public ModeratedContentType ContentType { get; set; }
    public int SourceId { get; set; }

    [Required, StringLength(180)]
    public string ContentTitle { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string ContentExcerpt { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string OwnerName { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string ModeratedByUserId { get; set; } = string.Empty;
    public ApplicationUser ModeratedByUser { get; set; } = null!;

    public DateTimeOffset ModeratedAt { get; set; } = DateTimeOffset.UtcNow;
}
