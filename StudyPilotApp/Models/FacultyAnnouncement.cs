using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum FacultyAnnouncementStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public sealed class FacultyAnnouncement
{
    public int Id { get; set; }
    public int FacultyCourseAssignmentId { get; set; }
    public FacultyCourseAssignment FacultyCourseAssignment { get; set; } = null!;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string Summary { get; set; } = string.Empty;

    [Required, StringLength(5000)]
    public string Content { get; set; } = string.Empty;

    public AnnouncementPriority Priority { get; set; }
    public FacultyAnnouncementStatus Status { get; set; } = FacultyAnnouncementStatus.Draft;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }

    public ICollection<FacultyAnnouncementRecipient> Recipients { get; set; } = [];
}

public sealed class FacultyAnnouncementRecipient
{
    public int Id { get; set; }
    public int FacultyAnnouncementId { get; set; }
    public FacultyAnnouncement FacultyAnnouncement { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTimeOffset DeliveredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
