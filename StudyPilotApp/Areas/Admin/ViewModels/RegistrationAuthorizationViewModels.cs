using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class RegistrationAuthorizationIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<RegistrationAuthorizationRowViewModel> Records { get; set; } = [];
    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public string? Search { get; set; }
    public string Role { get; set; } = "all";
    public string Status { get; set; } = "all";
    public int? DepartmentId { get; set; }
    public int TotalCount { get; set; }
    public int StudentCount { get; set; }
    public int FacultyCount { get; set; }
    public int RegisteredCount { get; set; }
    public int UnregisteredCount { get; set; }
    public int DisabledCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class RegistrationAuthorizationRowViewModel
{
    public int Id { get; set; }
    public string UniversityId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentCode { get; set; }
    public string? ProgramCode { get; set; }
    public bool IsActive { get; set; }
    public bool IsClaimed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RegistrationAuthorizationDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string UniversityId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public int? ProgramId { get; set; }
    public string? ProgramCode { get; set; }
    public string? ProgramName { get; set; }
    public string? Batch { get; set; }
    public int? CurrentSemester { get; set; }
    public bool IsActive { get; set; }
    public bool IsClaimed { get; set; }
    public string? ApplicationUserId { get; set; }
    public string? RegisteredUserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class RegistrationAuthorizationFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }
    public bool IsClaimed { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9./-]*$",
        ErrorMessage = "Use letters, numbers, dot, slash, or hyphen only.")]
    [Display(Name = "University ID")]
    public string UniversityId { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Student|Faculty)$", ErrorMessage = "Select Student or Faculty.")]
    [Display(Name = "User type")]
    public string Role { get; set; } = "Student";

    [Range(1, int.MaxValue, ErrorMessage = "Select a department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Display(Name = "Program")]
    public int? ProgramId { get; set; }

    [StringLength(30)]
    public string? Batch { get; set; }

    [Range(1, 12)]
    [Display(Name = "Current semester")]
    public int? CurrentSemester { get; set; }

    [Display(Name = "Active authorization")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> ProgramOptions { get; set; } = [];
}
