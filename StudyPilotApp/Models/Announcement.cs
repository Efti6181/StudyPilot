using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum AnnouncementAudience
{
    Everyone = 0,
    Students = 1,
    Faculty = 2
}

public enum AnnouncementPriority
{
    Normal = 0,
    Important = 1,
    Urgent = 2
}

public sealed class Announcement
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string Summary { get; set; } = string.Empty;

    [Required, StringLength(5000)]
    public string Content { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; }
    public AnnouncementPriority Priority { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public int RecipientCount { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
