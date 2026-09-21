using StudyPilotApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class ModerationIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<ModerationItemViewModel> Items { get; set; } = [];
    public IReadOnlyList<ModerationHistoryViewModel> History { get; set; } = [];
    public string? Search { get; set; }
    public string Scope { get; set; } = "all";
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int ResourceCount { get; set; }
    public int RemovedCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class ModerationItemViewModel
{
    public ModeratedContentType ContentType { get; set; }
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Detail { get; set; } = string.Empty;
    public int InteractionCount { get; set; }
}

public sealed class ModerationHistoryViewModel
{
    public long Id { get; set; }
    public ModeratedContentType ContentType { get; set; }
    public int SourceId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string ContentExcerpt { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ModeratorName { get; set; } = string.Empty;
    public DateTimeOffset ModeratedAt { get; set; }
}

public sealed class ModerationReviewViewModel : AdminShellViewModel
{
    public ModeratedContentType ContentType { get; set; }
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Detail { get; set; } = string.Empty;
    public int InteractionCount { get; set; }

    [Required, StringLength(1000, MinimumLength = 10)]
    [Display(Name = "Removal reason")]
    public string Reason { get; set; } = string.Empty;
}
