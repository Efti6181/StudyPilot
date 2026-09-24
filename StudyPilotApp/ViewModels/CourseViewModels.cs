using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class CourseListViewModel : StudentShellViewModel
{
    public IReadOnlyList<CourseCardViewModel> Courses { get; set; } = [];
    public string? Search { get; set; }
    public CourseStatus? Status { get; set; }
    public CourseType? Type { get; set; }
    public string Sort { get; set; } = "code";
    public int ActiveCourses { get; set; }
    public decimal ActiveCredits { get; set; }
    public double AverageProgress { get; set; }
    public int NeedsAttention { get; set; }
}

public sealed class CourseCardViewModel
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal CreditHours { get; set; }
    public string Instructor { get; set; } = "Instructor not set";
    public string InstructorInitials { get; set; } = "IN";
    public int Semester { get; set; }
    public AcademicTerm AcademicTerm { get; set; }
    public int AcademicYear { get; set; }
    public CourseType CourseType { get; set; }
    public CourseStatus Status { get; set; }
    public CourseColor Color { get; set; }
    public int ProgressPercentage { get; set; }
    public string? TargetGrade { get; set; }
    public string? Description { get; set; }

    public string ColorCss => Color.ToString().ToLowerInvariant();
    public string ProgressState => ProgressPercentage < 60 ? "risk" : ProgressPercentage >= 75 ? "strong" : "normal";
}

public sealed class CourseFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Select a course from the Admin-managed catalog.")]
    [Display(Name = "Catalog course")]
    public int? CatalogCourseId { get; set; }

    [Required(ErrorMessage = "Select your assigned instructor.")]
    [Display(Name = "Instructor")]
    public int? FacultyCourseAssignmentId { get; set; }

    public bool IsCatalogLocked { get; set; }
    public IReadOnlyList<CatalogCourseSelectionOption> CatalogOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> InstructorOptions { get; set; } = [];

    [StringLength(20)]
    [Display(Name = "Course Code")]
    public string CourseCode { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.5", "9.0")]
    [Display(Name = "Credit Hours")]
    public decimal? CreditHours { get; set; }

    [StringLength(150)]
    public string? Instructor { get; set; }

    [Range(1, 12)]
    public int? Semester { get; set; }

    [Display(Name = "Academic Term")]
    public AcademicTerm? AcademicTerm { get; set; }

    [Range(2000, 2100)]
    [Display(Name = "Academic Year")]
    public int? AcademicYear { get; set; }

    [Display(Name = "Course Type")]
    public CourseType? CourseType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Target Grade")]
    public string? TargetGrade { get; set; }

    [Required]
    public CourseStatus? Status { get; set; }

    [Required]
    [Display(Name = "Card Color")]
    public CourseColor? Color { get; set; }

    [Required]
    [Range(0, 100)]
    [Display(Name = "Current Progress")]
    public int? ProgressPercentage { get; set; }
}

public sealed record CatalogCourseSelectionOption(
    int Id,
    string Label,
    string Code,
    string Name,
    decimal CreditHours,
    CourseType CourseType,
    int Semester,
    string? Description);

public sealed class CourseDetailsViewModel : StudentShellViewModel
{
    public CourseCardViewModel Course { get; set; } = new();
    public int AssessmentCount { get; set; }
    public int CourseGradeCount { get; set; }
}

public sealed class CourseDeleteViewModel : StudentShellViewModel
{
    public CourseCardViewModel Course { get; set; } = new();
    public int RelatedAssessmentCount { get; set; }
    public int RelatedCourseGradeCount { get; set; }
}
