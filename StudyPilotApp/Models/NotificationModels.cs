using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum NotificationType
{
    General = 0,
    Academic = 1,
    Assessment = 2,
    Event = 3,
    Community = 4,
    System = 5
}

public sealed class AppNotification
{
    public long Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;

    [StringLength(500)]
    public string? RelatedUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
