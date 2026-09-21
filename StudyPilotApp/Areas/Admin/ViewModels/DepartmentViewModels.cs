using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class DepartmentIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<DepartmentRowViewModel> Departments { get; set; } = [];
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class DepartmentRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DepartmentDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class DepartmentFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Department code is required.")]
    [StringLength(20, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9.&-]*$",
        ErrorMessage = "Use letters, numbers, dot, ampersand, or hyphen only.")]
    [Display(Name = "Department code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(150, MinimumLength = 2)]
    [Display(Name = "Department name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Active department")]
    public bool IsActive { get; set; } = true;
}
