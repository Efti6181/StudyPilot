using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class ResourceListViewModel : StudentShellViewModel
{
    public IReadOnlyList<ResourceCardViewModel> Resources { get; set; } = [];
    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];
    public string? Search { get; set; }
    public int? CourseId { get; set; }
    public ResourceCategory? Category { get; set; }
    public ResourceKind? Kind { get; set; }
    public bool FavoritesOnly { get; set; }
    public string Sort { get; set; } = "newest";
    public int Total { get; set; }
    public int Files { get; set; }
    public int Links { get; set; }
    public int Favorites { get; set; }
    public long TotalFileBytes { get; set; }
}

public sealed class ResourceCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResourceKind Kind { get; set; }
    public ResourceCategory Category { get; set; }
    public string? Tags { get; set; }
    public string? ExternalUrl { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool IsFavorite { get; set; }
    public int? CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int? FacultyResourceId { get; set; }
    public bool IsFacultyPublished => FacultyResourceId.HasValue;

    public string CategoryLabel => Category switch
    {
        ResourceCategory.Document => "PDF / Document",
        ResourceCategory.PastQuestion => "Past Question",
        _ => Category.ToString()
    };

    public string Icon => Category switch
    {
        ResourceCategory.Notes => "bi-journal-text",
        ResourceCategory.Document => "bi-file-earmark-pdf",
        ResourceCategory.Slides => "bi-file-earmark-slides",
        ResourceCategory.Book => "bi-book",
        ResourceCategory.Website => "bi-globe2",
        ResourceCategory.Video => "bi-play-btn",
        ResourceCategory.PastQuestion => "bi-file-earmark-check",
        ResourceCategory.Tutorial => "bi-mortarboard",
        ResourceCategory.Code => "bi-code-square",
        _ => "bi-folder2-open"
    };

    public string FileSizeLabel => FileSizeBytes.HasValue
        ? FormatBytes(FileSizeBytes.Value)
        : "External link";

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }
}

public sealed class ResourceFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }
    public bool IsFacultyPublished { get; set; }

    [Required(ErrorMessage = "Please enter a resource title.")]
    [StringLength(180, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];

    [Required(ErrorMessage = "Please select file or link.")]
    [Display(Name = "Resource Source")]
    public ResourceKind? Kind { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    public ResourceCategory? Category { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    [StringLength(2048)]
    [Display(Name = "External URL")]
    public string? ExternalUrl { get; set; }

    [Display(Name = "Upload File")]
    public IFormFile? Upload { get; set; }

    [Display(Name = "Add to Favorites")]
    public bool IsFavorite { get; set; }

    public string? ExistingFileName { get; set; }
    public string? ExistingFileSize { get; set; }
}

public sealed class ResourceDetailsViewModel : StudentShellViewModel
{
    public ResourceCardViewModel Resource { get; set; } = new();
}

public sealed class ResourceDeleteViewModel : StudentShellViewModel
{
    public ResourceCardViewModel Resource { get; set; } = new();
}
