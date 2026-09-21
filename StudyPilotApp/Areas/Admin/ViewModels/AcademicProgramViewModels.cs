using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AcademicProgramIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<AcademicProgramRowViewModel> Programs { get; set; } = [];
    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class AcademicProgramRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalCredits { get; set; }
    public decimal DurationYears { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AcademicProgramDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalCredits { get; set; }
    public decimal DurationYears { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AcademicProgramFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Program code is required.")]
    [StringLength(30, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9.&-]*$",
        ErrorMessage = "Use letters, numbers, dot, ampersand, or hyphen only.")]
    [Display(Name = "Program code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program name is required.")]
    [StringLength(180, MinimumLength = 2)]
    [Display(Name = "Program name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Range(typeof(decimal), "1", "500", ErrorMessage = "Total credits must be between 1 and 500.")]
    [Display(Name = "Total credits")]
    public decimal TotalCredits { get; set; } = 120;

    [Range(typeof(decimal), "0.5", "10", ErrorMessage = "Duration must be between 0.5 and 10 years.")]
    [Display(Name = "Duration in years")]
    public decimal DurationYears { get; set; } = 4;

    [Display(Name = "Active program")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
}
