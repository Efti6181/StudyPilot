using StudyPilotApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AnnouncementIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<AnnouncementRowViewModel> Announcements { get; set; } = [];
    public string? Search { get; set; }
    public AnnouncementAudience? Audience { get; set; }
    public AnnouncementPriority? Priority { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ActiveCount { get; set; }
    public int UrgentCount { get; set; }
    public int DeliveredRecipientCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class AnnouncementRowViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; }
    public AnnouncementPriority Priority { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int RecipientCount { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}

public sealed class AnnouncementDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; }
    public AnnouncementPriority Priority { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public int RecipientCount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}

public sealed class AnnouncementFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }
    public bool AudienceLocked { get; set; }

    [Required, StringLength(180, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(320, MinimumLength = 10)]
    public string Summary { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 20)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Target audience")]
    public AnnouncementAudience? Audience { get; set; }

    [Required]
    public AnnouncementPriority? Priority { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Expires at (optional)")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "Publish and notify recipients")]
    public bool IsPublished { get; set; }
}
