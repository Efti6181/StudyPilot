using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class CourseCatalogIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<CourseCatalogRowViewModel> Courses { get; set; } = [];
    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> ProgramOptions { get; set; } = [];
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? ProgramId { get; set; }
    public CourseType? CourseType { get; set; }
    public CourseDifficultyLevel? Difficulty { get; set; }
    public string Status { get; set; } = "all";
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int CoreCount { get; set; }
    public int HardCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class CourseCatalogRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? ProgramCode { get; set; }
    public string? ProgramName { get; set; }
    public decimal CreditHours { get; set; }
    public CourseType CourseType { get; set; }
    public int? RecommendedSemester { get; set; }
    public CourseDifficultyLevel Difficulty { get; set; }
    public CareerRelevanceLevel CareerRelevance { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CourseCatalogDetailsViewModel : AdminShellViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int? ProgramId { get; set; }
    public string? ProgramCode { get; set; }
    public string? ProgramName { get; set; }
    public decimal CreditHours { get; set; }
    public CourseType CourseType { get; set; }
    public int? RecommendedSemester { get; set; }
    public CourseDifficultyLevel Difficulty { get; set; }
    public CareerRelevanceLevel CareerRelevance { get; set; }
    public string? Description { get; set; }
    public string? Prerequisites { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IReadOnlyList<FacultyCourseAssignmentAdminViewModel> FacultyAssignments { get; set; } = [];
    public IReadOnlyList<SelectListItem> FacultyOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> AcademicPeriodOptions { get; set; } = [];
}

public sealed class FacultyCourseAssignmentAdminViewModel
{
    public int Id { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string FacultyId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CourseCatalogFormViewModel : AdminShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(30, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 .&/-]*$", ErrorMessage = "Use letters, numbers, spaces, dot, slash, ampersand, or hyphen only.")]
    [Display(Name = "Course code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(180, MinimumLength = 2)]
    [Display(Name = "Course name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a department.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid department.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Display(Name = "Program (optional)")]
    public int? ProgramId { get; set; }

    [Range(typeof(decimal), "0.5", "9", ErrorMessage = "Credits must be between 0.5 and 9.")]
    [Display(Name = "Credit hours")]
    public decimal CreditHours { get; set; } = 3;

    [Required]
    [Display(Name = "Course type")]
    public CourseType? CourseType { get; set; }

    [Required(ErrorMessage = "Select the semester students should receive from the catalog.")]
    [Range(1, 12)]
    [Display(Name = "Recommended semester")]
    public int? RecommendedSemester { get; set; }

    [Required]
    public CourseDifficultyLevel? Difficulty { get; set; } = CourseDifficultyLevel.Moderate;

    [Required]
    [Display(Name = "Career relevance")]
    public CareerRelevanceLevel? CareerRelevance { get; set; } = CareerRelevanceLevel.Medium;

    [StringLength(1500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Prerequisites { get; set; }

    [Display(Name = "Active catalog course")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> ProgramOptions { get; set; } = [];
}
