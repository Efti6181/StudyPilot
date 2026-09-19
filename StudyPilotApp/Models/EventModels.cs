using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum CampusEventType
{
    Workshop = 0,
    Seminar = 1,
    Competition = 2,
    Career = 3,
    Cultural = 4,
    Sports = 5,
    Club = 6,
    Academic = 7
}

public enum EventLocationType
{
    [Display(Name = "On Campus")]
    OnCampus = 0,
    Online = 1,
    Hybrid = 2
}

public sealed class CampusEvent
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(320)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    public CampusEventType Type { get; set; }
    public EventLocationType LocationType { get; set; }

    [StringLength(250)]
    public string? Venue { get; set; }

    [StringLength(2048)]
    public string? OnlineUrl { get; set; }

    [Required, StringLength(150)]
    public string OrganizerName { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateTimeOffset RegistrationDeadline { get; set; }

    [Range(1, 100000)]
    public int? Capacity { get; set; }

    public bool IsPublished { get; set; } = true;

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<EventRegistration> Registrations { get; set; } = [];
    public ICollection<SavedEvent> SavedByStudents { get; set; } = [];
}

public sealed class EventRegistration
{
    public int CampusEventId { get; set; }
    public CampusEvent CampusEvent { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class SavedEvent
{
    public int CampusEventId { get; set; }
    public CampusEvent CampusEvent { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
}
