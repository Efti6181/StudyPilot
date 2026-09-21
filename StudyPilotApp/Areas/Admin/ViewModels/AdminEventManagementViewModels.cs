using StudyPilotApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AdminEventIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<AdminEventRowViewModel> Events { get; set; } = [];
    public string? Search { get; set; }
    public CampusEventType? Type { get; set; }
    public EventLocationType? LocationType { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int UpcomingCount { get; set; }
    public int ActiveRegistrationCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class AdminEventRowViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public CampusEventType Type { get; set; }
    public EventLocationType LocationType { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int RegistrationCount { get; set; }
    public int? Capacity { get; set; }
    public bool IsPublished { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}

public sealed class AdminEventDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampusEventType Type { get; set; }
    public EventLocationType LocationType { get; set; }
    public string? Venue { get; set; }
    public string? OnlineUrl { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateTimeOffset RegistrationDeadline { get; set; }
    public int? Capacity { get; set; }
    public bool IsPublished { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int RegistrationCount { get; set; }
    public int CancelledCount { get; set; }
    public int SavedCount { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public IReadOnlyList<AdminEventRegistrantViewModel> Registrants { get; set; } = [];
}

public sealed class AdminEventRegistrantViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public bool IsActive => !CancelledAt.HasValue;
}

public sealed class AdminEventFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(180, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(320, MinimumLength = 10)]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 20)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Event type")]
    public CampusEventType? Type { get; set; }

    [Required]
    [Display(Name = "Event format")]
    public EventLocationType? LocationType { get; set; }

    [StringLength(250)]
    public string? Venue { get; set; }

    [Url, StringLength(2048)]
    [Display(Name = "Online event URL")]
    public string? OnlineUrl { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    [Display(Name = "Organizer name")]
    public string OrganizerName { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Starts at")]
    public DateTime? StartAt { get; set; }

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Ends at")]
    public DateTime? EndAt { get; set; }

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Registration deadline")]
    public DateTime? RegistrationDeadline { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Participant capacity")]
    public int? Capacity { get; set; }

    [Display(Name = "Publish for students")]
    public bool IsPublished { get; set; }
}
