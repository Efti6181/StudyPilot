using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class EventIndexViewModel : StudentShellViewModel
{
    public IReadOnlyList<EventCardViewModel> Events { get; set; } = [];
    public string? Search { get; set; }
    public CampusEventType? Type { get; set; }
    public EventLocationType? LocationType { get; set; }
    public string Scope { get; set; } = "upcoming";
    public string Sort { get; set; } = "soonest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalEvents { get; set; }
    public int RegisteredCount { get; set; }
    public int SavedCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalEvents / (double)PageSize));
}

public sealed class EventCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampusEventType Type { get; set; }
    public EventLocationType LocationType { get; set; }
    public string? Venue { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateTimeOffset RegistrationDeadline { get; set; }
    public int? Capacity { get; set; }
    public int RegistrationCount { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsSaved { get; set; }
    public bool HasOnlineLink { get; set; }

    public bool IsPast => EndAt <= DateTimeOffset.UtcNow;
    public bool RegistrationClosed => RegistrationDeadline < DateTimeOffset.UtcNow || StartAt <= DateTimeOffset.UtcNow;
    public bool IsFull => Capacity.HasValue && RegistrationCount >= Capacity.Value;
    public int? SpotsLeft => Capacity.HasValue ? Math.Max(0, Capacity.Value - RegistrationCount) : null;
    public int CapacityPercentage => Capacity.HasValue
        ? Math.Min(100, (int)Math.Round(RegistrationCount / (double)Capacity.Value * 100))
        : 0;

    public string LocationLabel => LocationType switch
    {
        EventLocationType.OnCampus => Venue ?? "Campus venue",
        EventLocationType.Online => "Online event",
        _ => string.IsNullOrWhiteSpace(Venue) ? "Hybrid event" : $"{Venue} + Online"
    };

    public string LocationTypeLabel => LocationType switch
    {
        EventLocationType.OnCampus => "On Campus",
        EventLocationType.Online => "Online",
        _ => "Hybrid"
    };

    public string Icon => Type switch
    {
        CampusEventType.Workshop => "bi-tools",
        CampusEventType.Seminar => "bi-mic",
        CampusEventType.Competition => "bi-trophy",
        CampusEventType.Career => "bi-briefcase",
        CampusEventType.Cultural => "bi-music-note-beamed",
        CampusEventType.Sports => "bi-dribbble",
        CampusEventType.Club => "bi-people",
        _ => "bi-mortarboard"
    };
}

public sealed class EventDetailsViewModel : StudentShellViewModel
{
    public EventCardViewModel Event { get; set; } = new();
}

public sealed class FacultyEventIndexViewModel : FacultyShellViewModel
{
    public IReadOnlyList<EventCardViewModel> Events { get; set; } = [];
    public string? Search { get; set; }
    public CampusEventType? Type { get; set; }
    public EventLocationType? LocationType { get; set; }
    public string Scope { get; set; } = "upcoming";
    public string Sort { get; set; } = "soonest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalEvents { get; set; }
    public int RegisteredCount { get; set; }
    public int SavedCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalEvents / (double)PageSize));
}

public sealed class FacultyEventDetailsViewModel : FacultyShellViewModel
{
    public EventCardViewModel Event { get; set; } = new();
}

public sealed class DashboardEventViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public bool IsRegistered { get; set; }
}
