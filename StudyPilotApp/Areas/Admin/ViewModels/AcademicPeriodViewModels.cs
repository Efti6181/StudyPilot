using StudyPilotApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AcademicPeriodIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<AcademicPeriodRowViewModel> Periods { get; set; } = [];
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int CurrentCount { get; set; }
    public int UpcomingCount { get; set; }
    public int PastCount { get; set; }
    public int InactiveCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class AcademicPeriodRowViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public AcademicTerm Term { get; set; }
    public int AcademicYear { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}

public sealed class AcademicPeriodDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public AcademicTerm Term { get; set; }
    public int AcademicYear { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
    public string DisplayStatus { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AcademicPeriodFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Select an academic term.")]
    [Display(Name = "Term")]
    public AcademicTerm? Term { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Range(2000, 2100)]
    [Display(Name = "Academic year")]
    public int? AcademicYear { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Term start date")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Term end date")]
    public DateOnly? EndDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Registration opens")]
    public DateOnly? RegistrationStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Registration closes")]
    public DateOnly? RegistrationEndDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Current academic term")]
    public bool IsCurrent { get; set; }

    [Display(Name = "Active academic term")]
    public bool IsActive { get; set; } = true;
}
